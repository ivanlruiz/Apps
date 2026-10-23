package io.github.ivanlruiz.descargador

import android.app.Application
import android.content.Context
import android.net.Uri
import android.util.Log
import com.yausername.ffmpeg.FFmpeg
import com.yausername.youtubedl_android.YoutubeDL
import com.yausername.youtubedl_android.YoutubeDLRequest
import org.json.JSONArray
import org.json.JSONObject
import java.io.File
import java.util.UUID
import java.util.concurrent.ConcurrentHashMap
import java.util.concurrent.CountDownLatch
import java.util.concurrent.Executors
import java.util.concurrent.Semaphore
import java.util.concurrent.TimeUnit
import kotlin.concurrent.thread

/**
 * Atiende la misma API que el servidor de Python (app.py), pero dentro del
 * teléfono: yt-dlp, Python, ffmpeg y QuickJS vienen en youtubedl-android.
 */
object Backend {
    private const val TAG = "Backend"
    private const val MAX_CONCURRENT = 2
    private const val UPDATE_INTERVAL_MS = 24 * 60 * 60 * 1000L
    private const val MAX_FINISHED_JOBS = 30

    private lateinit var app: Application
    private val ready = CountDownLatch(1)
    @Volatile private var initError: String? = null

    val executor = Executors.newCachedThreadPool()
    private val slots = Semaphore(MAX_CONCURRENT, true)
    private val jobs = ConcurrentHashMap<String, Job>()

    fun init(application: Application) {
        app = application
        thread(name = "youtubedl-init") {
            try {
                YoutubeDL.getInstance().init(app)
                FFmpeg.getInstance().init(app)
                autoUpdate()
            } catch (e: Exception) {
                Log.e(TAG, "init", e)
                initError = e.message ?: e.toString()
            } finally {
                ready.countDown()
            }
        }
    }

    /** Busca una versión nueva de yt-dlp como mucho una vez al día: YouTube cambia a menudo. */
    private fun autoUpdate() {
        val prefs = app.getSharedPreferences("backend", Context.MODE_PRIVATE)
        if (System.currentTimeMillis() - prefs.getLong("last_update", 0) < UPDATE_INTERVAL_MS) return
        try {
            YoutubeDL.getInstance().updateYoutubeDL(app, YoutubeDL.UpdateChannel._STABLE)
            prefs.edit().putLong("last_update", System.currentTimeMillis()).apply()
        } catch (e: Exception) {
            Log.w(TAG, "No se pudo actualizar yt-dlp", e)
        }
    }

    private fun awaitReady() {
        if (!ready.await(3, TimeUnit.MINUTES)) throw ApiError(503, "La app sigue preparándose. Inténtalo en un momento.")
        initError?.let { throw ApiError(500, "No se pudo preparar yt-dlp: $it") }
    }

    fun job(id: String): Job? = jobs[id]

    fun activeJobs(): List<Job> = jobs.values.filter { it.active }.sortedBy { it.created }

    /** Devuelve (código HTTP, JSON) para una ruta de la API. */
    fun handle(path: String, body: String?): Pair<Int, JSONObject> = try {
        val data = if (body.isNullOrBlank()) JSONObject() else JSONObject(body)
        when {
            path == "/api/status" -> 200 to status()
            path == "/api/info" -> 200 to info(data.optString("url"))
            path == "/api/download" -> 202 to download(data).toJson()
            path == "/api/update" -> 200 to update()
            path == "/api/jobs" -> 200 to JSONObject().put("jobs", JSONArray(jobs.values.sortedBy { it.created }.map { it.toJson() }))
            path.startsWith("/api/jobs/") -> {
                val parts = path.removePrefix("/api/jobs/").split("/")
                val job = jobs[parts[0]] ?: throw ApiError(404, "No encontrado.")
                when (parts.getOrNull(1)) {
                    null -> 200 to job.toJson()
                    "cancel" -> 200 to cancel(job).toJson()
                    else -> throw ApiError(404, "No encontrado.")
                }
            }
            else -> throw ApiError(404, "No encontrado.")
        }
    } catch (e: ApiError) {
        e.status to JSONObject().put("error", e.message)
    } catch (e: Exception) {
        Log.e(TAG, path, e)
        500 to JSONObject().put("error", Summary.friendlyError(e.message))
    }

