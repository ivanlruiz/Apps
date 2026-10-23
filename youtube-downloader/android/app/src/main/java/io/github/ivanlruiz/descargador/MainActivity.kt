package io.github.ivanlruiz.descargador

import android.Manifest
import android.annotation.SuppressLint
import android.content.ActivityNotFoundException
import android.content.Intent
import android.content.pm.PackageManager
import android.content.res.Configuration
import android.graphics.Color
import android.net.Uri
import android.os.Build
import android.os.Bundle
import android.view.View
import android.view.ViewGroup
import android.webkit.WebChromeClient
import android.webkit.WebResourceRequest
import android.webkit.WebResourceResponse
import android.webkit.WebView
import android.webkit.WebViewClient
import android.widget.FrameLayout
import android.widget.Toast
import androidx.activity.OnBackPressedCallback
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AppCompatActivity
import androidx.core.content.ContextCompat
import androidx.core.view.ViewCompat
import androidx.core.view.WindowCompat
import androidx.core.view.WindowInsetsCompat
import androidx.core.view.WindowInsetsControllerCompat
import androidx.webkit.JavaScriptReplyProxy
import androidx.webkit.WebMessageCompat
import androidx.webkit.WebViewAssetLoader
import androidx.webkit.WebViewCompat
import androidx.webkit.WebViewFeature
import org.json.JSONException
import org.json.JSONObject

class MainActivity : AppCompatActivity() {
    private lateinit var root: FrameLayout
    private lateinit var webView: WebView
    private var pageLoaded = false
    private var pendingUrl: String? = null
    private var fullscreenView: View? = null
    private var fullscreenCallback: WebChromeClient.CustomViewCallback? = null

    // La interfaz se sirve desde un dominio propio (el id de la app): así el
    // reproductor de YouTube recibe un Referer válido y el puente solo existe ahí.
    private val domain by lazy { packageName }
    private val assetLoader by lazy {
        WebViewAssetLoader.Builder()
            .setDomain(domain)
            .addPathHandler("/static/", WebViewAssetLoader.AssetsPathHandler(this))
            .build()
    }

    private val askPermission = registerForActivityResult(ActivityResultContracts.RequestPermission()) {}

