package com.ediskrad.audiovisualizer.audio.capture

import com.ediskrad.audiovisualizer.audio.AudioFrame
import com.ediskrad.audiovisualizer.audio.AudioProvider
import com.ediskrad.audiovisualizer.audio.VisualizerConfig
import kotlin.math.exp
import kotlin.math.sin
import kotlin.math.PI as KPI

/**
 * Синтетический источник аудио — не требует микрофона, не требует разрешений.
 * Используется для тестирования визуализатора без реального звука.
 *
 * Огибающая: sin²(t/period·π), period = 2 с.
 *   — t=0.0 s: envelope ≈ 0  (тишина,  кадр 1 из референса)
 *   — t=1.0 s: envelope = 1  (пик баса, кадр 10 из референса)
 *   — t=2.0 s: envelope ≈ 0  (тишина снова)
 *
 * Распределение по полосам: exp(-bandFrac * 5) — энергия убывает от баса к высоким.
 */
class FakeAudioProvider(private val config: VisualizerConfig) : AudioProvider {

    private val periodMs = 2_000L
    private val frameIntervalMs = 16L   // ~60 fps

    @Volatile private var running = false
    private var thread: Thread? = null

    override fun start(onFrame: (AudioFrame) -> Unit) {
        running = true
        val startMs = System.currentTimeMillis()
        thread = Thread {
            while (running) {
                val elapsed = System.currentTimeMillis() - startMs
                val phase    = (elapsed % periodMs).toFloat() / periodMs   // 0..1
                val s        = sin(phase * KPI.toFloat())
                val envelope = s * s                                         // sin²: 0→1→0

                val spectrum = FloatArray(config.bandCount) { b ->
                    val bandFrac   = b.toFloat() / config.bandCount
                    // Бас доминирует, но есть «пол» 0.3*envelope чтобы всё кольцо реагировало,
                    // а не только верхушка (реалистичнее музыкального сигнала).
                    val bassWeight = 0.30f + 0.70f * exp(-bandFrac * 4.0).toFloat()
                    (envelope * bassWeight * config.sensitivity).coerceIn(0f, 1f)
                }

                val bass   = spectrum.take(4).average().toFloat()
                val mids   = spectrum.drop(4).take(20).average().toFloat()
                val highs  = spectrum.drop(24).average().toFloat()

                onFrame(AudioFrame(spectrum, envelope, bass, mids, highs))

                Thread.sleep(frameIntervalMs)
            }
        }.also { it.isDaemon = true; it.start() }
    }

    override fun stop()    { running = false; thread?.interrupt(); thread = null }
    override fun release() { stop() }
}
