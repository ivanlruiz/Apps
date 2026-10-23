"""Pruebas del servidor.

No se conectan a YouTube: las pruebas de descarga sirven videos generados con
ffmpeg desde un servidor HTTP local y sustituyen la extracción de yt-dlp por
una ficha de video falsa con formatos separados de video y audio, igual que
los que ofrece YouTube.
"""

import functools
import http.server
import json
import os
import shutil
import subprocess
import sys
import threading
import time

import pytest
import yt_dlp

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import app as appmod  # noqa: E402

VIDEO_ID = 'abcdefghijk'
HAS_FFMPEG = bool(shutil.which('ffmpeg') and shutil.which('ffprobe'))


@pytest.fixture
def client(tmp_path, monkeypatch):
    monkeypatch.setattr(appmod, 'DOWNLOAD_DIR', str(tmp_path / 'downloads'))
    appmod.app.config['TESTING'] = True
    return appmod.app.test_client()


# --------------------------------------------------------------------------
# Unidades
# --------------------------------------------------------------------------

@pytest.mark.parametrize('url', [
    'https://www.youtube.com/watch?v=dQw4w9WgXcQ',
    'youtube.com/watch?v=dQw4w9WgXcQ',
    'https://youtu.be/dQw4w9WgXcQ?t=10',
    'https://m.youtube.com/shorts/dQw4w9WgXcQ',
    'https://music.youtube.com/watch?v=dQw4w9WgXcQ',
])
def test_normalize_url_accepts_youtube(url):
    assert appmod.normalize_url(url)


@pytest.mark.parametrize('url', [
    '', None, 'hola', 'https://example.com/watch?v=x', 'file:///etc/passwd',
    'https://youtube.com.evil.com/watch', 'ftp://youtube.com/x',
])
def test_normalize_url_rejects_others(url):
    assert appmod.normalize_url(url) is None


def test_summarize_info_lists_qualities():
    info = {
        'id': VIDEO_ID, 'title': 'T', 'duration': 100,
        'formats': [
            {'format_id': 'sb', 'vcodec': 'none', 'acodec': 'none', 'ext': 'mhtml'},
            {'format_id': 'a', 'vcodec': 'none', 'acodec': 'opus', 'filesize': 1_000},
            {'format_id': '1080', 'vcodec': 'vp9', 'acodec': 'none', 'width': 1920, 'height': 1080,
             'fps': 60, 'filesize': 10_000},
            {'format_id': '1080b', 'vcodec': 'avc1', 'acodec': 'none', 'width': 1920, 'height': 1080,
             'fps': 30, 'tbr': 0.5},  # ≈ 6 KB, menor que el VP9
            {'format_id': '360', 'vcodec': 'avc1', 'acodec': 'mp4a', 'width': 640, 'height': 360,
             'fps': 30, 'filesize_approx': 3_000},
            {'format_id': 'short', 'vcodec': 'vp9', 'acodec': 'none', 'width': 720, 'height': 1280},
            {'format_id': 'drm', 'vcodec': 'vp9', 'acodec': 'none', 'height': 2160, 'has_drm': True},
        ],
    }
    summary = appmod.summarize_info(info)
    assert [q['label'] for q in summary['qualities']] == ['1080p60', '720p', '360p']
    assert summary['qualities'][0] == {'res': 1080, 'fps': 60, 'label': '1080p60', 'badge': 'HD', 'size': 11_000}
    assert summary['qualities'][2]['size'] == 3_000  # ya incluye audio
    assert summary['audio_size'] == 1_000


def test_friendly_error_bot_check():
    msg = appmod.friendly_error(Exception("ERROR: [youtube] x: Sign in to confirm you're not a bot."))
    assert 'cookies' in msg


# --------------------------------------------------------------------------
# Validación de la API
# --------------------------------------------------------------------------

def test_info_rejects_non_youtube(client):
    res = client.post('/api/info', json={'url': 'https://example.com/video'})
    assert res.status_code == 400
    assert 'YouTube' in res.get_json()['error']


