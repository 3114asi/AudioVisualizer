package com.ediskrad.audiovisualizer.ui

import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import com.ediskrad.audiovisualizer.audio.AudioCaptureMode
import com.ediskrad.audiovisualizer.visualizer.VisualizerState
import com.ediskrad.audiovisualizer.visualizer.VisualizerViewModel

@Composable
fun AudioVisualizerApp(
    viewModel: VisualizerViewModel,
    onModeSelected: (AudioCaptureMode) -> Unit,
    onStartRequested: () -> Unit,
    onStopRequested: () -> Unit,
) {
    val state by viewModel.state.collectAsState()
    VisualizerScreen(
        state = state,
        onStart = onStartRequested,
        onStop = onStopRequested,
        onModeSelected = onModeSelected,
        onSensitivityChanged = viewModel::setSensitivity,
    )
}
