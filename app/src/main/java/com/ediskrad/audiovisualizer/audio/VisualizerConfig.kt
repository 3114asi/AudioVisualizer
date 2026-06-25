package com.ediskrad.audiovisualizer.audio

data class VisualizerConfig(
    val fftSize: Int = 1024,
    val sampleRate: Int = 44100,
    val bandCount: Int = 96,
    // attack быстрый — кольцо реагирует мгновенно на удары
    val attack: Float = 0.45f,
    // release медленный — хвост не обрывается резко
    val release: Float = 0.08f,
    val peakDecay: Float = 0.010f,
    // sensitivity: усиление после log10. 1.8 даёт хороший отклик при умеренной громкости.
    // Пользователь может менять через слайдер на экране.
    val sensitivity: Float = 1.80f,
)