@pytest.mark.parametrize('body', [
    {'id': 'bad', 'kind': 'video'},
    {'id': VIDEO_ID, 'kind': 'gif'},
    {'id': VIDEO_ID, 'kind': 'video', 'container': 'avi'},
    {'id': VIDEO_ID, 'kind': 'video', 'quality': 'ultra'},
    {'id': VIDEO_ID, 'kind': 'video', 'start': 10, 'end': 10.5},
    {'id': VIDEO_ID, 'kind': 'video', 'start': -1},
    {'id': VIDEO_ID, 'kind': 'audio', 'audio_format': 'aiff'},
    {'id': VIDEO_ID, 'kind': 'audio', 'audio_format': 'mp3', 'audio_quality': '999'},
])
def test_download_rejects_invalid_params(client, body):
    assert client.post('/api/download', json=body).status_code == 400


def test_unknown_job_is_404(client):
    assert client.get('/api/jobs/' + '0' * 32).status_code == 404
    assert client.get('/api/jobs/../../etc/passwd').status_code == 404


# --------------------------------------------------------------------------
# Descargas de extremo a extremo (sin YouTube)
# --------------------------------------------------------------------------

class QuietHandler(http.server.SimpleHTTPRequestHandler):
    def log_message(self, *args):
        pass


@pytest.fixture(scope='module')
def media_server(tmp_path_factory):
    if not HAS_FFMPEG:
        pytest.skip('ffmpeg no está instalado')
    root = tmp_path_factory.mktemp('media')
    ff = ['ffmpeg', '-v', 'error', '-y']
    # Fotogramas clave cada 4 s para comprobar el corte preciso.
    h264 = ['-c:v', 'libx264', '-preset', 'ultrafast', '-g', '120', '-pix_fmt', 'yuv420p',
            '-movflags', '+faststart']
    jobs = [
        ['-f', 'lavfi', '-i', 'testsrc2=size=1280x720:rate=30:duration=20', *h264, 'v720.mp4'],
        ['-f', 'lavfi', '-i', 'testsrc2=size=640x360:rate=30:duration=20', *h264, 'v360.mp4'],
        ['-f', 'lavfi', '-i', 'testsrc2=size=640x360:rate=30:duration=20', '-c:v', 'libvpx-vp9',
         '-deadline', 'realtime', '-cpu-used', '8', '-b:v', '300k', '-g', '120', 'v360.webm'],
        ['-f', 'lavfi', '-i', 'sine=frequency=440:duration=20', '-c:a', 'aac', '-b:a', '128k',
         '-movflags', '+faststart', 'a.m4a'],
        ['-f', 'lavfi', '-i', 'sine=frequency=440:duration=20', '-c:a', 'libopus', '-b:a', '96k', 'a.webm'],
    ]
    for args in jobs:
        subprocess.run([*ff, *args[:-1], str(root / args[-1])], check=True)

    handler = functools.partial(QuietHandler, directory=str(root))
    server = http.server.ThreadingHTTPServer(('127.0.0.1', 0), handler)
    threading.Thread(target=server.serve_forever, daemon=True).start()
    yield f'http://127.0.0.1:{server.server_address[1]}'
    server.shutdown()


