package com.ediskrad.audiovisualizer.audio.fft

import kotlin.math.PI
import kotlin.math.cos

class HannWindow(size: Int) {
    private val coefficients = FloatArray(size) { index ->
        (0.5f * (1f - cos((2.0 * PI * index) / (size - 1)).toFloat()))
    }

    fun apply(input: ShortArray, output: FloatArray) {
        for (index in output.indices) {
            output[index] = (input[index] / Short.MAX_VALUE.toFloat()) * coefficients[index]
        }
    }
}
