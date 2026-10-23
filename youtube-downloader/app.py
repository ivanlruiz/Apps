"""Servidor web para descargar, recortar y elegir la calidad de videos de YouTube.

Usa yt-dlp para obtener la información y descargar los videos, y ffmpeg para
unir video + audio, recortar segmentos y convertir el audio.
"""

import os
import re
import shutil
import tempfile
import threading
import time
import uuid
from urllib.parse import urlparse

import yt_dlp
from flask import Flask, abort, jsonify, request, send_file, send_from_directory
from yt_dlp.utils import DownloadError, download_range_func, sanitize_filename

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
STATIC_DIR = os.path.join(BASE_DIR, 'static')
DOWNLOAD_DIR = os.environ.get('DOWNLOAD_DIR') or os.path.join(tempfile.gettempdir(), 'yt-downloader')
JOB_TTL_SECONDS = int(os.environ.get('JOB_TTL_MINUTES', '60')) * 60
MAX_CONCURRENT = max(1, int(os.environ.get('MAX_CONCURRENT_DOWNLOADS', '2')))
COOKIES_FILE = os.environ.get('YTDLP_COOKIES') or None
FFMPEG = shutil.which('ffmpeg')

YOUTUBE_HOST_RE = re.compile(r'^(?:[a-z0-9-]+\.)?(?:youtube\.com|youtube-nocookie\.com|youtu\.be)$')
VIDEO_ID_RE = re.compile(r'^[A-Za-z0-9_-]{11}$')
JOB_ID_RE = re.compile(r'^[0-9a-f]{32}$')

VIDEO_CONTAINERS = ('mp4', 'webm', 'mkv')
AUDIO_FORMATS = ('mp3', 'm4a', 'opus', 'wav', 'flac')
AUDIO_BITRATES = ('320', '256', '192', '128', '96')

# Codificadores usados cuando se pide un corte preciso (requiere recodificar el video).
PRECISE_CUT_ARGS = {
    'mp4': ['-c:v', 'libx264', '-preset', 'veryfast', '-crf', '18', '-c:a', 'aac', '-b:a', '192k'],
    'mkv': ['-c:v', 'libx264', '-preset', 'veryfast', '-crf', '18', '-c:a', 'aac', '-b:a', '192k'],
    'webm': ['-c:v', 'libvpx-vp9', '-crf', '32', '-b:v', '0', '-deadline', 'realtime',
             '-cpu-used', '8', '-row-mt', '1', '-c:a', 'libopus', '-b:a', '160k'],
}

POSTPROCESSOR_STAGES = {
    'Merger': 'Uniendo video y audio',
    'ExtractAudio': 'Convirtiendo audio',
    'VideoRemuxer': 'Ajustando el formato',
    'Metadata': 'Añadiendo metadatos',
    'MoveFiles': 'Finalizando',
}

app = Flask(__name__, static_folder=STATIC_DIR, static_url_path='/static')


# --------------------------------------------------------------------------
# Utilidades
# --------------------------------------------------------------------------

def normalize_url(url):
    """Devuelve la URL si apunta a YouTube, o None si no es válida."""
    if not isinstance(url, str):
        return None
    url = url.strip()
    if not url:
        return None
    if '://' not in url:
        url = 'https://' + url
    try:
        parsed = urlparse(url)
    except ValueError:
        return None
    if parsed.scheme not in ('http', 'https') or not YOUTUBE_HOST_RE.match(parsed.hostname or ''):
        return None
    return url


def base_opts():
    opts = {
        'quiet': True,
        'no_warnings': True,
        'noprogress': True,
        'noplaylist': True,
        # YouTube necesita un intérprete de JavaScript; se usa el primero disponible.
        'js_runtimes': {'deno': {}, 'node': {}, 'bun': {}},
        # Si falta el paquete yt-dlp-ejs (p. ej. en Termux), bajar el script oficial
        # desde GitHub; yt-dlp comprueba su hash antes de usarlo.
        'remote_components': ['ejs:github'],
    }
    if FFMPEG:
        opts['ffmpeg_location'] = FFMPEG
    if COOKIES_FILE:
        opts['cookiefile'] = COOKIES_FILE
    return opts


