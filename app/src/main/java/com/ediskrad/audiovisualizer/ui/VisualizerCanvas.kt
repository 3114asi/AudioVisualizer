package com.ediskrad.audiovisualizer.ui

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.CornerRadius
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.BlendMode
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.drawscope.clipRect
import androidx.compose.ui.graphics.lerp
import androidx.compose.ui.text.drawText
import androidx.compose.ui.text.rememberTextMeasurer
import androidx.compose.ui.unit.sp
import com.ediskrad.audiovisualizer.visualizer.VisualizerState
import kotlin.math.PI
import kotlin.math.cos
import kotlin.math.min
import kotlin.math.sin

@Composable
fun VisualizerCanvas(
    modifier: Modifier = Modifier,
    state: VisualizerState,
) {
    val textMeasurer = rememberTextMeasurer()
    Canvas(modifier = modifier.fillMaxSize()) {
        val width = size.width
        val height = size.height
        val center = Offset(width / 2f, height * 0.42f)
        val radius = min(width, height) * 0.235f
        val spectrum = state.spectrum
        val baseColor = listOf(Color(0xFF35E5FF), Color(0xFF8A4FFF), Color(0xFFFF4FDB))

        drawRect(
            brush = Brush.verticalGradient(
                colors = listOf(Color(0xFF03040D), Color(0xFF09071A), Color(0xFF160A30)),
            ),
        )

        repeat(140) { index ->
            val fraction = index / 140f
            val starX = ((index * 73) % 100) / 100f * width
            val starY = ((index * 29) % 100) / 100f * (height * 0.55f)
            val alpha = 0.2f + 0.65f * ((index % 7) / 7f)
            drawCircle(
                color = Color.White.copy(alpha = alpha),
                radius = 1.4f + (index % 3),
                center = Offset(starX, starY),
            )
            if (index % 11 == 0) {
                drawCircle(
                    color = lerp(baseColor.first(), baseColor.last(), fraction).copy(alpha = 0.25f),
                    radius = 3f,
                    center = Offset(starX, starY),
                    blendMode = BlendMode.Screen,
                )
            }
        }

        val mountainPath = Path().apply {
            moveTo(0f, height)
            lineTo(0f, height * 0.77f)
            cubicTo(width * 0.16f, height * 0.64f, width * 0.24f, height * 0.82f, width * 0.38f, height * 0.73f)
            cubicTo(width * 0.48f, height * 0.67f, width * 0.58f, height * 0.92f, width * 0.72f, height * 0.74f)
            cubicTo(width * 0.82f, height * 0.66f, width * 0.91f, height * 0.79f, width, height * 0.71f)
            lineTo(width, height)
            close()
        }
        drawPath(
            path = mountainPath,
            brush = Brush.verticalGradient(
                colors = listOf(Color(0xFF2D0A63), Color(0xFF0A0820), Color.Black),
                startY = height * 0.62f,
                endY = height,
            ),
        )

        drawRect(
            brush = Brush.verticalGradient(
                colors = listOf(Color.Transparent, Color(0xAA8A11FF), Color.Transparent),
                startY = height * 0.52f,
                endY = height,
            ),
            topLeft = Offset(width * 0.42f, height * 0.55f),
            size = Size(width * 0.16f, height * 0.38f),
            blendMode = BlendMode.Screen,
        )

        clipRect {
            spectrum.forEachIndexed { index, value ->
                val angle = (index / spectrum.size.toFloat()) * (PI * 2f) - PI.toFloat() / 2f
                val length = radius * (0.12f + value * 0.55f + state.volume * 0.2f)
                val inner = Offset(
                    x = center.x + (cos(angle) * (radius * 0.92f)).toFloat(),
                    y = center.y + (sin(angle) * (radius * 0.92f)).toFloat(),
                )
                val outer = Offset(
                    x = center.x + (cos(angle) * (radius + length)).toFloat(),
                    y = center.y + (sin(angle) * (radius + length)).toFloat(),
                )
                val color = lerp(baseColor.first(), baseColor.last(), index / spectrum.size.toFloat())
                drawLine(
                    color = color.copy(alpha = 0.24f + value * 0.65f),
                    start = inner,
                    end = outer,
                    strokeWidth = 2f + value * 5f,
                    cap = StrokeCap.Round,
                    blendMode = BlendMode.Screen,
                )
            }
        }

        val ringPath = Path()
        spectrum.forEachIndexed { index, value ->
            val angle = (index / spectrum.size.toFloat()) * (PI * 2f) - PI.toFloat() / 2f
            val offsetRadius = radius * (1f + value * 0.16f + state.bass * 0.08f)
            val point = Offset(
                x = center.x + (cos(angle) * offsetRadius).toFloat(),
                y = center.y + (sin(angle) * offsetRadius).toFloat(),
            )
            if (index == 0) {
                ringPath.moveTo(point.x, point.y)
            } else {
                ringPath.lineTo(point.x, point.y)
            }
        }
        ringPath.close()

        repeat(3) { glow ->
            drawPath(
                path = ringPath,
                color = Color(0xFFB23CFF).copy(alpha = 0.16f - glow * 0.035f),
                style = Stroke(width = radius * (0.13f + glow * 0.03f)),
                blendMode = BlendMode.Screen,
            )
        }

        drawPath(
            path = ringPath,
            brush = Brush.sweepGradient(baseColor + baseColor.first()),
            style = Stroke(width = radius * 0.055f),
            blendMode = BlendMode.Screen,
        )

        repeat(180) { index ->
            val angle = (index / 180f) * (PI * 2f)
            val pulse = spectrum[index % spectrum.size]
            val particleRadius = radius * (1.02f + pulse * 0.18f)
            val point = Offset(
                x = center.x + (cos(angle) * particleRadius).toFloat(),
                y = center.y + (sin(angle) * particleRadius).toFloat(),
            )
            val color = lerp(Color(0xFF26DDFF), Color(0xFFFF4BD1), index / 180f)
            drawCircle(
                color = color.copy(alpha = 0.2f + pulse * 0.8f),
                radius = 1.5f + pulse * 4.5f,
                center = point,
                blendMode = BlendMode.Screen,
            )
        }

        drawCircle(
            brush = Brush.radialGradient(
                colors = listOf(Color(0x2200D5FF), Color(0x16000000), Color(0xEE02030A)),
                center = center,
                radius = radius * 1.12f,
            ),
            radius = radius * 0.93f,
            center = center,
        )

        val logoText = textMeasurer.measure(
            text = "EDISK",
            style = androidx.compose.ui.text.TextStyle(
                color = Color.White,
                fontSize = 30.sp,
            ),
        )
        drawText(
            textLayoutResult = logoText,
            topLeft = Offset(center.x - logoText.size.width / 2f, center.y - logoText.size.height / 1.4f),
        )

        val subText = textMeasurer.measure(
            text = "AUDIOVISUALIZER",
            style = androidx.compose.ui.text.TextStyle(
                color = Color(0xFFCDD2E9),
                fontSize = 9.sp,
            ),
        )
        drawText(
            textLayoutResult = subText,
            topLeft = Offset(center.x - subText.size.width / 2f, center.y + 6f),
        )

        drawRoundRect(
            color = Color(0x12FFFFFF),
            topLeft = Offset(12f, 12f),
            size = Size(width - 24f, height - 24f),
            cornerRadius = CornerRadius(28f, 28f),
            style = Stroke(width = 2f),
        )
    }
}
