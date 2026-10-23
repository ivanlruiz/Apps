package io.github.ivanlruiz.descargador

import android.app.Application

class App : Application() {
    override fun onCreate() {
        super.onCreate()
        Notifier.createChannels(this)
        Backend.init(this)
    }
}
