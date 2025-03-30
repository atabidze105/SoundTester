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

    private const int sample_rate = 44100; //������� �������������, 44,1 ���, �.�. �� ������ ������� ������������ 44,1 ���. ������� (���������)
    private const short bits_per_sample = 16; //���-�� ��� �� �����, �.�. � ������ �� 44,1 ���. ������� ����� 16 ��� ��� �������� ������

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
        short[] wave = new short[sample_rate]; //������ �������
        byte[] binaryWave = new byte[sample_rate * sizeof(short)]; //������ �������, ��������������� � �����, ��� ������ � WAVE-������

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

        if (chbox_whtenoise.IsChecked == true) //����� ���
        {
            for (int i = 0; i < sample_rate; i++)
            {
                wave[i] += Convert.ToInt16(_Random.Next(-short.MaxValue, short.MaxValue) / checkboxCount);
            }
        }



        Buffer.BlockCopy(wave, 0,binaryWave, 0, wave.Length * sizeof(short)); //����������� ������� short-�� � ������ ������, ��� ������ � WAVE-����

        using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream)) //���������� � ������������ ���� ������� WAVE
            {
                short numChannels = 1;
                short blockAlign = bits_per_sample / 8;
                int subChunkTwoSize = sample_rate * numChannels * blockAlign;
                binaryWriter.Write(new[] { 'R', 'I', 'F', 'F' }); //������������� ���������
                binaryWriter.Write(36 + subChunkTwoSize); //������ ���������
                binaryWriter.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' }); //������ � ������������� ������������
                binaryWriter.Write(16); //������ ������������ (16 ��� PCM)
                binaryWriter.Write((short)1); //������ ���� (1 ��� PCM)
                binaryWriter.Write(numChannels); //���������� �������
                binaryWriter.Write(sample_rate); //������� �������������
                binaryWriter.Write(sample_rate * numChannels * blockAlign); //��������
                binaryWriter.Write(blockAlign); //������������ �����
                binaryWriter.Write(bits_per_sample); //���� �� �����
                binaryWriter.Write(new[] { 'd', 'a', 't', 'a' }); //��������� "data", �������� ������ ������ � ��� ����
                binaryWriter.Write(subChunkTwoSize); //���-�� ������ ������
                binaryWriter.Write(binaryWave); //���� �������� ������
                memoryStream.Position = 0;
                new SoundPlayer(memoryStream).Play();
;           }
    }
}