def friendly_error(exc):
    msg = re.sub(r'\x1b\[[0-9;]*m', '', str(exc))
    msg = re.sub(r'^ERROR:\s*', '', msg).strip()
    lowered = msg.lower()
    if 'confirm you' in lowered and 'bot' in lowered:
        return ('YouTube pide confirmar que no eres un bot. Configura un archivo de cookies '
                '(variable YTDLP_COOKIES, ver README) e inténtalo de nuevo.')
    if 'confirm your age' in lowered or 'age-restricted' in lowered:
        return 'El video tiene restricción de edad. Necesitas configurar cookies de una cuenta (ver README).'
    if 'private video' in lowered:
        return 'El video es privado.'
    if 'video unavailable' in lowered or 'not available' in lowered:
        return 'El video no está disponible.'
    if 'members-only' in lowered or 'join this channel' in lowered:
        return 'El video es exclusivo para miembros del canal.'
    if 'requested format is not available' in lowered:
        return 'Esa combinación de calidad y formato no está disponible para este video. Prueba otra.'
    if 'ffmpeg' in lowered and 'not found' in lowered:
        return 'No se encontró ffmpeg. Instálalo para poder unir, recortar y convertir.'
    return msg or 'Error desconocido.'


def estimate_size(fmt, duration):
    size = fmt.get('filesize') or fmt.get('filesize_approx')
    if not size and fmt.get('tbr') and duration:
        size = fmt['tbr'] * 125 * duration  # kbit/s -> bytes
    return int(size or 0)


def resolution_of(fmt):
    """Resolución estilo YouTube: el lado más corto (así un Short 1080x1920 es 1080p)."""
    width, height = fmt.get('width'), fmt.get('height')
    if width and height:
        return min(width, height)
    return height


def quality_badge(res):
    if res >= 4320:
        return '8K'
    if res >= 2160:
        return '4K'
    if res >= 1440:
        return '2K'
    if res >= 720:
        return 'HD'
    return ''


def summarize_info(info):
    duration = info.get('duration')
    formats = [f for f in info.get('formats') or [] if not f.get('has_drm')]

    audio_sizes = [estimate_size(f, duration) for f in formats
                   if f.get('vcodec') == 'none' and f.get('acodec') not in (None, 'none')]
    best_audio = max(audio_sizes, default=0)

    qualities = {}
    for f in formats:
        if f.get('vcodec') in (None, 'none'):
            continue
        res = resolution_of(f)
        if not res:
            continue
        q = qualities.setdefault(res, {'res': res, 'fps': 0, 'size': 0})
        q['fps'] = max(q['fps'], int(f.get('fps') or 0))
        size = estimate_size(f, duration)
        if size and f.get('acodec') in (None, 'none'):
            size += best_audio
        q['size'] = max(q['size'], size)

    quality_list = []
    for res in sorted(qualities, reverse=True):
        q = qualities[res]
        quality_list.append({
            'res': res,
            'fps': q['fps'],
            'label': f'{res}p' + (str(q['fps']) if q['fps'] > 30 else ''),
            'badge': quality_badge(res),
            'size': q['size'] or None,
        })

    return {
        'id': info.get('id'),
        'title': info.get('title') or info.get('id'),
        'channel': info.get('channel') or info.get('uploader'),
        'duration': duration,
        'thumbnail': info.get('thumbnail'),
        'view_count': info.get('view_count'),
        'upload_date': info.get('upload_date'),
        'qualities': quality_list,
        'audio_size': best_audio or None,
    }


def parse_seconds(value):
    if value in (None, ''):
        return None
    try:
        seconds = float(value)
    except (TypeError, ValueError):
        raise ValueError('Tiempo inválido.')
    if seconds < 0 or seconds != seconds or seconds == float('inf'):
        raise ValueError('Tiempo inválido.')
    return seconds


