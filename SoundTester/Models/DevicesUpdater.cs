using System.Collections.ObjectModel;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace SoundTesting
{
    public class
        DevicesUpdater : IMMNotificationClient //Кастомный интерфейс уведомлений об изменении количества и состояния подключенных аудиоустройств
    {
        public ObservableCollection<string> InputDevices { get; set; } = new();
        public ObservableCollection<string> OutputDevices { get; set; } = new();

        public DevicesUpdater()
        {
            DeviceListUpdate();
        }

        private void DeviceListUpdate() //Обновление данных об устройствах
        {
                
            InputDevices?.Clear();
            OutputDevices?.Clear();

            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();

            var inputs = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
            foreach (var device in inputs)
            {
                InputDevices.Add( device.FriendlyName );
                device.Dispose();
            }

            var outputs = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            foreach (var device in outputs)
            {
                OutputDevices.Add( device.FriendlyName );
                device.Dispose();
            }
            enumerator.Dispose();
        }

        public void OnDeviceStateChanged(string deviceId, DeviceState newState) //Изменение состояния устройства
        {
        }

        public void OnDeviceAdded(string deviceId) //Добавление устройства
        {
            DeviceListUpdate();
        }

        public void OnDeviceRemoved(string deviceId) //Удаление устройства
        {
            DeviceListUpdate();
        }

        public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId) //Изменение устройства по-умолчанию
        {
            DeviceListUpdate();
        }

        public void OnPropertyValueChanged(string deviceId, PropertyKey key) //Изменение свойства устройства
        {
        }
    }
}