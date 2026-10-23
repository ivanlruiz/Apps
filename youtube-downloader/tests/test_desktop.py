"""Pruebas del arranque de escritorio (desktop.py): elección y actualización de yt-dlp."""

import functools
import http.server
import os
import sys
import threading
import zipfile

import pytest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import desktop  # noqa: E402


def make_ytdlp(path, version):
    """Crea un archivo con la misma forma que el yt-dlp publicado: un zip con cabecera de script."""
    with open(path, 'wb') as f:
        f.write(b'#!/usr/bin/env python3\n')
        with zipfile.ZipFile(f, 'w') as zf:
            zf.writestr('yt_dlp/__init__.py', '')
            zf.writestr('yt_dlp/version.py', f"__version__ = '{version}'\n")
    return path


def test_zip_version(tmp_path):
    assert desktop.zip_version(make_ytdlp(tmp_path / 'yt-dlp', '2099.01.02')) == '2099.01.02'
    (tmp_path / 'roto').write_text('no es un zip')
    assert desktop.zip_version(tmp_path / 'roto') is None


def test_pick_ytdlp_prefers_newest_download(tmp_path):
    for version in ('2026.01.01', '2026.11.30', '2026.12.24'):
        make_ytdlp(tmp_path / f'yt-dlp-{version}.zip', version)
    chosen = desktop.pick_ytdlp(str(tmp_path), '2026.10.21')
    assert os.path.basename(chosen) == 'yt-dlp-2026.12.24.zip'
    assert sorted(os.listdir(tmp_path)) == ['yt-dlp-2026.12.24.zip']  # las viejas se borran


def test_pick_ytdlp_keeps_bundled_when_newer(tmp_path):
    make_ytdlp(tmp_path / 'yt-dlp-2026.01.01.zip', '2026.01.01')
    assert desktop.pick_ytdlp(str(tmp_path), '2026.10.21') is None
    assert os.listdir(tmp_path) == []
    assert desktop.pick_ytdlp(str(tmp_path / 'no-existe'), '2026.10.21') is None


@pytest.fixture
def release_server(tmp_path):
    root = tmp_path / 'release'
    root.mkdir()

    class Quiet(http.server.SimpleHTTPRequestHandler):
        def log_message(self, *args):
            pass

    server = http.server.ThreadingHTTPServer(('127.0.0.1', 0), functools.partial(Quiet, directory=str(root)))
    threading.Thread(target=server.serve_forever, daemon=True).start()
    yield root, f'http://127.0.0.1:{server.server_address[1]}/yt-dlp'
    server.shutdown()


def test_update_downloads_newer_version(tmp_path, release_server):
    root, url = release_server
    make_ytdlp(root / 'yt-dlp', '2099.01.02')
    data = tmp_path / 'data'
    path = desktop.update_ytdlp(str(data), '2026.10.21', url=url)
    assert os.path.basename(path) == 'yt-dlp-2099.01.02.zip'
    assert desktop.zip_version(path) == '2099.01.02'
    # Ya se comprobó hoy: no vuelve a descargar.
    assert desktop.update_ytdlp(str(data), '2026.10.21', url=url) is None


def test_update_skips_same_or_older_version(tmp_path, release_server):
    root, url = release_server
    make_ytdlp(root / 'yt-dlp', '2026.10.21')
    data = tmp_path / 'data'
    assert desktop.update_ytdlp(str(data), '2026.10.21', url=url) is None
    assert sorted(os.listdir(data)) == ['last-update-check']


def test_update_rejects_non_ytdlp_file(tmp_path, release_server):
    root, url = release_server
    (root / 'yt-dlp').write_text('<html>error</html>')
    with pytest.raises(ValueError):
        desktop.update_ytdlp(str(tmp_path / 'data'), '2026.10.21', url=url)
    assert not (tmp_path / 'data' / 'last-update-check').exists()
