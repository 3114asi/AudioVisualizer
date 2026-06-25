package com.ediskrad.audiovisualizer.audio

data class VisualizerConfig(
    val fftSize: Int = 1024,
    val sampleRate: Int = 44100,
    val bandCount: Int = 96,
    val attack: Float = 0.38f,
    val release: Float = 0.08f,
    val peakDecay: Float = 0.012f,
    val sensitivity: Float = 1.35f,
)
