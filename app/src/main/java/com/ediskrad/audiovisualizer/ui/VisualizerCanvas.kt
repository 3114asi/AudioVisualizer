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
import androidx.compose.ui.graphics.lerp
import androidx.compose.ui.text.drawText
import androidx.compose.ui.text.rememberTextMeasurer
import androidx.compose.ui.unit.sp
import com.ediskrad.audiovisualizer.visualizer.SparkState
import com.ediskrad.audiovisualizer.visualizer.VisualizerState
import kotlin.math.PI
import kotlin.math.cos
import kotlin.math.min
import kotlin.math.sin

// FIX #3: gradiant — cyan LEFT (angle≈π → cos≈−1 → frac≈0), magenta RIGHT (angle≈0 → cos≈+1 → frac≈1)
private val CYAN    = Color(0xFF3FC6FF)
private val MAGENTA = Color(0xFFE040FB)

private const val PARTICLE_COUNT = 128

// FIX #1: spatial smoothing — averages amp across ±SMOOTH_HALF adjacent spectrum bins
private fun List<Float>.smoothedAmp(centerIdx: Int, halfWidth: Int = 3): Float {
    var sum = 0f
    var cnt = 0
    for (d in -halfWidth..halfWidth) {
        val idx = (centerIdx + d).coerceIn(0, lastIndex)
        sum += this[idx]
        cnt++
    }
    return sum / cnt
}

