using StardewModdingAPI.Utilities;

namespace StardewAudioDeviceRefresh;

public sealed class ModConfig
{
    public KeybindList RefreshAudioKey { get; set; } = KeybindList.Parse("F8");
    public KeybindList TestSoundKey { get; set; } = KeybindList.Parse("F9");

    public string TestSoundName { get; set; } = "coin";

    public bool PulseVolumeBeforeReopen { get; set; } = true;
    public bool RestartCurrentMusic { get; set; } = true;
    public bool PlayTestSoundAfterRefresh { get; set; } = true;

    public int DelayedTestSoundTicks { get; set; } = 30;
    public float TemporaryVolume { get; set; } = 0.0f;

    public bool AutoRefreshOnDefaultDeviceChange { get; set; } = false;
    public int AutoRefreshDelayTicks { get; set; } = 60;
    public bool PlayTestSoundAfterAutoRefresh { get; set; } = false;
}