package com.ediskrad.audiovisualizer

import android.app.Notification
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.Service
import android.content.Context
import android.content.Intent
import android.media.projection.MediaProjectionManager
import android.os.Build
import android.os.IBinder
import android.util.Log
import androidx.core.app.NotificationCompat
import androidx.core.app.ServiceCompat

class MediaProjectionForegroundService : Service() {
    override fun onCreate() {
        super.onCreate()
        createNotificationChannel()
    }

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        when (intent?.action) {
            ACTION_STOP -> {
                MediaProjectionRepository.clear()
                stopForeground(STOP_FOREGROUND_REMOVE)
                stopSelf()
            }

            else -> {
                ServiceCompat.startForeground(
                    this,
                    NOTIFICATION_ID,
                    buildNotification(),
                    android.content.pm.ServiceInfo.FOREGROUND_SERVICE_TYPE_MEDIA_PROJECTION,
                )
                val resultCode = intent?.getIntExtra(EXTRA_RESULT_CODE, Int.MIN_VALUE) ?: Int.MIN_VALUE
                val resultData = intent?.parcelableIntentExtra(EXTRA_RESULT_DATA)
                if (resultCode != Int.MIN_VALUE && resultData != null) {
                    val manager = getSystemService(MediaProjectionManager::class.java)
                    runCatching {
                        manager.getMediaProjection(resultCode, resultData)
                    }.onSuccess {
                        MediaProjectionRepository.setProjection(it)
                    }.onFailure {
                        Log.e(TAG, "Failed to obtain MediaProjection", it)
                        MediaProjectionRepository.setProjection(null)
                    }
                }
            }
        }
        return START_NOT_STICKY
    }

    override fun onBind(intent: Intent?): IBinder? = null

    private fun buildNotification(): Notification {
        return NotificationCompat.Builder(this, CHANNEL_ID)
            .setSmallIcon(android.R.drawable.ic_btn_speak_now)
            .setContentTitle("Audio Visualizer")
            .setContentText("Internal audio capture is ready.")
            .setOngoing(true)
            .build()
    }

    private fun createNotificationChannel() {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.O) return
        val manager = getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
        val channel = NotificationChannel(
            CHANNEL_ID,
            "Media Projection",
            NotificationManager.IMPORTANCE_LOW,
        )
        manager.createNotificationChannel(channel)
    }

    companion object {
        private const val TAG = "ProjectionService"
        private const val CHANNEL_ID = "media_projection"
        private const val NOTIFICATION_ID = 1001
        const val ACTION_START = "com.ediskrad.audiovisualizer.action.START_MEDIA_PROJECTION"
        const val ACTION_STOP = "com.ediskrad.audiovisualizer.action.STOP_MEDIA_PROJECTION"
        private const val EXTRA_RESULT_CODE = "extra_result_code"
        private const val EXTRA_RESULT_DATA = "extra_result_data"

        fun startIntent(context: Context, resultCode: Int? = null, resultData: Intent? = null): Intent {
            return Intent(context, MediaProjectionForegroundService::class.java)
                .setAction(ACTION_START)
                .apply {
                    if (resultCode != null && resultData != null) {
                        putExtra(EXTRA_RESULT_CODE, resultCode)
                        putExtra(EXTRA_RESULT_DATA, resultData)
                    }
                }
        }

        fun stopIntent(context: Context): Intent {
            return Intent(context, MediaProjectionForegroundService::class.java).setAction(ACTION_STOP)
        }
    }
}

private fun Intent.parcelableIntentExtra(key: String): Intent? {
    return if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
        getParcelableExtra(key, Intent::class.java)
    } else {
        @Suppress("DEPRECATION")
        getParcelableExtra(key)
    }
}
