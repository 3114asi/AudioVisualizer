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
import com.ediskrad.audiovisualizer.visualizer.VisualizerState
import kotlin.math.PI
import kotlin.math.cos
import kotlin.math.min
import kotlin.math.sin

private val CYAN    = Color(0xFF3FC6FF) // #3FC6FF — левая дуга
private val MAGENTA = Color(0xFFE040FB) // #E040FB — правая дуга

private const val PARTICLE_COUNT = 128

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
                drawCircle(
                    color = lerp(CYAN, MAGENTA, i / 220f).copy(alpha = 0.35f),
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

        // ── Световой столб (вниз + вверх) ────────────────────────────
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
        // БАС-ВЫБРОС: стрики наружу от кольца
        // Рисуем ДО кольца — кольцо перекрывает основание стриков
        // ══════════════════════════════════════════════════════════════

        if (bass > 0.04f) {
            // alpha: минимум 0.50 при низком басе, до 0.95 на пике
            val burstAlpha = (0.50f + bass * 0.45f).coerceIn(0f, 1f)
            repeat(72) { i ->
                val t     = i.toFloat() / 72f
                val angle = t * PI.toFloat() * 2f - PI.toFloat() / 2f
                val bIdx  = (t * spec.size).toInt().coerceIn(0, spec.lastIndex)
                val amp   = spec[bIdx]
                // длина: минимум 0.15*radius при низком басе, до 2*radius на пике
                val len   = radius * (0.15f + bass * 1.8f + amp * 0.8f)
                val ir    = radius * (1f + amp * 0.25f)
                val or_   = ir + len
                drawLine(
                    color       = lerp(CYAN, MAGENTA, t).copy(alpha = burstAlpha),
                    start       = Offset(cx + cos(angle) * ir, cy + sin(angle) * ir),
                    end         = Offset(cx + cos(angle) * or_, cy + sin(angle) * or_),
                    strokeWidth = 1.8f + amp * 4f + bass * 2f,
                    cap         = StrokeCap.Round,
                    blendMode   = BlendMode.Screen,
                )
            }
        }

        // ══════════════════════════════════════════════════════════════
        // КОЛЬЦО: 128 дискретных частиц — двухпроходный рендер
        //
        // Проход 1 — halos: каждый halo перекрывается с соседними,
        //            в сумме образуя кольцевое glow (как в референсе)
        // Проход 2 — cores: маленькие яркие ядра поверх halo,
        //            создают дискретные видимые точки
        // ══════════════════════════════════════════════════════════════

        // Вспомогательные данные общие для обоих проходов
        val positions  = FloatArray(PARTICLE_COUNT * 2) // [x0,y0, x1,y1, ...]
        val amplitudes = FloatArray(PARTICLE_COUNT)
        val colors     = Array(PARTICLE_COUNT) { Color.Transparent }

        for (i in 0 until PARTICLE_COUNT) {
            val t         = i.toFloat() / PARTICLE_COUNT
            val angle     = t * PI.toFloat() * 2f - PI.toFloat() / 2f
            val bIdx      = (t * spec.size).toInt().coerceIn(0, spec.lastIndex)
            val amp       = spec[bIdx]
            val r         = radius * (1f + amp * 0.36f + bass * 0.10f)
            positions[i * 2]     = cx + cos(angle) * r
            positions[i * 2 + 1] = cy + sin(angle) * r
            amplitudes[i]  = amp
            colors[i]      = lerp(CYAN, MAGENTA, t)
        }

        // ── Проход 1: Halos (glow ring) ───────────────────────────────
        for (i in 0 until PARTICLE_COUNT) {
            val amp   = amplitudes[i]
            val color = colors[i]
            val pos   = Offset(positions[i * 2], positions[i * 2 + 1])

            // Внешний ореол — соседние halos overlap → непрерывное кольцо glow
            drawCircle(
                color     = color.copy(alpha = 0.20f + amp * 0.30f),
                radius    = 12f + amp * 16f,
                center    = pos,
                blendMode = BlendMode.Screen,
            )
            // Средний ореол — ближе к ядру, ярче
            drawCircle(
                color     = color.copy(alpha = 0.50f + amp * 0.35f),
                radius    = 5f + amp * 8f,
                center    = pos,
                blendMode = BlendMode.Screen,
            )
        }

        // ── Проход 2: Cores (дискретные точки) ────────────────────────
        for (i in 0 until PARTICLE_COUNT) {
            val amp   = amplitudes[i]
            val color = colors[i]
            val pos   = Offset(positions[i * 2], positions[i * 2 + 1])

            // Яркое цветное ядро — основной видимый элемент "частицы"
            drawCircle(
                color     = color.copy(alpha = 0.92f),
                radius    = 3f + amp * 5f,
                center    = pos,
                blendMode = BlendMode.Screen,
            )
            // Белое горячее центро — даёт impression пересвеченной точки
            drawCircle(
                color     = Color.White.copy(alpha = 0.78f + amp * 0.22f),
                radius    = 1.5f + amp * 2.5f,
                center    = pos,
                blendMode = BlendMode.Screen,
            )
        }

        // ── Внешнее рассеянное облако ─────────────────────────────────
        repeat(100) { i ->
            val t   = i.toFloat() / 100f
            val ang = t * PI.toFloat() * 2f
            val amp = spec[(i * 2) % spec.size]
            val scatter = 1.24f + (i % 5) * 0.05f + amp * 0.32f
            drawCircle(
                color = lerp(CYAN, MAGENTA, t).copy(alpha = 0.09f + amp * 0.50f),
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
