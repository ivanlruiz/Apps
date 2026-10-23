package io.github.ivanlruiz.descargador

import org.json.JSONObject
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Assert.fail
import org.junit.Test

class SummaryTest {
    private val id = "abcdefghijk"

    @Test
    fun acceptsYoutubeUrls() {
        listOf(
            "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            "youtube.com/watch?v=dQw4w9WgXcQ",
            "https://youtu.be/dQw4w9WgXcQ?t=10",
            "https://m.youtube.com/shorts/dQw4w9WgXcQ",
            "https://music.youtube.com/watch?v=dQw4w9WgXcQ",
        ).forEach { assertNotNull(it, Summary.normalizeUrl(it)) }
    }

    @Test
    fun rejectsOtherUrls() {
        listOf(
            "", null, "hola", "https://example.com/watch?v=x", "file:///etc/passwd",
            "https://youtube.com.evil.com/watch", "ftp://youtube.com/x",
        ).forEach { assertNull(it, Summary.normalizeUrl(it)) }
    }

    @Test
    fun summarizesQualities() {
        val info = JSONObject(
            """
            {"id": "$id", "title": "T", "duration": 100, "channel": null, "formats": [
              {"format_id": "sb", "vcodec": "none", "acodec": "none", "ext": "mhtml"},
              {"format_id": "a", "vcodec": "none", "acodec": "opus", "filesize": 1000},
              {"format_id": "1080", "vcodec": "vp9", "acodec": "none", "width": 1920, "height": 1080, "fps": 60, "filesize": 10000},
              {"format_id": "1080b", "vcodec": "avc1", "acodec": "none", "width": 1920, "height": 1080, "fps": 30, "tbr": 0.5},
              {"format_id": "360", "vcodec": "avc1", "acodec": "mp4a", "width": 640, "height": 360, "fps": 30, "filesize_approx": 3000},
              {"format_id": "short", "vcodec": "vp9", "acodec": "none", "width": 720, "height": 1280, "filesize": null},
              {"format_id": "drm", "vcodec": "vp9", "acodec": "none", "height": 2160, "has_drm": true}
            ]}
            """,
        )
        val summary = Summary.summarize(info)
        val qualities = summary.getJSONArray("qualities")
        assertEquals(listOf("1080p60", "720p", "360p"), (0 until qualities.length()).map { qualities.getJSONObject(it).getString("label") })
        val best = qualities.getJSONObject(0)
        assertEquals(1080, best.getInt("res"))
        assertEquals("HD", best.getString("badge"))
        assertEquals(11000L, best.getLong("size"))
        assertTrue(qualities.getJSONObject(1).isNull("size"))
        assertEquals(3000L, qualities.getJSONObject(2).getLong("size")) // ya incluye audio
        assertEquals(1000L, summary.getLong("audio_size"))
        assertTrue(summary.isNull("channel"))
    }

    private fun params(json: String) = Summary.parseDownload(JSONObject(json))

    @Test
    fun rejectsInvalidDownloads() {
        listOf(
            """{"id": "bad", "kind": "video"}""",
            """{"id": "$id", "kind": "gif"}""",
            """{"id": "$id", "kind": "video", "container": "avi"}""",
            """{"id": "$id", "kind": "video", "quality": "ultra"}""",
            """{"id": "$id", "kind": "video", "start": 10, "end": 10.5}""",
            """{"id": "$id", "kind": "video", "start": -1}""",
            """{"id": "$id", "kind": "audio", "audio_format": "aiff"}""",
            """{"id": "$id", "kind": "audio", "audio_format": "mp3", "audio_quality": "999"}""",
        ).forEach {
            try {
                params(it)
                fail("Debería rechazar $it")
            } catch (e: ApiError) {
                assertEquals(400, e.status)
            }
        }
    }

    @Test
    fun buildsTrimmedPreciseMp4Args() {
        val p = params("""{"id": "$id", "kind": "video", "quality": "720", "container": "mp4", "start": 12.5, "end": 65, "precise": true}""")
        assertEquals(
            listOf(
                "--no-playlist", "--newline", "--remote-components", "ejs:github", "--embed-metadata",
                "-o", "/out/%(id)s.%(ext)s",
                "--download-sections", "*12.5-65",
                "-f", "bestvideo*+bestaudio/best",
                "-S", "res:720,+codec:avc:m4a",
                "--merge-output-format", "mp4", "--remux-video", "mp4",
                "--force-keyframes-at-cuts",
                "--downloader-args", "ffmpeg_o:-c:v libx264 -preset veryfast -crf 18 -c:a aac -b:a 192k",
            ),
            Summary.downloadArgs(p, "/out"),
        )
    }

    @Test
    fun buildsFullWebmArgs() {
        val args = Summary.downloadArgs(params("""{"id": "$id", "kind": "video", "quality": "best", "container": "webm"}"""), "/out")
        assertEquals("bestvideo*[ext=webm]+bestaudio[ext=webm]/best[ext=webm]", args[args.indexOf("-f") + 1])
        assertTrue("-S" !in args)
        assertTrue("--download-sections" !in args)
    }

    @Test
    fun buildsAudioArgs() {
        val mp3 = Summary.downloadArgs(params("""{"id": "$id", "kind": "audio", "audio_format": "mp3", "audio_quality": "192", "start": 0, "end": 9}"""), "/out")
        assertEquals("*0-9", mp3[mp3.indexOf("--download-sections") + 1])
        assertEquals(listOf("-f", "bestaudio/best", "-x", "--audio-format", "mp3", "--audio-quality", "192K"), mp3.takeLast(7))

        val wav = Summary.downloadArgs(params("""{"id": "$id", "kind": "audio", "audio_format": "wav", "audio_quality": "320"}"""), "/out")
        assertTrue("--audio-quality" !in wav)
    }

    @Test
    fun namesFiles() {
        val trimmed = params("""{"id": "$id", "kind": "video", "start": 5, "end": 3725}""")
        assertEquals("Prueba clip raro (00m05s-1h02m05s).mp4", Summary.downloadName("Prueba: clip/raro?", trimmed, "mp4"))
        val full = params("""{"id": "$id", "kind": "audio"}""")
        assertEquals("Canción.mp3", Summary.downloadName("Canción", full, "mp3"))
    }

    @Test
    fun explainsErrors() {
        val bot = Summary.friendlyError("WARNING: x\nERROR: [youtube] abc: Sign in to confirm you’re not a bot.")
        assertTrue(bot, bot.contains("bot"))
        assertEquals("El video es privado.", Summary.friendlyError("ERROR: [youtube] abc: Private video. Sign in"))
    }
}