def time_tag(seconds):
    seconds = int(seconds)
    h, rem = divmod(seconds, 3600)
    m, s = divmod(rem, 60)
    return f'{h}h{m:02d}m{s:02d}s' if h else f'{m:02d}m{s:02d}s'


def error_response(message, status=400):
    return jsonify({'error': message}), status


# --------------------------------------------------------------------------
# Trabajos de descarga
# --------------------------------------------------------------------------

class Job:
    def __init__(self, params):
        self.id = uuid.uuid4().hex
        self.dir = os.path.join(DOWNLOAD_DIR, self.id)
        self.params = params
        self.status = 'queued'
        self.stage = 'En cola'
        self.progress = None
        self.speed = None
        self.eta = None
        self.file = None
        self.filename = None
        self.size = None
        self.error = None
        self.created = time.time()
        self.finished = None

    def to_dict(self):
        return {
            'id': self.id,
            'status': self.status,
            'stage': self.stage,
            'progress': self.progress,
            'speed': self.speed,
            'eta': self.eta,
            'filename': self.filename,
            'size': self.size,
            'error': self.error,
        }

    def on_progress(self, d):
        if d['status'] != 'downloading':
            return
        info = d.get('info_dict') or {}
        parts = info.get('requested_formats') or [info]
        index = next((i for i, f in enumerate(parts) if f.get('format_id') == info.get('format_id')), 0)

        total = d.get('total_bytes') or d.get('total_bytes_estimate')
        fraction = min(d.get('downloaded_bytes', 0) / total, 1) if total else None

        if fraction is not None:
            # Ponderar cada parte (video, audio) por su tamaño cuando se conoce.
            sizes = [estimate_size(f, info.get('duration')) for f in parts]
            if all(sizes):
                done = sum(sizes[:index]) + fraction * sizes[index]
                self.progress = round(100 * done / sum(sizes), 1)
            else:
                self.progress = round(100 * (index + fraction) / len(parts), 1)

        is_audio = info.get('vcodec') == 'none'
        label = 'Descargando audio' if is_audio else 'Descargando video'
        self.stage = f'{label} ({index + 1}/{len(parts)})' if len(parts) > 1 else label
        self.speed = d.get('speed')
        self.eta = d.get('eta')

    def on_postprocess(self, d):
        if d['status'] == 'started':
            self.status = 'processing'
            self.progress = None
            self.speed = self.eta = None
            self.stage = POSTPROCESSOR_STAGES.get(d.get('postprocessor'), 'Procesando')

    def build_opts(self):
        p = self.params
        opts = base_opts()
        opts.update({
            'outtmpl': os.path.join(self.dir, '%(id)s.%(ext)s'),
            'progress_hooks': [self.on_progress],
            'postprocessor_hooks': [self.on_postprocess],
            'overwrites': True,
        })

        trimmed = p['start'] is not None or p['end'] is not None
        if trimmed:
            start = p['start'] or 0
            end = p['end'] if p['end'] is not None else float('inf')
            opts['download_ranges'] = download_range_func(None, [(start, end)])

        if p['kind'] == 'audio':
            fmt = p['audio_format']
            opts['format'] = 'bestaudio[ext=m4a]/bestaudio/best' if fmt == 'm4a' else 'bestaudio/best'
            extract = {'key': 'FFmpegExtractAudio', 'preferredcodec': fmt}
            if fmt in ('mp3', 'm4a', 'opus'):
                extract['preferredquality'] = p['audio_quality']
            opts['postprocessors'] = [extract, {'key': 'FFmpegMetadata'}]
            return opts

        container = p['container']
        sort = [] if p['quality'] == 'best' else [f'res:{p["quality"]}']
        if container == 'mp4':
            sort.append('+codec:avc:m4a')  # H.264 + AAC: se reproduce en cualquier sitio
        if container == 'webm':
            # WEBM solo admite VP8/VP9/AV1 + Vorbis/Opus: elegir pistas que ya lo sean.
            opts['format'] = 'bestvideo*[ext=webm]+bestaudio[ext=webm]/best[ext=webm]'
        else:
            opts['format'] = 'bestvideo*+bestaudio/best'
        opts['format_sort'] = sort
        opts['merge_output_format'] = container
        opts['postprocessors'] = [
            {'key': 'FFmpegVideoRemuxer', 'preferedformat': container},
            {'key': 'FFmpegMetadata'},
        ]
        if trimmed and p['precise']:
            opts['force_keyframes_at_cuts'] = True
            opts['external_downloader_args'] = {'ffmpeg_o': PRECISE_CUT_ARGS[container]}
        return opts

    def download_name(self, info, ext):
        title = sanitize_filename(info.get('title') or self.params['id'], restricted=False).strip() or 'video'
        title = title[:150]
        p = self.params
        if p['start'] is not None or p['end'] is not None:
            end = p['end'] if p['end'] is not None else info.get('duration')
            title += f' ({time_tag(p["start"] or 0)}-{time_tag(end) if end else "fin"})'
        return f'{title}.{ext}'

    def run(self):
        with _download_slots:
            self.status = 'downloading'
            trimmed = self.params['start'] is not None or self.params['end'] is not None
            # Al recortar, ffmpeg descarga solo el segmento y no informa del progreso.
            self.stage = 'Descargando y recortando' if trimmed else 'Iniciando descarga'
            try:
                os.makedirs(self.dir, exist_ok=True)
                url = f'https://www.youtube.com/watch?v={self.params["id"]}'
                with yt_dlp.YoutubeDL(self.build_opts()) as ydl:
                    info = ydl.extract_info(url, download=True)
                path = self._find_output(info)
                ext = os.path.splitext(path)[1].lstrip('.')
                self.file = path
                self.filename = self.download_name(info, ext)
                self.size = os.path.getsize(path)
                self.progress = 100
                self.stage = 'Listo'
                self.status = 'done'
            except Exception as exc:  # noqa: BLE001 - cualquier fallo se muestra al usuario
                self.error = friendly_error(exc)
                self.stage = 'Error'
                self.status = 'error'
                shutil.rmtree(self.dir, ignore_errors=True)
            finally:
                self.finished = time.time()

    def _find_output(self, info):
        for download in info.get('requested_downloads') or []:
            path = download.get('filepath')
            if path and os.path.isfile(path):
                return path
        files = [os.path.join(self.dir, f) for f in os.listdir(self.dir)
                 if not f.endswith(('.part', '.ytdl', '.temp'))]
        files = [f for f in files if os.path.isfile(f)]
        if not files:
            raise DownloadError('No se generó ningún archivo.')
        return max(files, key=os.path.getsize)


