package com.ediskrad.audiovisualizer.visualizer

/**
 * Состояние одной частицы-искры для bass-burst эффекта.
 * Все поля неизменяемы после создания — позиция вычисляется в Canvas
 * из (currentTimeMs - startMs) каждый кадр.
 */
data class SparkState(
    val angle: Float,         // фиксированный угол на окружности, радианы
    val speed: Float,         // скорость, доли radius в мс
    val startMs: Long,        // System.currentTimeMillis() в момент рождения
    val maxLifeMs: Long,      // время жизни в мс
    val colorFraction: Float, // 0 = CYAN (#3FC6FF), 1 = MAGENTA (#E040FB)
    val size: Float,          // начальный радиус в dp
)
