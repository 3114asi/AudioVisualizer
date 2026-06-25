package com.ediskrad.audiovisualizer.ui

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp

@Composable
fun StatPill(label: String) {
    Box(
        modifier = Modifier
            .background(Color(0x331E2240), RoundedCornerShape(999.dp))
            .padding(horizontal = 14.dp, vertical = 10.dp),
    ) {
        Text(label, color = Color(0xFFD4D8EA))
    }
}
