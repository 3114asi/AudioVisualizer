package com.ediskrad.audiovisualizer.visualizer

import android.media.projection.MediaProjection
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.ediskrad.audiovisualizer.audio.AudioCaptureMode
import com.ediskrad.audiovisualizer.audio.AudioFrame
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
import kotlin.math.PI
import kotlin.math.cos

class VisualizerViewModel : ViewModel() {
    private val _state = MutableStateFlow(VisualizerState())
    val state: StateFlow<VisualizerState> = _state.asStateFlow()

    private var provider: AudioProvider? = null
    private var mediaProjection: MediaProjection? = null
    private var frameCounter = 0
    private var lastFpsStamp = System.currentTimeMillis()
    private val sparkEngine = SparkEngine()

    fun setCaptureMode(mode: AudioCaptureMode) {
        _state.update {
            it.copy(
                captureMode = mode,
                message = if (mode == AudioCaptureMode.INTERNAL_AUDIO)
                    "Internal audio capture requires screen-capture consent."
                else
                    "Microphone mode selected.",
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
        sparkEngine.clear()
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
                _state.update { s -> s.copy(fps = frameCounter) }
                frameCounter = 0
                lastFpsStamp = now
            }
            viewModelScope.launch(Dispatchers.Default) {
                val updatedSparks = sparkEngine.update(frame, now)
                _state.update {
                    it.copy(
                        isRunning = true,
                        spectrum  = frame.spectrum.toList(),
                        volume    = frame.volume,
                        bass      = frame.bass,
                        mids      = frame.mids,
                        highs     = frame.highs,
                        sparks    = updatedSparks,
                        message   = "Visualizer is live.",
                    )
                }
            }
        }
    }

    fun stop() {
        provider?.stop()
        provider?.release()
        provider = null
        sparkEngine.clear()
        _state.update {
            it.copy(
                isRunning = false,
                volume    = 0f,
                bass      = 0f,
                mids      = 0f,
                highs     = 0f,
                spectrum  = List(it.spectrum.size) { 0f },
                sparks    = emptyList(),
                message   = "Stopped",
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

/**
 * Управляет пулом искр (SparkState).
 * Вызывается из Dispatchers.Default — не требует синхронизации.
 * Каждый вызов update() возвращает неизменяемый снимок пула для передачи в State.
 */
private class SparkEngine {
    private val pool = mutableListOf<SparkState>()
    private var lastEmitMs = 0L

    fun update(frame: AudioFrame, nowMs: Long): List<SparkState> {
        emitNew(frame, nowMs)
        pruneExpired(nowMs)
        return pool.toList()
    }

    fun clear() {
        pool.clear()
        lastEmitMs = 0L
    }

    private fun emitNew(frame: AudioFrame, nowMs: Long) {
        if (frame.bass < 0.05f || nowMs - lastEmitMs < 18L) return

        // Число искр пропорционально интенсивности баса
        val count = (frame.bass * 22f).toInt().coerceIn(1, 28)
        val spec   = frame.spectrum

        repeat(count) { i ->
            // Распределяем по кольцу, добавляем лёгкий временной сдвиг чтобы не
            // все искры рождались строго в одних и тех же угловых позициях
            val t     = ((i.toFloat() / count) + (nowMs % 800L) * 0.00035f) % 1f
            val angle = t * (PI.toFloat() * 2f) - PI.toFloat() / 2f
            val bIdx  = (t * spec.size).toInt().coerceIn(0, spec.lastIndex)
            val amp   = spec[bIdx].coerceIn(0f, 1f)

            pool.add(
                SparkState(
                    angle         = angle,
                    // speed: доли radius в мс; max дистанция за 350ms ≈ 0.9*radius
                    speed         = 0.0007f + amp * 0.0012f + frame.bass * 0.0006f,
                    startMs       = nowMs,
                    maxLifeMs     = (130L + (amp * 270L).toLong()).coerceIn(80L, 400L),
                    // colorFraction: 0=CYAN слева (cos=−1), 1=MAGENTA справа (cos=+1)
                    colorFraction = (cos(angle.toDouble()).toFloat() + 1f) / 2f,
                    size          = 2.5f + amp * 4.5f + frame.bass * 2f,
                )
            )
        }
        lastEmitMs = nowMs
    }

    private fun pruneExpired(nowMs: Long) {
        pool.removeAll { nowMs - it.startMs > it.maxLifeMs }
        // Жёсткий потолок пула — отбрасываем самые старые
        while (pool.size > 360) pool.removeAt(0)
    }
}
