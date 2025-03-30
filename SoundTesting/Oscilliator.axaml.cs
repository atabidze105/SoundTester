using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;

namespace SoundTesting;

public partial class Oscilliator : UserControl
{
    

    private Random _Random = new Random();
    
    private float _FrequencySine = 440f; 
    private float _FrequencySquare = 440f; 
    private float _FrequencySaw = 440f; 
    private float _FrequencyTriangle = 440f;

    private const int sample_rate = 44100; //частота дискретизации, 44,1 гГц, т.е. на каждую секунду генерируется 44,1 тыс. семплов (амплитуда)
    private const short bits_per_sample = 16; //кол-во бит на семпл, т.е. в каждом из 44,1 тыс. семплов будет 16 бит для хранения данных

    public Oscilliator()
    {
        InitializeComponent();


        slider_sine.DataContext = _FrequencySine;
        slider_square.DataContext = _FrequencySquare;
        slider_sawtooth.DataContext = _FrequencySaw;
        slider_triangle.DataContext = _FrequencyTriangle;
    }

    private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        short[] wave = new short[sample_rate]; //Данные графика
        byte[] binaryWave = new byte[sample_rate * sizeof(short)]; //Данные графика, преобразованные в байты, для записи в WAVE-формат

        int samplesPerWaveLength;
        short ampStep;
        short tempSample;

        List<CheckBox> checkBoxes = grid_oscilliator.GetVisualDescendants().OfType<CheckBox>().Where(x => x.IsChecked == true).ToList(); 
        int checkboxCount = checkBoxes.Count;


        if (chbox_sine.IsChecked == true) //Sine
        {
            for (int i = 0; i < sample_rate; i++)
            {
                wave[i] += Convert.ToInt16((short.MaxValue * Math.Sin(((Math.PI * 2 * _FrequencySine) / sample_rate) * i)) / checkboxCount); 
            }
        }
            
        if (chbox_square.IsChecked == true) //Square
        {
            for (int i = 0; i < sample_rate; i++)
            {
                wave[i] += Convert.ToInt16((short.MaxValue * Math.Sign(Math.Sin((Math.PI * 2 * _FrequencySquare) / sample_rate * i))) / checkboxCount);
            }
        }
            
        if (chbox_sawtooth.IsChecked == true) //Sawtooth
        {
            samplesPerWaveLength = (int)(sample_rate / _FrequencySaw);
            ampStep = (short)((short.MaxValue * 2) / samplesPerWaveLength);

            for (int i = 0; i < sample_rate; i++)
            {
                tempSample = -short.MaxValue;
                for (int j = 0; j < samplesPerWaveLength && i < sample_rate; j++)
                {
                    tempSample += ampStep;
                    wave[i++] += Convert.ToInt16(tempSample / checkboxCount);
                }
                i--;
            }
        }
            
        if (chbox_triangle.IsChecked == true) //Triangle
        {
            samplesPerWaveLength = (int)(sample_rate / _FrequencyTriangle);
            ampStep = (short)((short.MaxValue * 2) / samplesPerWaveLength);

            tempSample = -short.MaxValue;
            for (int i = 0; i < sample_rate; i++)
            {
                if (Math.Abs(tempSample + ampStep) > short.MaxValue)
                {
                    ampStep = (short) - ampStep;
                }
                tempSample += ampStep;
                wave[i] += Convert.ToInt16(tempSample / checkboxCount);
            }
        }

        if (chbox_whtenoise.IsChecked == true) //Белый шум
        {
            for (int i = 0; i < sample_rate; i++)
            {
                wave[i] += Convert.ToInt16(_Random.Next(-short.MaxValue, short.MaxValue) / checkboxCount);
            }
        }



        Buffer.BlockCopy(wave, 0,binaryWave, 0, wave.Length * sizeof(short)); //Конвертация массива short-ов в массив байтов, для записи в WAVE-файл

        using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream)) //приведение к стандартному виду формата WAVE
            {
                short numChannels = 1;
                short blockAlign = bits_per_sample / 8;
                int subChunkTwoSize = sample_rate * numChannels * blockAlign;
                binaryWriter.Write(new[] { 'R', 'I', 'F', 'F' }); //Идентификатор фрагмента
                binaryWriter.Write(36 + subChunkTwoSize); //Размер фрагмента
                binaryWriter.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' }); //Формат и идентификатор подфрагмента
                binaryWriter.Write(16); //Размер подфрагмента (16 для PCM)
                binaryWriter.Write((short)1); //формат удио (1 для PCM)
                binaryWriter.Write(numChannels); //Количество каналов
                binaryWriter.Write(sample_rate); //частота дискретизации
                binaryWriter.Write(sample_rate * numChannels * blockAlign); //Байтрейт
                binaryWriter.Write(blockAlign); //Выравнивание блока
                binaryWriter.Write(bits_per_sample); //Биты на сэмпл
                binaryWriter.Write(new[] { 'd', 'a', 't', 'a' }); //Подраздел "data", содержит размер данных и сам звук
                binaryWriter.Write(subChunkTwoSize); //Кол-во байтов данных
                binaryWriter.Write(binaryWave); //Сами звуковые данные
                memoryStream.Position = 0;
                new SoundPlayer(memoryStream).Play();
;           }
    }
}