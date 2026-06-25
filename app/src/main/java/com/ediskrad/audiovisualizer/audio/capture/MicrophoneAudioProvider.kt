package com.ediskrad.audiovisualizer.audio.capture

import android.media.AudioFormat
import android.media.AudioRecord
import android.media.MediaRecorder
import com.ediskrad.audiovisualizer.audio.VisualizerConfig

class MicrophoneAudioProvider(
    config: VisualizerConfig = VisualizerConfig(),
) : BaseAudioProvider(config) {
    private var audioRecord: AudioRecord? = null

    override fun setup() {
        val minBuffer = AudioRecord.getMinBufferSize(
            config.sampleRate,
            AudioFormat.CHANNEL_IN_MONO,
            AudioFormat.ENCODING_PCM_16BIT,
        )
        audioRecord = AudioRecord(
            MediaRecorder.AudioSource.MIC,
            config.sampleRate,
            AudioFormat.CHANNEL_IN_MONO,
            AudioFormat.ENCODING_PCM_16BIT,
            maxOf(minBuffer, config.fftSize * 4),
        ).also { it.startRecording() }
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