    private val exitFullscreen = object : OnBackPressedCallback(false) {
        override fun handleOnBackPressed() = hideFullscreen()
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        WindowCompat.setDecorFitsSystemWindows(window, false)
        val night = resources.configuration.uiMode and Configuration.UI_MODE_NIGHT_MASK == Configuration.UI_MODE_NIGHT_YES
        insetsController().apply {
            isAppearanceLightStatusBars = !night
            isAppearanceLightNavigationBars = !night
        }

        webView = WebView(this)
        root = FrameLayout(this).apply {
            setBackgroundColor(ContextCompat.getColor(this@MainActivity, R.color.background))
            addView(webView, FrameLayout.LayoutParams(MATCH, MATCH))
        }
        setContentView(root)
        ViewCompat.setOnApplyWindowInsetsListener(root) { view, insets ->
            val bars = insets.getInsets(
                WindowInsetsCompat.Type.systemBars() or WindowInsetsCompat.Type.displayCutout() or WindowInsetsCompat.Type.ime(),
            )
            view.setPadding(bars.left, bars.top, bars.right, bars.bottom)
            WindowInsetsCompat.CONSUMED
        }
        onBackPressedDispatcher.addCallback(this, exitFullscreen)

        setupWebView()
        handleIntent(intent)
        webView.loadUrl("https://$domain/static/index.html")

        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.Q) {
            ensurePermission(Manifest.permission.WRITE_EXTERNAL_STORAGE)
        }
    }

    @SuppressLint("SetJavaScriptEnabled")
    private fun setupWebView() {
        webView.settings.apply {
            javaScriptEnabled = true
            domStorageEnabled = true
            allowFileAccess = false
            allowContentAccess = false
        }
        webView.webViewClient = object : WebViewClient() {
            override fun shouldInterceptRequest(view: WebView, request: WebResourceRequest): WebResourceResponse? =
                assetLoader.shouldInterceptRequest(request.url)

            override fun shouldOverrideUrlLoading(view: WebView, request: WebResourceRequest): Boolean {
                if (!request.isForMainFrame || request.url.host == domain) return false
                openExternal(request.url)
                return true
            }

            override fun onPageFinished(view: WebView, url: String) {
                pageLoaded = true
                deliverSharedUrl()
            }
        }
        webView.webChromeClient = object : WebChromeClient() {
            override fun onShowCustomView(view: View, callback: CustomViewCallback) = showFullscreen(view, callback)
            override fun onHideCustomView() = hideFullscreen()
        }

        if (WebViewFeature.isFeatureSupported(WebViewFeature.WEB_MESSAGE_LISTENER)) {
            WebViewCompat.addWebMessageListener(webView, "AndroidApp", setOf("https://$domain"), ::onMessage)
        } else {
            toast("Actualiza «Android System WebView» desde Play Store para usar la app.")
        }
    }

    /** Mensajes de la interfaz: peticiones a la API o abrir/compartir un archivo. */
    @Suppress("UNUSED_PARAMETER")
    private fun onMessage(
        view: WebView,
        message: WebMessageCompat,
        sourceOrigin: Uri,
        isMainFrame: Boolean,
        reply: JavaScriptReplyProxy,
    ) {
        if (!isMainFrame) return
        val msg = try {
            JSONObject(message.data ?: return)
        } catch (e: JSONException) {
            return
        }
        when (msg.optString("action")) {
            "open" -> openJob(msg.optString("job"), share = false)
            "share" -> openJob(msg.optString("job"), share = true)
            "request" -> {
                val id = msg.optString("id")
                val path = msg.optString("path")
                val body = msg.optString("body")
                Backend.executor.execute {
                    val (status, data) = Backend.handle(path, body)
                    runOnUiThread {
                        if (path == "/api/download" && status < 300) onDownloadStarted()
                        reply.postMessage(JSONObject().put("id", id).put("status", status).put("data", data).toString())
                    }
                }
            }
        }
    }

    private fun onDownloadStarted() {
        DownloadService.start(this)
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            ensurePermission(Manifest.permission.POST_NOTIFICATIONS)
        }
    }

    private fun ensurePermission(permission: String) {
        if (ContextCompat.checkSelfPermission(this, permission) != PackageManager.PERMISSION_GRANTED) {
            askPermission.launch(permission)
        }
    }

    private fun openJob(jobId: String, share: Boolean) {
        val job = Backend.job(jobId)
        val uri = job?.uri ?: return toast("No se encontró el archivo.")
        val intent = if (share) {
            Intent.createChooser(
                Intent(Intent.ACTION_SEND)
                    .setType(job.mime)
                    .putExtra(Intent.EXTRA_STREAM, uri)
                    .addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION),
                "Compartir",
            )
        } else {
            Intent(Intent.ACTION_VIEW).setDataAndType(uri, job.mime).addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
        }
        try {
            startActivity(intent)
        } catch (e: ActivityNotFoundException) {
            toast("No hay ninguna app para abrir este archivo.")
        }
    }

    private fun openExternal(uri: Uri) {
        try {
            startActivity(Intent(Intent.ACTION_VIEW, uri))
        } catch (e: ActivityNotFoundException) {
            toast("No se pudo abrir el enlace.")
        }
    }

    // Enlaces compartidos desde la app de YouTube ("Compartir" → Descargador).

    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        handleIntent(intent)
    }

    private fun handleIntent(intent: Intent?) {
        if (intent?.action != Intent.ACTION_SEND) return
        val text = intent.getStringExtra(Intent.EXTRA_TEXT) ?: return
        pendingUrl = URL.find(text)?.value ?: return
        deliverSharedUrl()
    }

    private fun deliverSharedUrl() {
        val url = pendingUrl ?: return
        if (!pageLoaded) return
        pendingUrl = null
        webView.evaluateJavascript("window.loadSharedUrl(${JSONObject.quote(url)})", null)
    }

    // Pantalla completa del reproductor de YouTube.

    private fun showFullscreen(view: View, callback: WebChromeClient.CustomViewCallback) {
        if (fullscreenView != null) {
            callback.onCustomViewHidden()
            return
        }
        fullscreenView = view
        fullscreenCallback = callback
        view.setBackgroundColor(Color.BLACK)
        (window.decorView as ViewGroup).addView(view, FrameLayout.LayoutParams(MATCH, MATCH))
        insetsController().apply {
            hide(WindowInsetsCompat.Type.systemBars())
            systemBarsBehavior = WindowInsetsControllerCompat.BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE
        }
        exitFullscreen.isEnabled = true
    }

    private fun hideFullscreen() {
        val view = fullscreenView ?: return
        (window.decorView as ViewGroup).removeView(view)
        fullscreenView = null
        insetsController().show(WindowInsetsCompat.Type.systemBars())
        exitFullscreen.isEnabled = false
        fullscreenCallback?.onCustomViewHidden()
        fullscreenCallback = null
    }

    private fun insetsController() = WindowCompat.getInsetsController(window, window.decorView)

    private fun toast(text: String) = Toast.makeText(this, text, Toast.LENGTH_LONG).show()

    override fun onResume() {
        super.onResume()
        webView.onResume()
    }

    override fun onPause() {
        webView.onPause()
        super.onPause()
    }

    override fun onDestroy() {
        webView.destroy()
        super.onDestroy()
    }

    companion object {
        private const val MATCH = ViewGroup.LayoutParams.MATCH_PARENT
        private val URL = Regex("""https?://\S+""")
    }
}
