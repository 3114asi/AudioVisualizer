package com.ediskrad.audiovisualizer.audio

import com.ediskrad.audiovisualizer.audio.fft.ComplexFft
import com.ediskrad.audiovisualizer.audio.fft.HannWindow
import kotlin.math.log10
import kotlin.math.max
import kotlin.math.min

class SpectrumAnalyzer(
    private val config: VisualizerConfig = VisualizerConfig(),
) {
    private val fft = ComplexFft(config.fftSize)
    private val window = HannWindow(config.fftSize)
    private val windowed = FloatArray(config.fftSize)
    private val smoothed = FloatArray(config.bandCount)
    private val peaks = FloatArray(config.bandCount)

    fun analyze(samples: ShortArray): AudioFrame {
        window.apply(samples, windowed)
        val magnitude = fft.magnitude(windowed)
        val rebinned = FloatArray(config.bandCount)
        val binPerBand = magnitude.size / config.bandCount.toFloat()

        for (band in 0 until config.bandCount) {
            val start = (band * binPerBand).toInt()
            val end = min(magnitude.lastIndex, ((band + 1) * binPerBand).toInt())
            var energy = 0f
            for (index in start..max(start, end)) {
                energy += magnitude[index]
            }
            val normalized = min(1f, log10(1f + energy * 18f) * config.sensitivity)
            rebinned[band] = smoothBand(band, normalized)
        }

        val volume = rebinned.average().toFloat()
        val bass = averageRange(rebinned, 0f, 0.18f)
        val mids = averageRange(rebinned, 0.18f, 0.6f)
        val highs = averageRange(rebinned, 0.6f, 1f)

        return AudioFrame(
            spectrum = rebinned.copyOf(),
            volume = volume,
            bass = bass,
            mids = mids,
            highs = highs,
        )
    }

    private fun smoothBand(index: Int, incoming: Float): Float {
        val current = smoothed[index]
        val delta = incoming - current
        val smoothing = if (delta >= 0f) config.attack else config.release
        val next = current + delta * smoothing
        smoothed[index] = next
        peaks[index] = if (next > peaks[index]) next else max(0f, peaks[index] - config.peakDecay)
        return max(next, peaks[index] * 0.65f)
    }

    private fun averageRange(values: FloatArray, startFraction: Float, endFraction: Float): Float {
        val start = (values.size * startFraction).toInt().coerceIn(0, values.lastIndex)
        val end = (values.size * endFraction).toInt().coerceIn(start + 1, values.size)
        var sum = 0f
        for (index in start until end) {
            sum += values[index]
        }
        return sum / (end - start).coerceAtLeast(1)
    }
}
