package com.ediskrad.audiovisualizer.ui.theme

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

private val AppColors = darkColorScheme(
    primary = Color(0xFF3EE0FF),
    secondary = Color(0xFFD74DFF),
    tertiary = Color(0xFFFF5BD3),
    background = Color(0xFF03040D),
    surface = Color(0xFF0D1020),
)

@Composable
fun AudioVisualizerTheme(content: @Composable () -> Unit) {
    MaterialTheme(
        colorScheme = AppColors,
        content = content,
    )
}