_download_slots = threading.BoundedSemaphore(MAX_CONCURRENT)
_jobs = {}
_jobs_lock = threading.Lock()


def cleanup_loop():
    while True:
        now = time.time()
        with _jobs_lock:
            expired = [j for j in _jobs.values() if j.finished and now - j.finished > JOB_TTL_SECONDS]
            for job in expired:
                del _jobs[job.id]
        for job in expired:
            shutil.rmtree(job.dir, ignore_errors=True)
        # Restos de ejecuciones anteriores del servidor.
        if os.path.isdir(DOWNLOAD_DIR):
            for name in os.listdir(DOWNLOAD_DIR):
                path = os.path.join(DOWNLOAD_DIR, name)
                if (JOB_ID_RE.match(name) and name not in _jobs and os.path.isdir(path)
                        and now - os.path.getmtime(path) > JOB_TTL_SECONDS):
                    shutil.rmtree(path, ignore_errors=True)
        time.sleep(300)


# --------------------------------------------------------------------------
# Rutas
# --------------------------------------------------------------------------

@app.get('/')
def index():
    return send_from_directory(STATIC_DIR, 'index.html')


@app.get('/api/status')
def api_status():
    js_runtime = next((name for name in ('deno', 'node', 'bun') if shutil.which(name)), None)
    return jsonify({'ffmpeg': bool(FFMPEG), 'yt_dlp': yt_dlp.version.__version__, 'js_runtime': js_runtime})