@pytest.fixture
def fake_youtube(media_server, monkeypatch):
    def fmt(format_id, name, ext, vcodec, acodec, height=None, tbr=None):
        f = {'format_id': format_id, 'url': f'{media_server}/{name}', 'ext': ext, 'protocol': 'http',
             'vcodec': vcodec, 'acodec': acodec, 'tbr': tbr}
        if height:
            f.update(width=height * 16 // 9, height=height, fps=30)
        return f

    info = {
        'id': VIDEO_ID,
        'title': 'Prueba: clip/raro?',
        'duration': 20,
        'webpage_url': f'https://www.youtube.com/watch?v={VIDEO_ID}',
        'extractor': 'youtube',
        'extractor_key': 'Youtube',
        'formats': [
            fmt('140', 'a.m4a', 'm4a', 'none', 'mp4a.40.2', tbr=128),
            fmt('251', 'a.webm', 'webm', 'none', 'opus', tbr=96),
            fmt('134', 'v360.mp4', 'mp4', 'avc1.4d401e', 'none', 360, 300),
            fmt('243', 'v360.webm', 'webm', 'vp9', 'none', 360, 300),
            fmt('136', 'v720.mp4', 'mp4', 'avc1.4d401f', 'none', 720, 900),
        ],
    }

    def extract_info(self, url, download=True, **kwargs):
        assert url == f'https://www.youtube.com/watch?v={VIDEO_ID}'
        return self.process_ie_result(dict(info, formats=[dict(f) for f in info['formats']]),
                                      download=download)

    monkeypatch.setattr(yt_dlp.YoutubeDL, 'extract_info', extract_info)


def probe(path):
    out = subprocess.run(
        ['ffprobe', '-v', 'error', '-show_entries', 'format=duration:stream=codec_type,codec_name,height',
         '-of', 'json', path], capture_output=True, check=True, text=True).stdout
    data = json.loads(out)
    streams = {s['codec_type']: s for s in data['streams']}
    return float(data['format']['duration']), streams


def run_download(client, body):
    res = client.post('/api/download', json={'id': VIDEO_ID, **body})
    assert res.status_code == 202, res.get_json()
    job_id = res.get_json()['id']
    deadline = time.time() + 120
    while time.time() < deadline:
        job = client.get(f'/api/jobs/{job_id}').get_json()
        if job['status'] in ('done', 'error'):
            break
        time.sleep(0.2)
    assert job['status'] == 'done', job
    file_res = client.get(f'/api/jobs/{job_id}/file')
    assert file_res.status_code == 200
    assert 'attachment' in file_res.headers['Content-Disposition']
    file_res.close()
    return job, appmod._jobs[job_id].file


def test_video_mp4_trimmed_precise(client, fake_youtube):
    job, path = run_download(client, {'kind': 'video', 'quality': 720, 'container': 'mp4',
                                      'start': 5, 'end': 12, 'precise': True})
    duration, streams = probe(path)
    assert job['filename'].endswith('(00m05s-00m12s).mp4')
    assert abs(duration - 7) < 0.3
    assert streams['video']['height'] == 720
    assert streams['video']['codec_name'] == 'h264'
    assert streams['audio']['codec_name'] == 'aac'


def test_video_mkv_trimmed_fast_snaps_to_keyframe(client, fake_youtube):
    _, path = run_download(client, {'kind': 'video', 'quality': 'best', 'container': 'mkv',
                                    'start': 5, 'end': 12, 'precise': False})
    duration, streams = probe(path)
    assert path.endswith('.mkv')
    assert 6.5 < duration < 9  # empieza en el fotograma clave de los 4 s
    assert streams['video']['height'] == 720


def test_video_webm_full_lower_quality(client, fake_youtube):
    job, path = run_download(client, {'kind': 'video', 'quality': 360, 'container': 'webm'})
    duration, streams = probe(path)
    assert job['filename'] == 'Prueba： clip⧸raro？.webm'
    assert abs(duration - 20) < 0.5
    assert streams['video']['codec_name'] == 'vp9'
    assert streams['video']['height'] == 360
    assert streams['audio']['codec_name'] == 'opus'


def test_audio_mp3_trimmed(client, fake_youtube):
    job, path = run_download(client, {'kind': 'audio', 'audio_format': 'mp3', 'audio_quality': '192',
                                      'start': 3, 'end': 9})
    duration, streams = probe(path)
    assert job['filename'].endswith('(00m03s-00m09s).mp3')
    assert abs(duration - 6) < 0.3
    assert 'video' not in streams
    assert streams['audio']['codec_name'] == 'mp3'


def test_audio_wav_full(client, fake_youtube):
    _, path = run_download(client, {'kind': 'audio', 'audio_format': 'wav', 'audio_quality': '320'})
    duration, streams = probe(path)
    assert path.endswith('.wav')
    assert abs(duration - 20) < 0.5
    assert streams['audio']['codec_name'] == 'pcm_s16le'
