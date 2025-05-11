using NAudio.CoreAudioApi;
using ReactiveUI;

namespace SoundTesting;

public class  DevicesEnumerator : MMDeviceEnumerator
{
    private DevicesUpdater _devicesUpdater = new DevicesUpdater();

    public DevicesUpdater DevicesUpdater => _devicesUpdater;
    public DevicesEnumerator()
    {
        this.RegisterEndpointNotificationCallback(_devicesUpdater);
    }
}