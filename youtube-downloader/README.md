# Descargador de YouTube

Página web para **descargar**, **recortar** y **elegir la calidad** de videos de YouTube.
Funciona en tu propio ordenador: el servidor (Python + [yt-dlp](https://github.com/yt-dlp/yt-dlp) + ffmpeg)
hace las descargas y la página te deja elegir qué bajar.

## Qué puedes hacer

- **Ver el video** dentro de la página antes de descargarlo.
- **Recortar** un fragmento:
  - con la barra de dos tiradores,
  - escribiendo los tiempos (`90`, `1:30`, `1:02:03.5`),
  - o con el botón **Momento actual**, que toma el tiempo del reproductor.
  - **Previsualizar recorte** reproduce solo el fragmento elegido.
  - **Corte preciso** corta en el fotograma exacto, pero vuelve a codificar el video y tarda más. Si lo desactivas, el corte es casi instantáneo, aunque puede empezar unos segundos antes, en el fotograma clave anterior.
- **Elegir la calidad del video**: se listan todas las resoluciones disponibles (144p … 4K/8K, incluido 60 fps) con el tamaño aproximado.
- **Elegir el formato del video**:
  - **MP4**: prefiere H.264 + AAC, que se reproduce en cualquier sitio.
  - **WEBM**: VP9/AV1 + Opus.
  - **MKV**: la mejor calidad, sea cual sea el códec.
- **Solo audio** en MP3, M4A, OPUS, WAV o FLAC, con bitrate de 96 a 320 kbps.
- Varias descargas a la vez, con progreso, velocidad y tiempo restante. El archivo se guarda solo cuando termina.

## Requisitos

- **Python 3.10 o superior**
- **ffmpeg**, necesario para unir video y audio, recortar y convertir:

  | Sistema | Comando |
  |---|---|
  | Windows | `winget install Gyan.FFmpeg` |
  | macOS | `brew install ffmpeg` |
  | Debian/Ubuntu | `sudo apt install ffmpeg` |

## Instalación y uso

```bash
cd youtube-downloader
python -m venv .venv
source .venv/bin/activate        # En Windows: .venv\Scripts\activate
pip install -r requirements.txt
python app.py
```

Después abre **http://localhost:5000** en el navegador, pega el enlace de un video y pulsa **Buscar**.

> Al pegar un enlace en el cuadro, la búsqueda arranca sola. También puedes abrir
> `http://localhost:5000/?url=ENLACE_DEL_VIDEO` directamente, por ejemplo desde un marcador.

### Con Docker

```bash
cd youtube-downloader
docker build -t yt-downloader .
docker run --rm -p 5000:5000 yt-downloader
```

## Programa para Windows

**[⬇ Descargar DescargadorSetup.exe](https://github.com/ivanlruiz/Apps/releases/latest/download/DescargadorSetup.exe)**

1. Abre el instalador. No pide permisos de administrador.
2. Si aparece **"Windows protegió tu PC"**, toca **Más información → Ejecutar de todas formas**.
   El aviso sale con cualquier programa que no tenga una firma digital de pago.
3. El Descargador se abre en el navegador. Después puedes abrirlo desde el acceso directo del
   escritorio o desde el menú Inicio.

- Lleva todo dentro: Python, ffmpeg y deno, el intérprete de JavaScript que YouTube necesita.
- Se abre una ventana negra que hay que dejar abierta mientras lo usas. Para salir, ciérrala.
- yt-dlp se actualiza solo una vez al día. La versión nueva se guarda en
  `%LOCALAPPDATA%\Descargador` y se usa a partir del siguiente arranque.

El ejecutable lo crea GitHub Actions en Windows con [PyInstaller](https://pyinstaller.org)
(`windows/descargador.spec`) e [Inno Setup](https://jrsoftware.org/isinfo.php) (`windows/installer.iss`).
El punto de entrada es `desktop.py`.

## App de Android

**[⬇ Descargar el APK](https://github.com/ivanlruiz/Apps/releases/latest/download/Descargador-arm64-v8a.apk)**
sirve para la mayoría de los celulares. Para otros modelos, en
[la página de versiones](https://github.com/ivanlruiz/Apps/releases/latest) están `armeabi-v7a`, para
celulares antiguos, y `x86_64`.

- Funciona entera en el teléfono, sin servidor. Lleva dentro yt-dlp, Python, ffmpeg y QuickJS, gracias a
  [youtubedl-android](https://github.com/JunkFood02/youtubedl-android).
- Tiene la misma interfaz que la versión web: recortar, elegir calidad y formato, y descargar solo el audio.
- Desde la app de YouTube puedes usar **Compartir → Descargador**.
- Los archivos se guardan en **Descargas/Descargador**, con botones para abrirlos o compartirlos.
- Sigue descargando en segundo plano y avisa con una notificación al terminar.
- yt-dlp se actualiza solo una vez al día. También puedes forzarlo con **Buscar actualización de yt-dlp**.

La app no está en Google Play porque Play no admite apps que descargan videos de YouTube. Se distribuye
como APK desde *Releases*.

### Publicar una versión nueva

GitHub Actions ([`.github/workflows/android.yml`](../.github/workflows/android.yml)) compila los APK y
el instalador de Windows con cada cambio en `youtube-downloader/`. Después publica los dos juntos en una
versión de *Releases*. También puedes lanzarlo a mano en **Actions → Compilar y publicar → Run workflow**.

La clave de firma está cifrada en `android/release.jks.enc`. Para descifrarla, el workflow necesita el
secreto `ANDROID_KEYSTORE_PASSWORD`, que se crea en **Settings → Secrets and variables → Actions**.
Sin ese secreto, el APK se firma con una clave de prueba y solo queda como artefacto de Actions, sin
publicarse.

Si prefieres usar otra clave, crea también el secreto `ANDROID_KEYSTORE_BASE64` con el `.jks` codificado en
base64; tiene prioridad sobre el archivo cifrado. **Guarda la contraseña en un lugar seguro**: sin ella,
las versiones nuevas no se podrán instalar encima de las anteriores.

### Compilar en tu ordenador

Necesitas JDK 17 o superior y el Android SDK (variable `ANDROID_HOME`):

```bash
cd youtube-downloader/android
./gradlew testReleaseUnitTest assembleRelease
# APKs en app/build/outputs/apk/release/
```

## En el celular sin instalar la app

### Android (con Termux)

La aplicación funciona entera en el propio teléfono:

1. Instala **Termux** desde [F-Droid](https://f-droid.org/packages/com.termux/). La versión de Play Store
   puede estar desactualizada.
2. Abre Termux, pega este comando y pulsa Enter:

   ```bash
   curl -fsSL https://raw.githubusercontent.com/ivanlruiz/Apps/HEAD/youtube-downloader/termux.sh | bash
   ```

   El comando instala Python, ffmpeg y Node.js, descarga la aplicación y abre `http://localhost:5000`
   en el navegador. La primera vez tarda unos minutos.
3. La próxima vez abre Termux y escribe `descargador`.

- Deja Termux abierto mientras descargas. Los archivos se guardan en la carpeta **Descargas** del teléfono.
- **Corte preciso** vuelve a codificar el video y en un teléfono puede ir lento. Para clips largos,
  desactívalo.
- Para actualizar la app y yt-dlp, vuelve a ejecutar el comando del paso 2.

### iPhone (o cualquier celular, usando tu ordenador)

En iPhone no existe Termux. Arranca la aplicación en tu ordenador permitiendo conexiones de la red local:

```bash
HOST=0.0.0.0 python app.py          # Windows (cmd): set HOST=0.0.0.0 && python app.py
```

Después abre `http://IP-DEL-ORDENADOR:5000` en el navegador del celular, conectado a la misma wifi.
Para ver la IP del ordenador, usa `ipconfig` en Windows o `ipconfig getifaddr en0` en Mac.

## Configuración (variables de entorno)

| Variable | Por defecto | Para qué sirve |
|---|---|---|
| `PORT` | `5000` | Puerto del servidor. |
| `HOST` | `127.0.0.1` | Usa `0.0.0.0` para abrir la página desde otros dispositivos de tu red. |
| `DOWNLOAD_DIR` | carpeta temporal del sistema | Dónde se guardan los archivos mientras los descargas. |
| `JOB_TTL_MINUTES` | `60` | Minutos que se conserva cada archivo en el servidor antes de borrarse. |
| `MAX_CONCURRENT_DOWNLOADS` | `2` | Descargas simultáneas; las demás esperan en cola. |
| `YTDLP_COOKIES` | — | Ruta a un archivo `cookies.txt` (ver abajo). |

## Problemas frecuentes

**“YouTube pide confirmar que no eres un bot”** o **video con restricción de edad**
YouTube a veces bloquea las peticiones anónimas. Para solucionarlo, exporta las cookies de tu navegador
en formato `cookies.txt`. Puedes hacerlo con una extensión del tipo “Get cookies.txt LOCALLY”, con la
sesión de YouTube iniciada. Después arranca el servidor así:

```bash
YTDLP_COOKIES=/ruta/a/cookies.txt python app.py
```

Más detalles en la [guía de yt-dlp](https://github.com/yt-dlp/yt-dlp/wiki/Extractors#exporting-youtube-cookies).

**Dejó de funcionar de un día para otro**
YouTube cambia a menudo. Casi siempre se arregla actualizando yt-dlp:

```bash
pip install -U "yt-dlp[default,deno]"
```

**Aviso “ffmpeg no está instalado”**
Instala ffmpeg (ver *Requisitos*), comprueba que el comando `ffmpeg -version` funciona en una terminal
nueva y reinicia `python app.py`.

## Cómo funciona

```
Navegador (static/)                       Servidor (app.py)
───────────────────                       ─────────────────
pegar enlace ── POST /api/info ─────────▶ yt-dlp lee título, duración y formatos
elegir recorte/calidad
Descargar ───── POST /api/download ─────▶ crea un trabajo en segundo plano:
                                            yt-dlp elige el formato, ffmpeg descarga
                                            solo el fragmento, une y convierte
progreso ────── GET /api/jobs/<id> ─────▶ estado, %, velocidad, tiempo restante
guardar ─────── GET /api/jobs/<id>/file ▶ envía el archivo con el nombre del video
```

- El recorte usa la opción `download_ranges` de yt-dlp. ffmpeg solo descarga el fragmento pedido,
  así que recortar 30 segundos de un video de 3 horas es rápido.
- Los archivos se guardan en una carpeta temporal y se borran solos pasado `JOB_TTL_MINUTES`.

## Pruebas

```bash
pip install pytest
python -m pytest tests
```

Las pruebas no se conectan a YouTube. Generan videos de prueba con ffmpeg, los sirven desde un servidor
local y comprueban el recorte, las calidades y los formatos de video y de audio.

## Aviso

Usa esta herramienta solo con videos propios, con licencia libre (por ejemplo, Creative Commons) o con
permiso de su autor. Respeta los derechos de autor y las
[condiciones de uso de YouTube](https://www.youtube.com/t/terms).
