package io.github.ivanlruiz.descargador

import org.json.JSONArray
import org.json.JSONObject
import java.net.URI
import java.util.Locale
import kotlin.math.floor
import kotlin.math.max
import kotlin.math.min

/** Error que se devuelve a la interfaz con su código HTTP equivalente. */
class ApiError(val status: Int, message: String) : Exception(message)

data class DownloadParams(
    val id: String,
    val kind: String,
    val start: Double?,
    val end: Double?,
    val quality: Int? = null, // null = la mejor disponible
    val container: String = "mp4",
    val precise: Boolean = true,
    val audioFormat: String = "mp3",
    val audioQuality: String = "192",
) {
    val trimmed get() = start != null || end != null
}

/**
 * Lógica de la descarga sin dependencias de Android, equivalente a app.py de la
 * versión web: validar enlaces y parámetros, resumir la información del video y
 * construir los argumentos de yt-dlp.
 */
object Summary {
    private val YOUTUBE_HOST = Regex("^(?:[a-z0-9-]+\\.)?(?:youtube\\.com|youtube-nocookie\\.com|youtu\\.be)$")
    private val VIDEO_ID = Regex("^[A-Za-z0-9_-]{11}$")
    private val VIDEO_CONTAINERS = listOf("mp4", "webm", "mkv")
    private val AUDIO_FORMATS = listOf("mp3", "m4a", "opus", "wav", "flac")
    private val AUDIO_BITRATES = listOf("320", "256", "192", "128", "96")
    private val LOSSY_AUDIO = listOf("mp3", "m4a", "opus")

    // Codificadores usados cuando se pide un corte preciso (requiere recodificar el video).
    private const val H264_CUT = "-c:v libx264 -preset veryfast -crf 18 -c:a aac -b:a 192k"
    private val PRECISE_CUT_ARGS = mapOf(
        "mp4" to H264_CUT,
        "mkv" to H264_CUT,
        "webm" to "-c:v libvpx-vp9 -crf 32 -b:v 0 -deadline realtime -cpu-used 8 -row-mt 1 -c:a libopus -b:a 160k",
    )

    private val MIME_TYPES = mapOf(
        "mp4" to "video/mp4",
        "webm" to "video/webm",
        "mkv" to "video/x-matroska",
        "mp3" to "audio/mpeg",
        "m4a" to "audio/mp4",
        "opus" to "audio/ogg",
        "wav" to "audio/wav",
        "flac" to "audio/flac",
    )

    fun normalizeUrl(raw: String?): String? {
        var url = raw?.trim().orEmpty()
        if (url.isEmpty()) return null
        if ("://" !in url) url = "https://$url"
        val uri = try {
            URI(url)
        } catch (e: Exception) {
            return null
        }
        val host = uri.host?.lowercase(Locale.ROOT) ?: return null
        if (uri.scheme !in listOf("http", "https") || !YOUTUBE_HOST.matches(host)) return null
        return url
    }

    fun summarize(info: JSONObject): JSONObject {
        val duration = info.num("duration")
        val formats = info.optJSONArray("formats").objects().filter { !it.optBoolean("has_drm") }

        val bestAudio = formats
            .filter { it.str("vcodec") == "none" && hasCodec(it.str("acodec")) }
            .maxOfOrNull { estimateSize(it, duration) } ?: 0L

        data class Quality(var fps: Int = 0, var size: Long = 0)
        val qualities = sortedMapOf<Int, Quality>(compareByDescending { it })
        for (f in formats) {
            if (!hasCodec(f.str("vcodec"))) continue
            val res = resolutionOf(f) ?: continue
            val q = qualities.getOrPut(res) { Quality() }
            q.fps = max(q.fps, (f.num("fps") ?: 0.0).toInt())
            var size = estimateSize(f, duration)
            if (size > 0 && !hasCodec(f.str("acodec"))) size += bestAudio
            q.size = max(q.size, size)
        }

        val list = JSONArray()
        for ((res, q) in qualities) {
            list.put(
                JSONObject()
                    .put("res", res)
                    .put("fps", q.fps)
                    .put("label", "${res}p" + if (q.fps > 30) q.fps.toString() else "")
                    .put("badge", qualityBadge(res))
                    .put("size", if (q.size > 0) q.size else JSONObject.NULL),
            )
        }

        return JSONObject()
            .put("id", info.str("id"))
            .put("title", info.str("title") ?: info.str("id"))
            .put("channel", info.str("channel") ?: info.str("uploader") ?: JSONObject.NULL)
            .put("duration", duration ?: JSONObject.NULL)
            .put("thumbnail", info.str("thumbnail") ?: JSONObject.NULL)
            .put("view_count", info.num("view_count")?.toLong() ?: JSONObject.NULL)
            .put("upload_date", info.str("upload_date") ?: JSONObject.NULL)
            .put("qualities", list)
            .put("audio_size", if (bestAudio > 0) bestAudio else JSONObject.NULL)
    }

