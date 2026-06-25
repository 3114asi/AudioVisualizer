package com.ediskrad.audiovisualizer.audio

interface AudioProvider {
    fun start(onFrame: (AudioFrame) -> Unit)
    fun stop()
    fun release()
}
