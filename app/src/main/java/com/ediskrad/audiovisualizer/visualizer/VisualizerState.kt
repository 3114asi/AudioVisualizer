package com.ediskrad.audiovisualizer.visualizer

import com.ediskrad.audiovisualizer.audio.AudioCaptureMode

data class VisualizerState(
    val isRunning: Boolean = false,
    val captureMode: AudioCaptureMode = AudioCaptureMode.MICROPHONE,
    val spectrum: List<Float> = List(96) { 0f },
    val volume: Float = 0f,
    val bass: Float = 0f,
    val mids: Float = 0f,
    val highs: Float = 0f,
    val sensitivity: Float = 1.80f,
    val fps: Int = 60,
    val message: String = "Ready",
    val sparks: List<SparkState> = emptyList(),
)
