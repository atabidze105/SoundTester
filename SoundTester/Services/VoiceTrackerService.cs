using System;
using System.Linq;
using Avalonia.Threading;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using SoundTesting;

namespace SoundTester.Service;

public class VoiceTrackerService
{
    private WasapiCapture _wasapiCapture;
    private WasapiOut _wasapiOut;
    private BufferedWaveProvider _bufferedWaveProvider;
    private DevicesEnumerator _devicesEnumerator;
    private float _volume = -96;
    public delegate void DecibelLevelHandler(float level);
    public event DecibelLevelHandler? DecibelLevelEvent;
    public VoiceTrackerService(string deviceName)
    {  
        
        var en = new MMDeviceEnumerator(); //Костыль
        var inD = en.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).FirstOrDefault(x => x.FriendlyName == deviceName);
        var outD = en.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        
        _wasapiCapture = new WasapiCapture(inD, false, 50);
        _wasapiOut = new WasapiOut(outD, AudioClientShareMode.Shared, false, 50);
        _wasapiCapture.DataAvailable += OnDataAvailable;
        _bufferedWaveProvider = new BufferedWaveProvider(_wasapiCapture?.WaveFormat){ DiscardOnBufferOverflow = true };
        _wasapiOut.Init(_bufferedWaveProvider);
    }

    public void Record()
    {
        _wasapiCapture.StartRecording();
        _wasapiOut.Play();
    }
   
   private void OnDataAvailable(object sender, WaveInEventArgs e)
   {
       float[] samples = new float[e.BytesRecorded / 2];
       for (int i = 0; i < e.BytesRecorded; i += 2)
       {
           short sample = BitConverter.ToInt16(e.Buffer, i);
           samples[i / 2] = sample / 32768f;
       }

       float sum = 0;
       for (int i = 0; i < samples.Length; i++)
       {
           sum += samples[i] * samples[i];
       }

       float rms = (float)Math.Sqrt(sum / samples.Length);

       float res = rms > 0 ? 20 * (float)Math.Log10(rms) : -96;
       DecibelLevelEvent?.Invoke(res);
       _bufferedWaveProvider.AddSamples(e.Buffer, 0, e.BytesRecorded); //Добавление данных для мониторинга микрофона
   }
}