    private fun version(): String? = try {
        YoutubeDL.getInstance().version(app)
            ?: YoutubeDL.getInstance().execute(YoutubeDLRequest(emptyList<String>()).addOption("--version")).out.trim()
    } catch (e: Exception) {
        null
    }

    private fun status(): JSONObject {
        awaitReady()
        return JSONObject().put("ffmpeg", true).put("native", true).put("yt_dlp", version() ?: JSONObject.NULL)
    }

    private fun info(rawUrl: String): JSONObject {
        val url = Summary.normalizeUrl(rawUrl) ?: throw ApiError(400, "Pega un enlace válido de YouTube.")
        awaitReady()
        val request = YoutubeDLRequest(url).apply {
            addOption("--dump-single-json")
            addOption("--no-playlist")
            addOption("--flat-playlist")
            addOption("--no-warnings")
            addOption("--remote-components", "ejs:github")
        }
        val info = try {
            JSONObject(YoutubeDL.getInstance().execute(request).out)
        } catch (e: Exception) {
            throw ApiError(422, Summary.friendlyError(e.message))
        }
        if (info.optString("_type") == "playlist") {
            throw ApiError(422, "Eso es una lista de reproducción. Pega el enlace de un solo video.")
        }
        if (info.optString("live_status") in listOf("is_live", "is_upcoming", "post_live")) {
            throw ApiError(422, "No se pueden descargar transmisiones en vivo en curso o programadas.")
        }
        return Summary.summarize(info)
    }

    private fun download(data: JSONObject): Job {
        val params = Summary.parseDownload(data)
        val job = Job(params, data.optString("title").ifBlank { params.id }, data.optString("label"))
        jobs[job.id] = job
        pruneFinished()
        executor.execute { run(job) }
        return job
    }

    private fun cancel(job: Job): Job {
        job.cancelled = true
        YoutubeDL.getInstance().destroyProcessById(job.id)
        if (job.status == "queued") job.finish("cancelled", "Cancelada")
        return job
    }

    private fun update(): JSONObject {
        awaitReady()
        if (activeJobs().isNotEmpty()) throw ApiError(409, "Espera a que terminen las descargas.")
        val result = try {
            YoutubeDL.getInstance().updateYoutubeDL(app, YoutubeDL.UpdateChannel._STABLE)
        } catch (e: Exception) {
            throw ApiError(502, "No se pudo buscar la actualización. Revisa tu conexión.")
        }
        app.getSharedPreferences("backend", Context.MODE_PRIVATE).edit()
            .putLong("last_update", System.currentTimeMillis()).apply()
        return JSONObject()
            .put("updated", result == YoutubeDL.UpdateStatus.DONE)
            .put("yt_dlp", version() ?: JSONObject.NULL)
    }

    private fun run(job: Job) {
        slots.acquire()
        val dir = File(app.cacheDir, "jobs/${job.id}")
        try {
            if (job.cancelled) return
            awaitReady()
            if (job.cancelled) throw YoutubeDL.CanceledException()
            job.status = "downloading"
            // Al recortar, ffmpeg descarga solo el segmento y no informa del progreso.
            job.stage = if (job.params.trimmed) "Descargando y recortando" else "Iniciando descarga"
            dir.mkdirs()

            val request = YoutubeDLRequest("https://www.youtube.com/watch?v=${job.params.id}")
            request.addCommands(Summary.downloadArgs(job.params, dir.absolutePath))
            val tracker = ProgressTracker(job)
            YoutubeDL.getInstance().execute(request, job.id) { _, eta, line -> tracker.onLine(eta, line) }

            val output = dir.listFiles()
                ?.filter { it.isFile && !it.name.endsWith(".part") && !it.name.endsWith(".ytdl") }
                ?.maxByOrNull { it.length() }
                ?: throw ApiError(500, "No se generó ningún archivo.")
            job.status = "processing"
            job.stage = "Guardando en Descargas"
            job.progress = null
            val name = Summary.downloadName(job.title, job.params, output.extension)
            val saved = MediaSaver.save(app, output, name)
            job.uri = saved.uri
            job.location = saved.location
            job.filename = saved.name
            job.mime = Summary.mimeType(output.extension)
            job.size = output.length()
            job.progress = 100.0
            job.finish("done", "Listo")
        } catch (e: Exception) {
            if (job.cancelled || e is YoutubeDL.CanceledException) {
                job.finish("cancelled", "Cancelada")
            } else {
                Log.e(TAG, "download ${job.id}", e)
                job.error = if (e is ApiError) e.message else Summary.friendlyError(e.message)
                job.finish("error", "Error")
            }
        } finally {
            dir.deleteRecursively()
            slots.release()
            if (job.status == "done" || job.status == "error") Notifier.finished(app, job)
        }
    }

