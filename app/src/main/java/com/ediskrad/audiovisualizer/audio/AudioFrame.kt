package com.ediskrad.audiovisualizer.audio

data class AudioFrame(
    val spectrum: FloatArray,
    val volume: Float,
    val bass: Float,
    val mids: Float,
    val highs: Float,
)
