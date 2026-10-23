package io.github.ivanlruiz.descargador

import android.content.ContentValues
import android.content.Context
import android.media.MediaScannerConnection
import android.net.Uri
import android.os.Build
import android.os.Environment
import android.provider.MediaStore
import java.io.File
import java.io.IOException
import java.util.concurrent.CountDownLatch
import java.util.concurrent.TimeUnit

/** Copia el archivo terminado a la carpeta pública Descargas/Descargador. */
object MediaSaver {
    private const val FOLDER = "Descargador"
    private const val LOCATION = "Descargas/$FOLDER"

    data class Saved(val uri: Uri?, val name: String, val location: String)

    fun save(context: Context, source: File, displayName: String): Saved {
        val mime = Summary.mimeType(source.extension)
        return if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
            saveWithMediaStore(context, source, displayName, mime)
        } else {
            saveLegacy(context, source, displayName, mime)
        }
    }

    private fun saveWithMediaStore(context: Context, source: File, displayName: String, mime: String): Saved {
        val resolver = context.contentResolver
        val values = ContentValues().apply {
            put(MediaStore.Downloads.DISPLAY_NAME, displayName)
            put(MediaStore.Downloads.MIME_TYPE, mime)
            put(MediaStore.Downloads.RELATIVE_PATH, "${Environment.DIRECTORY_DOWNLOADS}/$FOLDER")
            put(MediaStore.Downloads.IS_PENDING, 1)
        }
        val uri = resolver.insert(MediaStore.Downloads.EXTERNAL_CONTENT_URI, values)
            ?: throw IOException("No se pudo crear el archivo en Descargas.")
        try {
            resolver.openOutputStream(uri).use { out ->
                if (out == null) throw IOException("No se pudo escribir en Descargas.")
                source.inputStream().use { it.copyTo(out) }
            }
            values.clear()
            values.put(MediaStore.Downloads.IS_PENDING, 0)
            resolver.update(uri, values, null, null)
        } catch (e: Exception) {
            resolver.delete(uri, null, null)
            throw e
        }
        return Saved(uri, displayName, LOCATION)
    }

    @Suppress("DEPRECATION")
    private fun saveLegacy(context: Context, source: File, displayName: String, mime: String): Saved {
        val dir = File(Environment.getExternalStoragePublicDirectory(Environment.DIRECTORY_DOWNLOADS), FOLDER)
        if (!dir.isDirectory && !dir.mkdirs()) {
            throw IOException("Sin permiso para guardar en Descargas. Actívalo en los ajustes de la app.")
        }
        var target = File(dir, displayName)
        var n = 1
        while (target.exists()) {
            target = File(dir, "${displayName.substringBeforeLast('.')} ($n).${source.extension}")
            n++
        }
        source.copyTo(target)

        // El escáner de medios da una URI content:// para poder abrir y compartir el archivo.
        var uri: Uri? = null
        val scanned = CountDownLatch(1)
        MediaScannerConnection.scanFile(context, arrayOf(target.absolutePath), arrayOf(mime)) { _, result ->
            uri = result
            scanned.countDown()
        }
        scanned.await(10, TimeUnit.SECONDS)
        return Saved(uri, target.name, LOCATION)
    }
}