    private fun pruneFinished() {
        val finished = jobs.values.filter { !it.active }.sortedByDescending { it.created }
        finished.drop(MAX_FINISHED_JOBS).forEach { jobs.remove(it.id) }
    }
}

class Job(val params: DownloadParams, val title: String, private val label: String) {
    val id: String = UUID.randomUUID().toString().replace("-", "")
    val created = System.currentTimeMillis()

    @Volatile var status = "queued"
    @Volatile var stage = "En cola"
    @Volatile var progress: Double? = null
    @Volatile var speed: Double? = null
    @Volatile var eta: Long? = null
    @Volatile var error: String? = null
    @Volatile var filename: String? = null
    @Volatile var size: Long? = null
    @Volatile var location: String? = null
    @Volatile var uri: Uri? = null
    @Volatile var mime: String? = null
    @Volatile var cancelled = false

    val active get() = status !in listOf("done", "error", "cancelled")

    fun finish(status: String, stage: String) {
        this.stage = stage
        speed = null
        eta = null
        this.status = status
    }

    fun toJson(): JSONObject = JSONObject()
        .put("id", id)
        .put("title", title)
        .put("label", label)
        .put("status", status)
        .put("stage", stage)
        .put("progress", progress ?: JSONObject.NULL)
        .put("speed", speed ?: JSONObject.NULL)
        .put("eta", eta ?: JSONObject.NULL)
        .put("error", error ?: JSONObject.NULL)
        .put("filename", filename ?: JSONObject.NULL)
        .put("size", size ?: JSONObject.NULL)
        .put("location", location ?: JSONObject.NULL)
}

/** Interpreta la salida de yt-dlp para mostrar la etapa, el porcentaje y la velocidad. */
class ProgressTracker(private val job: Job) {
    private var parts = 1
    private var part = -1

    fun onLine(eta: Long, line: String) {
        FORMATS.find(line)?.let {
            parts = it.groupValues[1].count { c -> c == '+' } + 1
            return
        }
        if (line.startsWith("[download] Destination:")) {
            part++
            job.stage = when {
                job.params.trimmed -> "Descargando y recortando"
                parts > 1 -> if (part == 0) "Descargando video (1/2)" else "Descargando audio (2/2)"
                job.params.kind == "audio" -> "Descargando audio"
                else -> "Descargando video"
            }
            return
        }
        PERCENT.find(line)?.let {
            val pct = it.groupValues[1].toDoubleOrNull() ?: return
            // El video suele pesar mucho más que el audio.
            job.progress = when {
                parts < 2 -> pct
                part <= 0 -> pct * 0.9
                else -> 90 + pct * 0.1
            }
            job.speed = SPEED.find(line)?.let { m -> m.groupValues[1].toDouble() * unit(m.groupValues[2]) }
            job.eta = eta.takeIf { e -> e >= 0 }
            return
        }
        STAGES.entries.firstOrNull { line.startsWith(it.key) }?.let {
            job.status = "processing"
            job.stage = it.value
            job.progress = null
            job.speed = null
            job.eta = null
        }
    }

    private fun unit(u: String) = when (u.first()) {
        'K' -> 1024.0
        'M' -> 1024.0 * 1024
        'G' -> 1024.0 * 1024 * 1024
        else -> 1.0
    }

    companion object {
        private val FORMATS = Regex("""Downloading \d+ format\(s\): (\S+)""")
        private val PERCENT = Regex("""^\[download]\s+([\d.]+)%""")
        private val SPEED = Regex("""at\s+([\d.]+)([KMG]?i?B)/s""")
        private val STAGES = mapOf(
            "[Merger]" to "Uniendo video y audio",
            "[ExtractAudio]" to "Convirtiendo audio",
            "[VideoRemuxer]" to "Ajustando el formato",
            "[Metadata]" to "Añadiendo metadatos",
            "[Fixup" to "Procesando",
        )
    }
}
