package com.ediskrad.audiovisualizer.ui

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.FilterChip
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Slider
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import com.ediskrad.audiovisualizer.audio.AudioCaptureMode
import com.ediskrad.audiovisualizer.visualizer.VisualizerState

@Composable
fun VisualizerScreen(
    state: VisualizerState,
    onStart: () -> Unit,
    onStop: () -> Unit,
    onModeSelected: (AudioCaptureMode) -> Unit,
    onSensitivityChanged: (Float) -> Unit,
) {
    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(Color(0xFF03040D)),
    ) {
        VisualizerCanvas(
            modifier = Modifier.fillMaxSize(),
            state = state,
        )

        Column(
            modifier = Modifier
                .fillMaxWidth()
                .statusBarsPadding()
                .padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            Surface(
                shape = RoundedCornerShape(24.dp),
                color = Color(0x33101326),
                tonalElevation = 0.dp,
            ) {
                Column(
                    modifier = Modifier.padding(horizontal = 18.dp, vertical = 14.dp),
                    verticalArrangement = Arrangement.spacedBy(8.dp),
                ) {
                    Text("AUDIO VISUALIZER", color = Color.White, style = MaterialTheme.typography.titleMedium)
                    Text(
                        state.message,
                        color = Color(0xFFB3B9D4),
                        style = MaterialTheme.typography.bodyMedium,
                    )
                }
            }

            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                FilterChip(
                    selected = state.captureMode == AudioCaptureMode.MICROPHONE,
                    onClick = { onModeSelected(AudioCaptureMode.MICROPHONE) },
                    label = { Text("Microphone") },
                )
                FilterChip(
                    selected = state.captureMode == AudioCaptureMode.INTERNAL_AUDIO,
                    onClick = { onModeSelected(AudioCaptureMode.INTERNAL_AUDIO) },
                    label = { Text("Internal Audio") },
                )
            }
        }

        Column(
            modifier = Modifier
                .align(Alignment.BottomCenter)
                .navigationBarsPadding()
                .padding(16.dp)
                .fillMaxWidth()
                .clip(RoundedCornerShape(28.dp))
                .background(
                    Brush.verticalGradient(
                        colors = listOf(Color(0x80121A33), Color(0xCC090B14)),
                    ),
                )
                .padding(18.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            Text(
                text = "Sensitivity ${"%.2f".format(state.sensitivity)}",
                color = Color.White,
                style = MaterialTheme.typography.bodyLarge,
            )
            Slider(
                value = state.sensitivity,
                onValueChange = onSensitivityChanged,
                valueRange = 0.6f..2f,
            )
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically,
            ) {
                ControlPill("Start", isActive = state.isRunning, onClick = onStart)
                ControlPill("Stop", isActive = !state.isRunning, onClick = onStop)
                StatPill("FPS ${state.fps}")
                StatPill("Bass ${(state.bass * 100).toInt()}")
            }
        }
    }
}
