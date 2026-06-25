package com.ediskrad.audiovisualizer.visualizer

import android.media.projection.MediaProjection
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.ediskrad.audiovisualizer.audio.AudioCaptureMode
import com.ediskrad.audiovisualizer.audio.AudioProvider
import com.ediskrad.audiovisualizer.audio.VisualizerConfig
import com.ediskrad.audiovisualizer.audio.capture.MicrophoneAudioProvider
import com.ediskrad.audiovisualizer.audio.capture.PlaybackCaptureAudioProvider
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

class VisualizerViewModel : ViewModel() {
    private val _state = MutableStateFlow(VisualizerState())
    val state: StateFlow<VisualizerState> = _state.asStateFlow()

    private var provider: AudioProvider? = null
    private var mediaProjection: MediaProjection? = null
    private var frameCounter = 0
    private var lastFpsStamp = System.currentTimeMillis()

    fun setCaptureMode(mode: AudioCaptureMode) {
        _state.update {
            it.copy(
                captureMode = mode,
                message = if (mode == AudioCaptureMode.INTERNAL_AUDIO) {
                    "Internal audio capture requires screen-capture consent."
                } else {
                    "Microphone mode selected."
                },
            )
        }
    }

    fun setSensitivity(value: Float) {
        _state.update { it.copy(sensitivity = value) }
    }

    fun attachProjection(projection: MediaProjection) {
        mediaProjection = projection
        _state.update { it.copy(message = "Internal audio capture is ready.") }
    }

    fun start() {
        stop()
        val config = VisualizerConfig(sensitivity = _state.value.sensitivity)
        provider = when (_state.value.captureMode) {
            AudioCaptureMode.MICROPHONE -> MicrophoneAudioProvider(config)
            AudioCaptureMode.INTERNAL_AUDIO -> {
                val projection = mediaProjection
                if (projection == null) {
                    _state.update { it.copy(message = "Grant internal audio capture access first.") }
                    return
                }
                PlaybackCaptureAudioProvider(projection, config)
            }
        }
        provider?.start { frame ->
            val now = System.currentTimeMillis()
            frameCounter++
            if (now - lastFpsStamp >= 1_000L) {
                _state.update { state ->
                    state.copy(fps = frameCounter)
                }
                frameCounter = 0
                lastFpsStamp = now
            }
            viewModelScope.launch(Dispatchers.Default) {
                _state.update {
                    it.copy(
                        isRunning = true,
                        spectrum = frame.spectrum.toList(),
                        volume = frame.volume,
                        bass = frame.bass,
                        mids = frame.mids,
                        highs = frame.highs,
                        message = "Visualizer is live.",
                    )
                }
            }
        }
    }

    fun stop() {
        provider?.stop()
        provider?.release()
        provider = null
        _state.update {
            it.copy(
                isRunning = false,
                volume = 0f,
                bass = 0f,
                mids = 0f,
                highs = 0f,
                spectrum = List(it.spectrum.size) { _ -> 0f },
                message = "Stopped",
            )
        }
    }

    override fun onCleared() {
        stop()
        mediaProjection?.stop()
        mediaProjection = null
        super.onCleared()
    }
}