@Composable
fun VisualizerCanvas(
    modifier: Modifier = Modifier,
    state: VisualizerState,
) {
    val textMeasurer = rememberTextMeasurer()

    Canvas(modifier = modifier.fillMaxSize()) {
        val w      = size.width
        val h      = size.height
        val cx     = w / 2f
        val cy     = h * 0.42f
        val center = Offset(cx, cy)
        val radius = min(w, h) * 0.235f
        val spec   = state.spectrum
        val bass   = state.bass

        // ══════════════════════════════════════════════════════════════
        // ФОН
        // ══════════════════════════════════════════════════════════════

        drawRect(
            brush = Brush.verticalGradient(
                colors = listOf(Color(0xFF03040D), Color(0xFF09071A), Color(0xFF160A30)),
            ),
        )

        // ── Звёзды ────────────────────────────────────────────────────
        repeat(220) { i ->
            val sx    = ((i * 73) % 100) / 100f * w
            val sy    = ((i * 29) % 100) / 100f * (h * 0.60f)
            val alpha = 0.3f + 0.7f * ((i % 7) / 7f)
            val sr    = 1.2f + (i % 4) * 0.55f
            drawCircle(color = Color.White.copy(alpha = alpha), radius = sr, center = Offset(sx, sy))
            if (i % 7 == 0) {
                val starFrac = (cos(i.toFloat() * 0.14f) + 1f) / 2f
                drawCircle(
                    color = lerp(CYAN, MAGENTA, starFrac).copy(alpha = 0.35f),
                    radius = sr * 2.8f, center = Offset(sx, sy),
                    blendMode = BlendMode.Screen,
                )
            }
        }

        // ── Горы задние ───────────────────────────────────────────────
        val backMtn = Path().apply {
            moveTo(0f, h); lineTo(0f, h * 0.72f)
            cubicTo(w * 0.12f, h * 0.57f, w * 0.22f, h * 0.74f, w * 0.35f, h * 0.62f)
            cubicTo(w * 0.46f, h * 0.54f, w * 0.55f, h * 0.70f, w * 0.68f, h * 0.59f)
            cubicTo(w * 0.80f, h * 0.51f, w * 0.90f, h * 0.67f, w, h * 0.57f)
            lineTo(w, h); close()
        }
        drawPath(
            path  = backMtn,
            brush = Brush.verticalGradient(
                colors = listOf(Color(0xFF1A084A), Color(0xFF0A061A), Color.Black),
                startY = h * 0.49f, endY = h,
            ),
        )

        // ── Горы передние ─────────────────────────────────────────────
        val frontMtn = Path().apply {
            moveTo(0f, h); lineTo(0f, h * 0.77f)
            cubicTo(w * 0.16f, h * 0.64f, w * 0.24f, h * 0.82f, w * 0.38f, h * 0.73f)
            cubicTo(w * 0.48f, h * 0.67f, w * 0.58f, h * 0.92f, w * 0.72f, h * 0.74f)
            cubicTo(w * 0.82f, h * 0.66f, w * 0.91f, h * 0.79f, w, h * 0.71f)
            lineTo(w, h); close()
        }
        drawPath(
            path  = frontMtn,
            brush = Brush.verticalGradient(
                colors = listOf(Color(0xFF2D0A63), Color(0xFF0A0820), Color.Black),
                startY = h * 0.62f, endY = h,
            ),
        )

        // ── Неоновое свечение у основания гор ────────────────────────
        drawRect(
            brush = Brush.verticalGradient(
                colors = listOf(Color.Transparent, Color(0x553FC6FF), Color.Transparent),
                startY = h * 0.66f, endY = h * 0.78f,
            ),
            size = Size(w, h), blendMode = BlendMode.Screen,
        )
        drawRect(
            brush = Brush.verticalGradient(
                colors = listOf(Color.Transparent, Color(0x448A4FFF), Color.Transparent),
                startY = h * 0.70f, endY = h * 0.84f,
            ),
            size = Size(w, h), blendMode = BlendMode.Screen,
        )

        // ── Световой столб ────────────────────────────────────────────
        drawRect(
            brush = Brush.verticalGradient(
                colors = listOf(Color(0xBB8A11FF), Color(0x778A11FF), Color.Transparent),
                startY = cy, endY = h,
            ),
            topLeft = Offset(w * 0.38f, cy), size = Size(w * 0.24f, h - cy),
            blendMode = BlendMode.Screen,
        )
        drawRect(
            brush = Brush.verticalGradient(
                colors = listOf(Color.Transparent, Color(0x558A11FF), Color(0xBB8A11FF)),
                startY = 0f, endY = cy,
            ),
            topLeft = Offset(w * 0.43f, 0f), size = Size(w * 0.14f, cy),
            blendMode = BlendMode.Screen,
        )

        // ══════════════════════════════════════════════════════════════
        // FIX #2: ИСКРЫ — temporальные частицы вместо line-лучей
        // Рисуем ДО кольца — кольцо перекрывает хвосты
        // ══════════════════════════════════════════════════════════════

        val nowMs = System.currentTimeMillis()
        for (spark in state.sparks) {
            val age      = (nowMs - spark.startMs).coerceAtLeast(0L)
            val progress = (age.toFloat() / spark.maxLifeMs).coerceIn(0f, 1f)
            val lifeAlpha = 1f - progress                         // linear fade-out
            val dist     = radius * (1f + spark.speed * age)      // расстояние от центра

            val sx = cx + cos(spark.angle) * dist
            val sy = cy + sin(spark.angle) * dist

            // FIX #3: цвет по colorFraction, рассчитанной в SparkEngine из cos(angle)
            val sparkColor = lerp(CYAN, MAGENTA, spark.colorFraction)

            // FIX #4: каждая искра — маленький bloom: внешний ореол + яркое ядро
            val sr = spark.size * (1f - progress * 0.5f) // слегка уменьшается к концу

            // ореол
            drawCircle(
                color     = sparkColor.copy(alpha = 0.30f * lifeAlpha),
                radius    = sr * 3.2f,
                center    = Offset(sx, sy),
                blendMode = BlendMode.Screen,
            )
            // ядро
            drawCircle(
                color     = sparkColor.copy(alpha = 0.90f * lifeAlpha),
                radius    = sr,
                center    = Offset(sx, sy),
                blendMode = BlendMode.Screen,
            )
            // белая горячая точка в центре
            drawCircle(
                color     = Color.White.copy(alpha = 0.70f * lifeAlpha),
                radius    = sr * 0.45f,
                center    = Offset(sx, sy),
                blendMode = BlendMode.Screen,
            )
        }

        // ══════════════════════════════════════════════════════════════
        // КОЛЬЦО: 128 дискретных частиц — четырёхслойный рендер
        // ══════════════════════════════════════════════════════════════

        val positions  = FloatArray(PARTICLE_COUNT * 2)
        val amplitudes = FloatArray(PARTICLE_COUNT)
        val colors     = Array(PARTICLE_COUNT) { Color.Transparent }

        for (i in 0 until PARTICLE_COUNT) {
            val t     = i.toFloat() / PARTICLE_COUNT
            // angle: −π/2 = вверх, +π/2 = вниз, π/−π = влево, 0 = вправо
            val angle = t * PI.toFloat() * 2f - PI.toFloat() / 2f

            val bIdx  = (t * spec.size).toInt().coerceIn(0, spec.lastIndex)
            // FIX #1: spatial smoothing устраняет острые пики (в т.ч. DC-bin вверху)
            val amp   = spec.smoothedAmp(bIdx, halfWidth = 3).coerceIn(0f, 1f)
            // FIX #1: clamp radialOffset — не даём частице улететь дальше 18% от radius
            val radialOffset = (amp * 0.36f + bass * 0.10f).coerceAtMost(0.18f)
            val r     = radius * (1f + radialOffset)

            positions[i * 2]     = cx + cos(angle) * r
            positions[i * 2 + 1] = cy + sin(angle) * r
            amplitudes[i] = amp

            // FIX #3: colorFrac = (cos(angle)+1)/2 → left(π)=0→CYAN, right(0)=1→MAGENTA
            val colorFrac = (cos(angle.toDouble()).toFloat() + 1f) / 2f
            colors[i] = lerp(CYAN, MAGENTA, colorFrac)
        }

        // ── FIX #4: Проход 0 — ambient outer glow (новый слой) ───────
        for (i in 0 until PARTICLE_COUNT) {
            val amp   = amplitudes[i]
            val color = colors[i]
            val pos   = Offset(positions[i * 2], positions[i * 2 + 1])
            drawCircle(
                color     = color.copy(alpha = 0.15f + amp * 0.20f),
                radius    = 20f + amp * 24f,
                center    = pos,
                blendMode = BlendMode.Screen,
            )
        }

        // ── Проход 1: Outer halos ─────────────────────────────────────
        for (i in 0 until PARTICLE_COUNT) {
            val amp   = amplitudes[i]
            val color = colors[i]
            val pos   = Offset(positions[i * 2], positions[i * 2 + 1])
            drawCircle(
                color     = color.copy(alpha = 0.28f + amp * 0.38f),
                radius    = 10f + amp * 14f,
                center    = pos,
                blendMode = BlendMode.Screen,
            )
            // FIX #4: средний ореол ярче
            drawCircle(
                color     = color.copy(alpha = 0.60f + amp * 0.35f),
                radius    = 4.5f + amp * 7f,
                center    = pos,
                blendMode = BlendMode.Screen,
            )
        }

        // ── Проход 2: Cores ───────────────────────────────────────────
        for (i in 0 until PARTICLE_COUNT) {
            val amp   = amplitudes[i]
            val color = colors[i]
            val pos   = Offset(positions[i * 2], positions[i * 2 + 1])
            // FIX #4: яркость ядра увеличена
            drawCircle(
                color     = color.copy(alpha = 1.0f),
                radius    = 3.5f + amp * 5.5f,
                center    = pos,
                blendMode = BlendMode.Screen,
            )
            drawCircle(
                color     = Color.White.copy(alpha = 0.85f + amp * 0.15f),
                radius    = 1.8f + amp * 3f,
                center    = pos,
                blendMode = BlendMode.Screen,
            )
        }

        // ── Рассеянные частицы вокруг кольца ─────────────────────────
        repeat(100) { i ->
            val t   = i.toFloat() / 100f
            val ang = t * PI.toFloat() * 2f
            val amp = spec.smoothedAmp((i * 2) % spec.size)
            // FIX #3: та же формула для scatter-частиц
            val scatterFrac = (cos(ang.toDouble()).toFloat() + 1f) / 2f
            val scatter = 1.24f + (i % 5) * 0.05f + amp * 0.32f
            drawCircle(
                color = lerp(CYAN, MAGENTA, scatterFrac).copy(alpha = 0.09f + amp * 0.55f),
                radius = 1.5f + amp * 4f,
                center = Offset(cx + cos(ang) * radius * scatter, cy + sin(ang) * radius * scatter),
                blendMode = BlendMode.Screen,
            )
        }

        // ══════════════════════════════════════════════════════════════
        // ЦЕНТР: тёмное заполнение + логотип
        // ══════════════════════════════════════════════════════════════

        drawCircle(
            brush = Brush.radialGradient(
                colors = listOf(Color(0x1800D5FF), Color(0x12000000), Color(0xEE02030A)),
                center = center, radius = radius * 1.10f,
            ),
            radius = radius * 0.90f, center = center,
        )

        val logo = textMeasurer.measure(
            text  = "EDISK",
            style = androidx.compose.ui.text.TextStyle(color = Color.White, fontSize = 30.sp),
        )
        drawText(logo, topLeft = Offset(cx - logo.size.width / 2f, cy - logo.size.height / 1.4f))

        val sub = textMeasurer.measure(
            text  = "AUDIOVISUALIZER",
            style = androidx.compose.ui.text.TextStyle(color = Color(0xFFCDD2E9), fontSize = 9.sp),
        )
        drawText(sub, topLeft = Offset(cx - sub.size.width / 2f, cy + 6f))

        // Рамка
        drawRoundRect(
            color        = Color(0x12FFFFFF),
            topLeft      = Offset(12f, 12f),
            size         = Size(w - 24f, h - 24f),
            cornerRadius = CornerRadius(28f, 28f),
            style        = Stroke(width = 2f),
        )
    }
}
