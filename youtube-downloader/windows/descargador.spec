# -*- mode: python -*-
# Empaqueta la versión de escritorio:  pyinstaller --noconfirm windows/descargador.spec
# Antes hay que dejar en windows/bin/ las herramientas que van dentro:
# ffmpeg, ffprobe (con sus DLL) y deno. El workflow de GitHub lo hace solo.
import os

from PyInstaller.utils.hooks import copy_metadata

ROOT = os.path.abspath(os.path.join(SPECPATH, '..'))
BIN = os.path.join(SPECPATH, 'bin')

a = Analysis(
    [os.path.join(ROOT, 'desktop.py')],
    pathex=[ROOT],
    hiddenimports=['app'],
    datas=[
        (os.path.join(ROOT, 'static'), 'static'),
        (BIN, 'bin'),
        *copy_metadata('yt-dlp'),  # desktop.py lee de aquí la versión incluida
    ],
)
pyz = PYZ(a.pure)
exe = EXE(
    pyz,
    a.scripts,
    [],
    exclude_binaries=True,
    name='Descargador',
    icon=os.path.join(SPECPATH, 'icon.ico'),
    console=True,
)
coll = COLLECT(exe, a.binaries, a.datas, name='Descargador')
