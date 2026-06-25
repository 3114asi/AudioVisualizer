package com.ediskrad.audiovisualizer.audio

import com.ediskrad.audiovisualizer.audio.fft.ComplexFft
import com.ediskrad.audiovisualizer.audio.fft.HannWindow
import kotlin.math.log10
import kotlin.math.max
import kotlin.math.min
import kotlin.math.pow

class SpectrumAnalyzer(
    private val config: VisualizerConfig = VisualizerConfig(),
) {
    private val fft      = ComplexFft(config.fftSize)
    private val window   = HannWindow(config.fftSize)
    private val windowed = FloatArray(config.fftSize)
    private val smoothed = FloatArray(config.bandCount)
    private val peaks    = FloatArray(config.bandCount)

    // Предвычисленные границы логарифмических полос.
    // Логарифмический rebinning: low/hi = half^(b/N), где half = fftSize/2.
    // Это даёт равное перцептуальное разрешение на всём диапазоне (как у Unity-версии).
    // Bin 0 (DC-компонента) намеренно пропускается — он всегда завышен и вызывает
    // ложный пик у верхней частицы кольца.
    private val binStarts: IntArray
    private val binEnds: IntArray

    init {
        val half = config.fftSize / 2                // 512 для fftSize=1024
        val n    = config.bandCount
        binStarts = IntArray(n)
        binEnds   = IntArray(n)
        for (b in 0 until n) {
            val lo = half.toDouble().pow(b.toDouble() / n)
            val hi = half.toDouble().pow((b + 1.0) / n)
            // FloorToInt(lo), но минимум 1 чтобы пропустить DC-bin (bin 0)
            binStarts[b] = max(1, lo.toInt())
            binEnds[b]   = min(half - 1, hi.toInt().coerceAtLeast(binStarts[b]))
        }
    }

    fun analyze(samples: ShortArray): AudioFrame {
        window.apply(samples, windowed)
        val magnitude = fft.magnitude(windowed)

        val rebinned = FloatArray(config.bandCount)
        for (band in 0 until config.bandCount) {
            val start = binStarts[band]
            val end   = binEnds[band]
            var sum   = 0f
            var count = 0
            for (i in start..end) {
                sum += magnitude[i]
                count++
            }
            val energy     = if (count > 0) sum / count else 0f
            val normalized = min(1f, log10(1f + energy * 18f) * config.sensitivity)
            rebinned[band] = smoothBand(band, normalized)
        }

        val volume = rebinned.average().toFloat()
        val bass   = averageRange(rebinned, 0f,   0.18f)
        val mids   = averageRange(rebinned, 0.18f, 0.60f)
        val highs  = averageRange(rebinned, 0.60f, 1f)

        return AudioFrame(
            spectrum = rebinned.copyOf(),
            volume   = volume,
            bass     = bass,
            mids     = mids,
            highs    = highs,
        )
    }

    private fun smoothBand(index: Int, incoming: Float): Float {
        val current  = smoothed[index]
        val delta    = incoming - current
        val smoothing = if (delta >= 0f) config.attack else config.release
        val next     = current + delta * smoothing
        smoothed[index] = next
        peaks[index] = if (next > peaks[index]) next
                       else max(0f, peaks[index] - config.peakDecay)
        return max(next, peaks[index] * 0.65f)
    }

    private fun averageRange(values: FloatArray, startFraction: Float, endFraction: Float): Float {
        val start = (values.size * startFraction).toInt().coerceIn(0, values.lastIndex)
        val end   = (values.size * endFraction).toInt().coerceIn(start + 1, values.size)
        var sum   = 0f
        for (i in start until end) sum += values[i]
        return sum / (end - start).coerceAtLeast(1)
    }
}
