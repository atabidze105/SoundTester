using System;
using System.IO;
using System.Linq;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace SoundTesting
{
    class AudioRecorder
    {
        private WaveInEvent waveIn;
        
        private WaveFileWriter writer;
        private string outputFilePath = "RecordedAudio/recorded.wav";

        // Метод для начала записи с указанного устройства
        public void StartRecording(int deviceNumber)
        {
            waveIn = new WaveInEvent
            {
                DeviceNumber = deviceNumber, // Указываем номер устройства
                WaveFormat = new WaveFormat(44100, 1) // 44.1kHz, моно
            };

            waveIn.DataAvailable += OnDataAvailable;
            waveIn.RecordingStopped += OnRecordingStopped;

            writer = new WaveFileWriter(outputFilePath, waveIn.WaveFormat);

            waveIn.StartRecording();
        }

        // Метод для остановки записи
        public void StopRecording()
        {
            waveIn?.StopRecording();
        }

        // Метод для воспроизведения записанного аудио
        public void PlayRecordedAudio()
        {
            using (var audioFile = new AudioFileReader(outputFilePath))
            using (var outputDevice = new WaveOutEvent())
            {
                outputDevice.Init(audioFile);
                outputDevice.Play();
                while (outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    System.Threading.Thread.Sleep(500);
                }
            }
        }

        // Обработчик события поступления данных
        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            writer.Write(e.Buffer, 0, e.BytesRecorded);
        }

        // Обработчик события остановки записи
        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            writer.Dispose();
            writer = null;
            waveIn.Dispose();
        }
    }

}