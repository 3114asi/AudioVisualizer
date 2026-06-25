package com.ediskrad.audiovisualizer.audio.capture

import android.media.AudioAttributes
import android.media.AudioFormat
import android.media.AudioPlaybackCaptureConfiguration
import android.media.AudioRecord
import android.media.projection.MediaProjection
import com.ediskrad.audiovisualizer.audio.VisualizerConfig

class PlaybackCaptureAudioProvider(
    private val mediaProjection: MediaProjection,
    config: VisualizerConfig = VisualizerConfig(),
) : BaseAudioProvider(config) {
    private var audioRecord: AudioRecord? = null

    override fun setup() {
        val captureConfig = AudioPlaybackCaptureConfiguration.Builder(mediaProjection)
            .addMatchingUsage(AudioAttributes.USAGE_MEDIA)
            .addMatchingUsage(AudioAttributes.USAGE_GAME)
            .build()

        val format = AudioFormat.Builder()
            .setEncoding(AudioFormat.ENCODING_PCM_16BIT)
            .setSampleRate(config.sampleRate)
            .setChannelMask(AudioFormat.CHANNEL_IN_MONO)
            .build()

        val minBuffer = AudioRecord.getMinBufferSize(
            config.sampleRate,
            AudioFormat.CHANNEL_IN_MONO,
            AudioFormat.ENCODING_PCM_16BIT,
        )

        audioRecord = AudioRecord.Builder()
            .setAudioFormat(format)
            .setAudioPlaybackCaptureConfig(captureConfig)
            .setBufferSizeInBytes(maxOf(minBuffer, config.fftSize * 4))
            .build()
            .also { it.startRecording() }
    }

    override fun read(target: ShortArray): Int {
        return audioRecord?.read(target, 0, target.size, AudioRecord.READ_BLOCKING) ?: 0
    }

    override fun teardown() {
        audioRecord?.stop()
        audioRecord?.release()
        audioRecord = null
    }
}