    fun parseDownload(data: JSONObject): DownloadParams {
        val id = data.str("id")
        if (id == null || !VIDEO_ID.matches(id)) throw ApiError(400, "Identificador de video inválido.")

        val kind = data.str("kind")
        if (kind != "video" && kind != "audio") throw ApiError(400, "Tipo de descarga inválido.")

        val start = parseSeconds(data, "start")?.takeIf { it > 0 }
        val end = parseSeconds(data, "end")
        if (end != null && end - (start ?: 0.0) < 1) throw ApiError(400, "El recorte debe durar al menos 1 segundo.")

        if (kind == "audio") {
            val format = data.str("audio_format") ?: "mp3"
            val quality = data.opt("audio_quality")?.toString() ?: "192"
            if (format !in AUDIO_FORMATS || quality !in AUDIO_BITRATES) throw ApiError(400, "Formato de audio inválido.")
            return DownloadParams(id, kind, start, end, audioFormat = format, audioQuality = quality)
        }

        val rawQuality = data.opt("quality")?.toString() ?: "best"
        val quality = if (rawQuality == "best") {
            null
        } else {
            rawQuality.toIntOrNull()?.takeIf { it in 100..8640 } ?: throw ApiError(400, "Calidad inválida.")
        }
        val container = data.str("container") ?: "mp4"
        if (container !in VIDEO_CONTAINERS) throw ApiError(400, "Formato de video inválido.")
        val precise = data.optBoolean("precise", true)
        return DownloadParams(id, kind, start, end, quality, container, precise)
    }

    /** Argumentos de yt-dlp (sin la URL) para una descarga. */
    fun downloadArgs(p: DownloadParams, outDir: String): List<String> {
        val args = mutableListOf(
            "--no-playlist",
            "--newline",
            // Script oficial para los retos de YouTube; yt-dlp comprueba su hash.
            "--remote-components", "ejs:github",
            "--embed-metadata",
            "-o", "$outDir/%(id)s.%(ext)s",
        )
        if (p.trimmed) {
            args += listOf("--download-sections", "*${seconds(p.start ?: 0.0)}-${p.end?.let(::seconds) ?: "inf"}")
        }

        if (p.kind == "audio") {
            val format = if (p.audioFormat == "m4a") "bestaudio[ext=m4a]/bestaudio/best" else "bestaudio/best"
            args += listOf("-f", format, "-x", "--audio-format", p.audioFormat)
            if (p.audioFormat in LOSSY_AUDIO) args += listOf("--audio-quality", "${p.audioQuality}K")
            return args
        }

        val sort = mutableListOf<String>()
        p.quality?.let { sort += "res:$it" }
        if (p.container == "mp4") sort += "+codec:avc:m4a" // H.264 + AAC: se reproduce en cualquier sitio
        val format = if (p.container == "webm") {
            // WEBM solo admite VP8/VP9/AV1 + Vorbis/Opus: elegir pistas que ya lo sean.
            "bestvideo*[ext=webm]+bestaudio[ext=webm]/best[ext=webm]"
        } else {
            "bestvideo*+bestaudio/best"
        }
        args += listOf("-f", format)
        if (sort.isNotEmpty()) args += listOf("-S", sort.joinToString(","))
        args += listOf("--merge-output-format", p.container, "--remux-video", p.container)
        if (p.trimmed && p.precise) {
            args += listOf("--force-keyframes-at-cuts", "--downloader-args", "ffmpeg_o:${PRECISE_CUT_ARGS.getValue(p.container)}")
        }
        return args
    }

