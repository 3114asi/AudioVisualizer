package com.ediskrad.audiovisualizer.audio.capture

import com.ediskrad.audiovisualizer.audio.AudioFrame
import com.ediskrad.audiovisualizer.audio.AudioProvider
import com.ediskrad.audiovisualizer.audio.SpectrumAnalyzer
import com.ediskrad.audiovisualizer.audio.VisualizerConfig
import java.util.concurrent.Executors
import java.util.concurrent.Future
import java.util.concurrent.atomic.AtomicBoolean

abstract class BaseAudioProvider(
    protected val config: VisualizerConfig = VisualizerConfig(),
) : AudioProvider {
    private val executor = Executors.newSingleThreadExecutor()
    private val running = AtomicBoolean(false)
    private var loop: Future<*>? = null
    private val analyzer = SpectrumAnalyzer(config)

    override fun start(onFrame: (AudioFrame) -> Unit) {
        if (!running.compareAndSet(false, true)) return
        loop = executor.submit {
            setup()
            val buffer = ShortArray(config.fftSize)
            while (running.get()) {
                if (read(buffer) > 0) {
                    onFrame(analyzer.analyze(buffer))
                }
            }
            teardown()
        }
    }

    override fun stop() {
        running.set(false)
        loop?.cancel(true)
        loop = null
    }

    override fun release() {
        stop()
        executor.shutdownNow()
    }

    protected abstract fun setup()
    protected abstract fun read(target: ShortArray): Int
    protected abstract fun teardown()
}
