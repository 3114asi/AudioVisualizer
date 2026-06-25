package com.ediskrad.audiovisualizer

import android.media.projection.MediaProjection
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow

object MediaProjectionRepository {
    private val _projection = MutableStateFlow<MediaProjection?>(null)
    val projection: StateFlow<MediaProjection?> = _projection.asStateFlow()

    fun setProjection(mediaProjection: MediaProjection?) {
        _projection.value = mediaProjection
    }

    fun clear() {
        _projection.value?.stop()
        _projection.value = null
    }
}