    fun downloadName(title: String?, p: DownloadParams, ext: String): String {
        var name = (title ?: p.id)
            .replace(Regex("[\\\\/:*?\"<>|\\p{Cntrl}]"), " ")
            .replace(Regex("\\s+"), " ")
            .trim()
            .take(150)
            .ifEmpty { "video" }
        if (p.trimmed) name += " (${timeTag(p.start ?: 0.0)}-${p.end?.let(::timeTag) ?: "fin"})"
        return "$name.$ext"
    }

    fun mimeType(ext: String): String = MIME_TYPES[ext.lowercase(Locale.ROOT)] ?: "application/octet-stream"

    fun friendlyError(raw: String?): String {
        val msg = raw.orEmpty()
            .replace(Regex("\u001B\\[[0-9;]*m"), "")
            .lines()
            .lastOrNull { it.contains("ERROR:") }
            ?.substringAfter("ERROR:")
            ?.trim()
            ?: raw.orEmpty().trim()
        val lowered = msg.lowercase(Locale.ROOT)
        return when {
            "confirm you" in lowered && "bot" in lowered ->
                "YouTube pide confirmar que no eres un bot. Prueba con otra red (wifi o datos) o más tarde."
            "confirm your age" in lowered || "age-restricted" in lowered ->
                "El video tiene restricción de edad y no se puede descargar sin iniciar sesión."
            "private video" in lowered -> "El video es privado."
            "video unavailable" in lowered || "not available" in lowered -> "El video no está disponible."
            "members-only" in lowered || "join this channel" in lowered -> "El video es exclusivo para miembros del canal."
            "requested format is not available" in lowered ->
                "Esa combinación de calidad y formato no está disponible para este video. Prueba otra."
            "unable to download" in lowered || "failed to resolve" in lowered || "timed out" in lowered ->
                "No hay conexión con YouTube. Revisa tu conexión a internet."
            msg.isEmpty() -> "Error desconocido."
            else -> msg.take(300)
        }
    }

    fun timeTag(seconds: Double): String {
        val total = seconds.toLong()
        val h = total / 3600
        val m = total % 3600 / 60
        val s = total % 60
        return if (h > 0) String.format(Locale.ROOT, "%dh%02dm%02ds", h, m, s) else String.format(Locale.ROOT, "%02dm%02ds", m, s)
    }

    private fun seconds(value: Double): String =
        if (value == floor(value)) value.toLong().toString() else String.format(Locale.ROOT, "%.2f", value).trimEnd('0').trimEnd('.')

    private fun parseSeconds(data: JSONObject, key: String): Double? {
        if (data.isNull(key)) return null
        val value = when (val raw = data.get(key)) {
            is Number -> raw.toDouble()
            is String -> if (raw.isBlank()) return null else raw.toDoubleOrNull()
            else -> null
        }
        if (value == null || value.isNaN() || value.isInfinite() || value < 0) throw ApiError(400, "Tiempo inválido.")
        return value
    }

    private fun estimateSize(f: JSONObject, duration: Double?): Long {
        f.num("filesize")?.let { if (it > 0) return it.toLong() }
        f.num("filesize_approx")?.let { if (it > 0) return it.toLong() }
        val tbr = f.num("tbr")
        return if (tbr != null && duration != null) (tbr * 125 * duration).toLong() else 0L // kbit/s -> bytes
    }

    /** Resolución estilo YouTube: el lado más corto (así un Short 1080x1920 es 1080p). */
    private fun resolutionOf(f: JSONObject): Int? {
        val width = f.num("width")?.toInt()
        val height = f.num("height")?.toInt()
        return if (width != null && height != null) min(width, height) else height
    }

    private fun qualityBadge(res: Int) = when {
        res >= 4320 -> "8K"
        res >= 2160 -> "4K"
        res >= 1440 -> "2K"
        res >= 720 -> "HD"
        else -> ""
    }

    private fun hasCodec(codec: String?) = codec != null && codec != "none"

    private fun JSONObject.str(key: String): String? = if (isNull(key)) null else optString(key)

    private fun JSONObject.num(key: String): Double? = if (isNull(key)) null else optDouble(key).takeUnless { it.isNaN() }

    private fun JSONArray?.objects(): List<JSONObject> =
        if (this == null) emptyList() else (0 until length()).mapNotNull { optJSONObject(it) }
}
