package com.ediskrad.audiovisualizer.ui

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp

@Composable
fun ControlPill(
    label: String,
    isActive: Boolean,
    onClick: () -> Unit,
) {
    val colors = if (isActive) {
        listOf(Color(0xFF2AFFF4), Color(0xFFBF44FF))
    } else {
        listOf(Color(0xFF30364D), Color(0xFF171B28))
    }

    Box(
        modifier = Modifier
            .background(Brush.horizontalGradient(colors), RoundedCornerShape(999.dp))
            .clickable(onClick = onClick)
            .padding(horizontal = 18.dp, vertical = 10.dp),
    ) {
        Text(label, color = Color.White)
    }
}
