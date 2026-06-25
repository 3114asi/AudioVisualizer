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

        // Background
        drawRect(
            brush = Brush.verticalGradient(
                colors = listOf(Color(0xFF03040D), Color(0xFF09071A), Color(0xFF160A30)),
            ),
        )

        // Stars — больше и ярче
        repeat(220) { index ->
            val starX = ((index * 73) % 100) / 100f * width
            val starY = ((index * 29) % 100) / 100f * (height * 0.60f)
            val alpha = 0.3f + 0.7f * ((index % 7) / 7f)
            val starSize = 1.2f + (index % 4) * 0.55f
            drawCircle(
                color = Color.White.copy(alpha = alpha),
                radius = starSize,
                center = Offset(starX, starY),
            )
            if (index % 7 == 0) {
                val fraction = index / 220f
                drawCircle(
                    color = lerp(baseColor.first(), baseColor.last(), fraction).copy(alpha = 0.38f),
                    radius = starSize * 2.8f,
                    center = Offset(starX, starY),
                    blendMode = BlendMode.Screen,
                )
            }
        }

        // Горы — задний слой (выше, темнее)
        val backMountainPath = Path().apply {
            moveTo(0f, height)
            lineTo(0f, height * 0.72f)
            cubicTo(width * 0.12f, height * 0.57f, width * 0.22f, height * 0.74f, width * 0.35f, height * 0.62f)
            cubicTo(width * 0.46f, height * 0.54f, width * 0.55f, height * 0.70f, width * 0.68f, height * 0.59f)
            cubicTo(width * 0.80f, height * 0.51f, width * 0.90f, height * 0.67f, width, height * 0.57f)
            lineTo(width, height)
            close()
        }
        drawPath(
            path = backMountainPath,
            brush = Brush.verticalGradient(
                colors = listOf(Color(0xFF1A084A), Color(0xFF0A061A), Color.Black),
                startY = height * 0.49f,
                endY = height,
            ),
        )

        // Горы — передний слой
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

        // Неоновое свечение у основания гор
        drawRect(
            brush = Brush.verticalGradient(
                colors = listOf(Color.Transparent, Color(0x5535E5FF), Color.Transparent),
                startY = height * 0.66f,
                endY = height * 0.78f,
            ),
            size = Size(width, height),
            blendMode = BlendMode.Screen,
        )
        drawRect(
            brush = Brush.verticalGradient(
                colors = listOf(Color.Transparent, Color(0x448A4FFF), Color.Transparent),
                startY = height * 0.70f,
                endY = height * 0.84f,
            ),
            size = Size(width, height),
            blendMode = BlendMode.Screen,
        )

        // Световой столб — двусторонний и шире
        drawRect(
            brush = Brush.verticalGradient(
                colors = listOf(Color(0xBB8A11FF), Color(0x778A11FF), Color.Transparent),
                startY = center.y,
                endY = height,
            ),
            topLeft = Offset(width * 0.38f, center.y),
            size = Size(width * 0.24f, height - center.y),
            blendMode = BlendMode.Screen,
        )
        drawRect(
            brush = Brush.verticalGradient(
                colors = listOf(Color.Transparent, Color(0x558A11FF), Color(0xBB8A11FF)),
                startY = 0f,
                endY = center.y,
            ),
            topLeft = Offset(width * 0.43f, 0f),
            size = Size(width * 0.14f, center.y),
            blendMode = BlendMode.Screen,
        )

        // Лучи спектра — минимальная длина увеличена, ярче
        clipRect {
            spectrum.forEachIndexed { index, value ->
                val angle = (index / spectrum.size.toFloat()) * (PI * 2f) - PI.toFloat() / 2f
                val minLen = radius * 0.20f
                val length = minLen + radius * (value * 0.65f + state.volume * 0.25f)
                val inner = Offset(
                    x = center.x + (cos(angle) * (radius * 0.93f)).toFloat(),
                    y = center.y + (sin(angle) * (radius * 0.93f)).toFloat(),
                )
                val outer = Offset(
                    x = center.x + (cos(angle) * (radius + length)).toFloat(),
                    y = center.y + (sin(angle) * (radius + length)).toFloat(),
                )
                val color = lerp(baseColor.first(), baseColor.last(), index / spectrum.size.toFloat())
                drawLine(
                    color = color.copy(alpha = 0.38f + value * 0.62f),
                    start = inner,
                    end = outer,
                    strokeWidth = 2.5f + value * 6f,
                    cap = StrokeCap.Round,
                    blendMode = BlendMode.Screen,
                )
            }
        }

        // Кольцо — усиленная деформация по FFT
        val ringPath = Path()
        spectrum.forEachIndexed { index, value ->
            val angle = (index / spectrum.size.toFloat()) * (PI * 2f) - PI.toFloat() / 2f
            val offsetRadius = radius * (1f + value * 0.42f + state.bass * 0.12f)
            val point = Offset(
                x = center.x + (cos(angle) * offsetRadius).toFloat(),
                y = center.y + (sin(angle) * offsetRadius).toFloat(),
            )
            if (index == 0) ringPath.moveTo(point.x, point.y) else ringPath.lineTo(point.x, point.y)
        }
        ringPath.close()

        // Glow — 5 фиолетовых слоёв + 3 синих
        repeat(5) { glow ->
            drawPath(
                path = ringPath,
                color = Color(0xFFB23CFF).copy(alpha = 0.24f - glow * 0.038f),
                style = Stroke(width = radius * (0.20f + glow * 0.058f)),
                blendMode = BlendMode.Screen,
            )
        }
        repeat(3) { glow ->
            drawPath(
                path = ringPath,
                color = Color(0xFF35E5FF).copy(alpha = 0.14f - glow * 0.035f),
                style = Stroke(width = radius * (0.10f + glow * 0.04f)),
                blendMode = BlendMode.Screen,
            )
        }

        // Кольцо — основная линия
        drawPath(
            path = ringPath,
            brush = Brush.sweepGradient(baseColor + baseColor.first()),
            style = Stroke(width = radius * 0.065f),
            blendMode = BlendMode.Screen,
        )

        // Внутренние частицы (по периметру кольца)
        repeat(180) { index ->
            val angle = (index / 180f) * (PI * 2f)
            val pulse = spectrum[index % spectrum.size]
            val particleRadius = radius * (1.02f + pulse * 0.22f)
            val point = Offset(
                x = center.x + (cos(angle) * particleRadius).toFloat(),
                y = center.y + (sin(angle) * particleRadius).toFloat(),
            )
            val color = lerp(Color(0xFF26DDFF), Color(0xFFFF4BD1), index / 180f)
            drawCircle(
                color = color.copy(alpha = 0.28f + pulse * 0.72f),
                radius = 2f + pulse * 5.5f,
                center = point,
                blendMode = BlendMode.Screen,
            )
        }

        // Внешнее облако частиц (дальше от кольца)
        repeat(120) { index ->
            val angle = (index / 120f) * (PI * 2f)
            val pulse = spectrum[(index * 2) % spectrum.size]
            val scatter = 1.22f + (index % 5) * 0.06f + pulse * 0.38f
            val point = Offset(
                x = center.x + (cos(angle) * radius * scatter).toFloat(),
                y = center.y + (sin(angle) * radius * scatter).toFloat(),
            )
            val color = lerp(Color(0xFF35E5FF), Color(0xFF8A4FFF), index / 120f)
            drawCircle(
                color = color.copy(alpha = 0.10f + pulse * 0.58f),
                radius = 1.5f + pulse * 4.5f,
                center = point,
                blendMode = BlendMode.Screen,
            )
        }

        // Тёмный центр кольца
        drawCircle(
            brush = Brush.radialGradient(
                colors = listOf(Color(0x2200D5FF), Color(0x16000000), Color(0xEE02030A)),
                center = center,
                radius = radius * 1.12f,
            ),
            radius = radius * 0.93f,
            center = center,
        )

        // Логотип
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
