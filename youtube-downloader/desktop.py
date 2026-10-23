"""Versión de escritorio (Descargador.exe): arranca el servidor y abre el navegador.

El ejecutable lleva dentro ffmpeg, ffprobe y deno (ver windows/descargador.spec).
yt-dlp se actualiza solo una vez al día: la versión nueva se guarda en la carpeta
de datos del usuario y se usa en lugar de la incluida desde el siguiente arranque.
"""

import importlib.metadata
import logging
import os
import re
import shutil
import sys
import threading
import time
import urllib.request
import webbrowser
import zipfile

BASE_DIR = getattr(sys, '_MEIPASS', os.path.dirname(os.path.abspath(__file__)))
DATA_DIR = os.path.join(os.environ.get('LOCALAPPDATA') or os.path.expanduser('~/.local/share'), 'Descargador')
LATEST_YTDLP_URL = 'https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp'
UPDATE_INTERVAL = 24 * 60 * 60
OVERRIDE_RE = re.compile(r'^yt-dlp-(\d+(?:\.\d+)*)\.zip$')
VERSION_RE = re.compile(r"""__version__\s*=\s*['"]([^'"]+)['"]""")

HOST = '127.0.0.1'
PORT = int(os.environ.get('PORT', '5000'))


def version_tuple(version):
    return tuple(int(part) for part in re.findall(r'\d+', version or ''))


def zip_version(path):
    """Versión de yt-dlp dentro de un archivo como el que publica yt-dlp (un zip ejecutable)."""
    try:
        with zipfile.ZipFile(path) as zf:
            return VERSION_RE.search(zf.read('yt_dlp/version.py').decode()).group(1)
    except (OSError, KeyError, AttributeError, zipfile.BadZipFile):
        return None


def pick_ytdlp(data_dir, bundled_version):
    """Devuelve el yt-dlp descargado más nuevo si supera al incluido; borra los que sobran."""
    if not os.path.isdir(data_dir):
        return None
    found = sorted(
        (version_tuple(m.group(1)), os.path.join(data_dir, name))
        for name in os.listdir(data_dir)
        if (m := OVERRIDE_RE.match(name))
    )
    best = found[-1] if found and found[-1][0] > version_tuple(bundled_version) else None
    for version, path in found:
        if best is None or path != best[1]:
            try:
                os.remove(path)
            except OSError:
                pass
    return best[1] if best else None


def update_ytdlp(data_dir, current_version, url=LATEST_YTDLP_URL, interval=UPDATE_INTERVAL):
    """Descarga la última versión de yt-dlp si es más nueva. Devuelve su ruta, o None."""
    stamp = os.path.join(data_dir, 'last-update-check')
    try:
        if time.time() - os.path.getmtime(stamp) < interval:
            return None
    except OSError:
        pass
    os.makedirs(data_dir, exist_ok=True)
    part = os.path.join(data_dir, 'yt-dlp.download')
    try:
        with urllib.request.urlopen(url, timeout=120) as res, open(part, 'wb') as out:
            shutil.copyfileobj(res, out)
        latest = zip_version(part)
        if latest is None:
            raise ValueError('el archivo descargado no es yt-dlp')
        with open(stamp, 'w'):
            pass
        if version_tuple(latest) <= version_tuple(current_version):
            return None
        # Nombre nuevo para no tocar el archivo que se está usando ahora.
        target = os.path.join(data_dir, f'yt-dlp-{latest}.zip')
        os.replace(part, target)
        return target
    finally:
        if os.path.exists(part):
            os.remove(part)


def prepare_environment():
    """Prepara PATH y yt-dlp. Debe ejecutarse antes de importar app o yt_dlp."""
    os.environ['PATH'] = os.path.join(BASE_DIR, 'bin') + os.pathsep + os.environ.get('PATH', '')
    try:
        bundled = importlib.metadata.version('yt-dlp')
    except importlib.metadata.PackageNotFoundError:
        bundled = '0'
    override = pick_ytdlp(DATA_DIR, bundled)
    if override:
        sys.path.insert(0, override)


def already_running(port):
    try:
        with urllib.request.urlopen(f'http://{HOST}:{port}/api/status', timeout=2) as res:
            return b'yt_dlp' in res.read()
    except OSError:
        return False


def background_update(current_version):
    try:
        path = update_ytdlp(DATA_DIR, current_version)
    except Exception as exc:  # noqa: BLE001 - sin conexión, GitHub caído…
        print(f'No se pudo buscar actualizaciones de yt-dlp: {exc}')
        return
    if path:
        print(f'Se descargó yt-dlp {zip_version(path)}: se usará la próxima vez que abras el Descargador.')


def main():
    sys.stdout.reconfigure(line_buffering=True)
    prepare_environment()
    import app  # noqa: E402 - después de prepare_environment(), para que use ffmpeg y yt-dlp correctos
    from werkzeug.serving import make_server

    # La consola es para el usuario: sin una línea por cada petición.
    logging.getLogger('werkzeug').setLevel(logging.ERROR)

    if already_running(PORT):
        webbrowser.open(f'http://{HOST}:{PORT}')
        return
    try:
        server = make_server(HOST, PORT, app.app, threaded=True)
    except OSError:
        server = make_server(HOST, 0, app.app, threaded=True)  # puerto ocupado: usar otro libre
    url = f'http://{HOST}:{server.server_port}'

    if os.name == 'nt':
        import ctypes
        ctypes.windll.kernel32.SetConsoleTitleW('Descargador')
    print('Descargador de YouTube')
    print(f'Abierto en {url}  (yt-dlp {app.yt_dlp.version.__version__})')
    print('Deja esta ventana abierta mientras lo uses. Para salir, ciérrala.')
    if not app.FFMPEG:
        print('AVISO: no se encontró ffmpeg; no se podrá unir, recortar ni convertir.')

    if not os.environ.get('DESCARGADOR_NO_BROWSER'):
        threading.Timer(1.0, webbrowser.open, [url]).start()
    threading.Thread(target=background_update, args=(app.yt_dlp.version.__version__,), daemon=True).start()
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass


if __name__ == '__main__':
    main()
