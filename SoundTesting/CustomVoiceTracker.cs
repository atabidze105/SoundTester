using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using NAudio.Wave;

namespace SoundTesting
{
    internal class CustomVoiceTracker : IDisposable
    {
        private readonly WaveInEvent _waveIn;
        private float _minDb = -96; // Минимальный уровень дб
        private float _currentDb;

        public event Action<float> DbUpdated; // Событие обновления уровня дБ

        public CustomVoiceTracker(int deviceNumber)
        {
            _waveIn = new WaveInEvent
            {
                DeviceNumber = deviceNumber,
                WaveFormat = new WaveFormat(44100, 1), // 44.1 кГц, моно
                BufferMilliseconds = 50 // Буфер 50 мс
            };

            _waveIn.DataAvailable += OnDataAvailable;
        }

        public float CurrentDB => _currentDb;
        public string Color => "Red";

        public void Start() => _waveIn.StartRecording();
        public void Stop() => _waveIn.StopRecording();

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            // Преобразуем байты в массив float (от -1.0 до 1.0)
            float[] samples = new float[e.BytesRecorded / 2];
            for (int i = 0; i < e.BytesRecorded; i += 2)
            {
                short sample = BitConverter.ToInt16(e.Buffer, i);
                samples[i / 2] = sample / 32768f; // Нормализация до [-1, 1]
            }

            // Вычисляем RMS (среднеквадратичное значение)
            float sum = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                sum += samples[i] * samples[i];
            }
            float rms = (float)Math.Sqrt(sum / samples.Length);

            // Переводим RMS в децибелы (дБ)
            _currentDb = rms > 0 ? 20 * (float)Math.Log10(rms) : _minDb;

            DbUpdated?.Invoke(_currentDb);
        }

        public void Dispose() => _waveIn.Dispose();
    }
}
