#!/data/data/com.termux/files/usr/bin/bash
# Instala (o actualiza) el descargador en Android con Termux y lo arranca.
#
#   curl -fsSL https://raw.githubusercontent.com/ivanlruiz/Apps/HEAD/youtube-downloader/termux.sh | bash
#
# Después, para volver a abrirlo basta con escribir: descargador
#
# Todo va dentro de main() para que bash lea el script completo antes de
# ejecutarlo (al usar "curl | bash" la entrada estándar es el propio script).

main() {
  set -e
  local repo_dir="$HOME/Apps"

  echo "==> Actualizando Termux…"
  yes | pkg upgrade -y

  echo "==> Instalando Python, ffmpeg, Node.js y git…"
  pkg install -y python python-pip ffmpeg nodejs git

  echo "==> Instalando Flask y yt-dlp…"
  pip install --upgrade flask yt-dlp mutagen

  echo "==> Descargando la aplicación…"
  if [ -d "$repo_dir/.git" ]; then
    git -C "$repo_dir" pull --ff-only
  else
    git clone https://github.com/ivanlruiz/Apps "$repo_dir"
  fi

  # Atajo para arrancarla otra vez: comando "descargador".
  cat > "$PREFIX/bin/descargador" <<EOF
#!/data/data/com.termux/files/usr/bin/bash
termux-wake-lock 2>/dev/null || true
trap 'termux-wake-unlock 2>/dev/null || true' EXIT
(sleep 3 && termux-open-url http://localhost:5000) &
cd "$repo_dir/youtube-downloader" && python app.py
EOF
  chmod +x "$PREFIX/bin/descargador"

  echo
  echo "==> Listo. Se abrirá http://localhost:5000 en el navegador."
  echo "    Deja Termux abierto mientras descargas. Para pararlo: CTRL + C."
  echo "    La próxima vez solo escribe: descargador"
  echo
  exec "$PREFIX/bin/descargador" < /dev/tty
}

main "$@"
