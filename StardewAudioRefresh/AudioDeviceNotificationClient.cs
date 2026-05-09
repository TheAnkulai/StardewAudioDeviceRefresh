using System;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using StardewModdingAPI;

namespace StardewAudioDeviceRefresh;

public sealed class AudioDeviceNotificationClient : IMMNotificationClient
{
    private readonly IMonitor Monitor;
    private readonly Action<string?> OnDefaultDeviceChangedCallback;

    public AudioDeviceNotificationClient(
        IMonitor monitor,
        Action<string?> onDefaultDeviceChanged
    )
    {
        this.Monitor = monitor;
        this.OnDefaultDeviceChangedCallback = onDefaultDeviceChanged;
    }

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        if (flow != DataFlow.Render)
            return;

        if (role != Role.Multimedia)
            return;

        this.Monitor.Log(
            $"Windows default multimedia render device changed: {defaultDeviceId}",
            LogLevel.Trace
        );

        this.OnDefaultDeviceChangedCallback(defaultDeviceId);
    }

    public void OnDeviceAdded(string pwstrDeviceId)
    {
    }

    public void OnDeviceRemoved(string deviceId)
    {
    }

    public void OnDeviceStateChanged(string deviceId, DeviceState newState)
    {
    }

    public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
    {
    }
}