@app.post('/api/info')
def api_info():
    url = normalize_url((request.get_json(silent=True) or {}).get('url'))
    if not url:
        return error_response('Pega un enlace válido de YouTube.')
    opts = base_opts()
    opts['extract_flat'] = 'in_playlist'
    try:
        with yt_dlp.YoutubeDL(opts) as ydl:
            info = ydl.extract_info(url, download=False)
    except Exception as exc:  # noqa: BLE001
        return error_response(friendly_error(exc), 422)

    if info.get('_type') == 'playlist':
        return error_response('Eso es una lista de reproducción. Pega el enlace de un solo video.', 422)
    if info.get('live_status') in ('is_live', 'is_upcoming', 'post_live'):
        return error_response('No se pueden descargar transmisiones en vivo en curso o programadas.', 422)
    return jsonify(summarize_info(info))


@app.post('/api/download')
def api_download():
    data = request.get_json(silent=True) or {}

    video_id = data.get('id')
    if not isinstance(video_id, str) or not VIDEO_ID_RE.match(video_id):
        return error_response('Identificador de video inválido.')

    kind = data.get('kind')
    if kind not in ('video', 'audio'):
        return error_response('Tipo de descarga inválido.')

    try:
        start = parse_seconds(data.get('start'))
        end = parse_seconds(data.get('end'))
    except ValueError as exc:
        return error_response(str(exc))
    if start == 0:
        start = None
    if end is not None and end - (start or 0) < 1:
        return error_response('El recorte debe durar al menos 1 segundo.')

    params = {'id': video_id, 'kind': kind, 'start': start, 'end': end}
    if kind == 'video':
        quality = data.get('quality', 'best')
        if quality != 'best':
            try:
                quality = int(quality)
            except (TypeError, ValueError):
                return error_response('Calidad inválida.')
            if not 100 <= quality <= 8640:
                return error_response('Calidad inválida.')
        container = data.get('container', 'mp4')
        if container not in VIDEO_CONTAINERS:
            return error_response('Formato de video inválido.')
        params.update(quality=quality, container=container, precise=bool(data.get('precise', True)))
    else:
        audio_format = data.get('audio_format', 'mp3')
        audio_quality = str(data.get('audio_quality', '192'))
        if audio_format not in AUDIO_FORMATS or audio_quality not in AUDIO_BITRATES:
            return error_response('Formato de audio inválido.')
        params.update(audio_format=audio_format, audio_quality=audio_quality)

    if not FFMPEG:
        return error_response('ffmpeg no está instalado en el servidor.', 503)

    job = Job(params)
    with _jobs_lock:
        _jobs[job.id] = job
    threading.Thread(target=job.run, daemon=True).start()
    return jsonify(job.to_dict()), 202


def get_job(job_id):
    if not JOB_ID_RE.match(job_id):
        abort(404)
    with _jobs_lock:
        job = _jobs.get(job_id)
    if not job:
        abort(404)
    return job


@app.get('/api/jobs/<job_id>')
def api_job(job_id):
    return jsonify(get_job(job_id).to_dict())


@app.get('/api/jobs/<job_id>/file')
def api_job_file(job_id):
    job = get_job(job_id)
    if job.status != 'done' or not job.file or not os.path.isfile(job.file):
        abort(404)
    return send_file(job.file, as_attachment=True, download_name=job.filename)


@app.errorhandler(404)
def not_found(_):
    return error_response('No encontrado.', 404)


threading.Thread(target=cleanup_loop, daemon=True).start()

if __name__ == '__main__':
    host = os.environ.get('HOST', '127.0.0.1')
    port = int(os.environ.get('PORT', '5000'))
    print(f'Abre http://{"localhost" if host in ("0.0.0.0", "127.0.0.1") else host}:{port} en tu navegador')
    if not FFMPEG:
        print('AVISO: ffmpeg no está instalado; no se podrá unir, recortar ni convertir.')
    app.run(host=host, port=port, threaded=True)
