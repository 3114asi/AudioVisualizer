package com.ediskrad.audiovisualizer

import android.Manifest
import android.content.Intent
import android.content.pm.PackageManager
import android.media.projection.MediaProjectionManager
import android.os.Bundle
import android.widget.Toast
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.activity.result.contract.ActivityResultContracts
import androidx.activity.viewModels
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.core.content.ContextCompat
import com.ediskrad.audiovisualizer.audio.AudioCaptureMode
import com.ediskrad.audiovisualizer.ui.AudioVisualizerApp
import com.ediskrad.audiovisualizer.ui.theme.AudioVisualizerTheme
import com.ediskrad.audiovisualizer.visualizer.VisualizerViewModel
import kotlinx.coroutines.MainScope
import kotlinx.coroutines.flow.filterNotNull
import kotlinx.coroutines.launch

class MainActivity : ComponentActivity() {
    private val viewModel: VisualizerViewModel by viewModels()
    private val scope = MainScope()
    private lateinit var mediaProjectionManager: MediaProjectionManager
    private var pendingStartAfterPermission by mutableStateOf(false)

    private val microphonePermissionLauncher =
        registerForActivityResult(ActivityResultContracts.RequestPermission()) { granted ->
            if (granted && pendingStartAfterPermission) {
                viewModel.start()
            }
            pendingStartAfterPermission = false
        }

    private val captureLauncher =
        registerForActivityResult(ActivityResultContracts.StartActivityForResult()) { result ->
            if (result.resultCode == RESULT_OK && result.data != null) {
                runCatching {
                    ContextCompat.startForegroundService(
                        this,
                        MediaProjectionForegroundService.startIntent(
                            this,
                            result.resultCode,
                            result.data,
                        ),
                    )
                }.onSuccess {
                    scope.launch {
                        MediaProjectionRepository.projection
                            .filterNotNull()
                            .collect { projection ->
                                viewModel.attachProjection(projection)
                                if (pendingStartAfterPermission) {
                                    viewModel.start()
                                }
                                pendingStartAfterPermission = false
                                return@collect
                            }
                    }
                }.onFailure {
                    Toast.makeText(
                        this,
                        "Failed to enable internal audio capture.",
                        Toast.LENGTH_SHORT,
                    ).show()
                    stopService(MediaProjectionForegroundService.stopIntent(this))
                    pendingStartAfterPermission = false
                }
            } else {
                stopService(MediaProjectionForegroundService.stopIntent(this))
                pendingStartAfterPermission = false
            }
        }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        mediaProjectionManager = getSystemService(MediaProjectionManager::class.java)
        enableEdgeToEdge()

        setContent {
            AudioVisualizerTheme {
                AudioVisualizerApp(
                    viewModel = viewModel,
                    onModeSelected = viewModel::setCaptureMode,
                    onStartRequested = { ensurePermissionAndStart(viewModel.state.value.captureMode) },
                    onStopRequested = ::stopVisualizer,
                )
            }
        }
    }

    private fun ensurePermissionAndStart(mode: AudioCaptureMode) {
        pendingStartAfterPermission = true
        when (mode) {
            AudioCaptureMode.MICROPHONE -> {
                val granted = ContextCompat.checkSelfPermission(
                    this,
                    Manifest.permission.RECORD_AUDIO,
                ) == PackageManager.PERMISSION_GRANTED
                if (granted) {
                    viewModel.start()
                    pendingStartAfterPermission = false
                } else {
                    microphonePermissionLauncher.launch(Manifest.permission.RECORD_AUDIO)
                }
            }

            AudioCaptureMode.INTERNAL_AUDIO -> {
                captureLauncher.launch(mediaProjectionManager.createScreenCaptureIntent())
            }
        }
    }

    private fun stopVisualizer() {
        viewModel.stop()
        stopService(MediaProjectionForegroundService.stopIntent(this))
    }
}
