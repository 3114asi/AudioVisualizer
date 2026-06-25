package com.ediskrad.audiovisualizer.audio.fft

import kotlin.math.PI
import kotlin.math.cos
import kotlin.math.sin

class ComplexFft(private val size: Int) {
    private val cosTable = FloatArray(size / 2)
    private val sinTable = FloatArray(size / 2)

    init {
        require(size > 0 && size and (size - 1) == 0) { "FFT size must be a power of two." }
        for (i in cosTable.indices) {
            val angle = 2.0 * PI * i / size
            cosTable[i] = cos(angle).toFloat()
            sinTable[i] = sin(angle).toFloat()
        }
    }

    fun magnitude(windowedSamples: FloatArray): FloatArray {
        val real = windowedSamples.copyOf()
        val imag = FloatArray(size)

        bitReverse(real, imag)

        var len = 2
        while (len <= size) {
            val halfLen = len / 2
            val tableStep = size / len
            var i = 0
            while (i < size) {
                var j = 0
                while (j < halfLen) {
                    val tableIndex = j * tableStep
                    val cos = cosTable[tableIndex]
                    val sin = sinTable[tableIndex]
                    val evenIndex = i + j
                    val oddIndex = evenIndex + halfLen
                    val treal = cos * real[oddIndex] + sin * imag[oddIndex]
                    val timag = cos * imag[oddIndex] - sin * real[oddIndex]

                    real[oddIndex] = real[evenIndex] - treal
                    imag[oddIndex] = imag[evenIndex] - timag
                    real[evenIndex] += treal
                    imag[evenIndex] += timag
                    j++
                }
                i += len
            }
            len *= 2
        }

        val output = FloatArray(size / 2)
        for (index in output.indices) {
            val magnitude = kotlin.math.sqrt(real[index] * real[index] + imag[index] * imag[index])
            output[index] = magnitude / size
        }
        return output
    }

    private fun bitReverse(real: FloatArray, imag: FloatArray) {
        var j = 0
        for (i in 1 until size) {
            var bit = size shr 1
            while (j and bit != 0) {
                j = j xor bit
                bit = bit shr 1
            }
            j = j xor bit
            if (i < j) {
                val tempReal = real[i]
                real[i] = real[j]
                real[j] = tempReal

                val tempImag = imag[i]
                imag[i] = imag[j]
                imag[j] = tempImag
            }
        }
    }
}
