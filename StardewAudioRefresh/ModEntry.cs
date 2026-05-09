using System;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework.Audio;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace StardewAudioDeviceRefresh;

public sealed class ModEntry : Mod
{
    [DllImport("soft_oal.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "alcIsExtensionPresent")]
    private static extern bool AlcIsExtensionPresent(IntPtr device, string extensionName);

    [DllImport("soft_oal.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "alcReopenDeviceSOFT")]
    private static extern bool AlcReopenDeviceSOFT(IntPtr device, IntPtr deviceName, IntPtr attribs);

    private ModConfig Config = null!;
    private int PendingTestSoundTicks = 0;

    public override void Entry(IModHelper helper)
    {
        this.Config = helper.ReadConfig<ModConfig>();

        helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;

        this.Monitor.Log(
            $"Stardew Audio Device Refresh loaded. Press {this.Config.RefreshAudioKey} to refresh audio, {this.Config.TestSoundKey} to test sound.",
            LogLevel.Info
        );
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        this.RegisterGenericModConfigMenu();
    }

    private void RegisterGenericModConfigMenu()
    {
        IGenericModConfigMenuApi? configMenu =
            this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");

        if (configMenu is null)
        {
            this.Monitor.Log("Generic Mod Config Menu not found; using config.json only.", LogLevel.Trace);
            return;
        }

        configMenu.Register(
            mod: this.ModManifest,
            reset: () => this.Config = new ModConfig(),
            save: () => this.Helper.WriteConfig(this.Config),
            titleScreenOnly: false
        );

        configMenu.AddSectionTitle(
            mod: this.ModManifest,
            text: () => "Controls"
        );

        configMenu.AddKeybindList(
            mod: this.ModManifest,
            getValue: () => this.Config.RefreshAudioKey,
            setValue: value => this.Config.RefreshAudioKey = value,
            name: () => "Refresh audio device key",
            tooltip: () => "Reopens Stardew Valley's OpenAL audio device. Use this after changing the Windows output device."
        );

        configMenu.AddKeybindList(
            mod: this.ModManifest,
            getValue: () => this.Config.TestSoundKey,
            setValue: value => this.Config.TestSoundKey = value,
            name: () => "Test sound key",
            tooltip: () => "Plays a test sound to check which output device Stardew Valley is using."
        );

        configMenu.AddSectionTitle(
            mod: this.ModManifest,
            text: () => "Audio refresh"
        );

        configMenu.AddBoolOption(
            mod: this.ModManifest,
            getValue: () => this.Config.PulseVolumeBeforeReopen,
            setValue: value => this.Config.PulseVolumeBeforeReopen = value,
            name: () => "Pulse volume before refresh",
            tooltip: () => "Briefly changes SoundEffect.MasterVolume before reopening the OpenAL device. Usually safe to leave enabled."
        );

        configMenu.AddBoolOption(
            mod: this.ModManifest,
            getValue: () => this.Config.RestartCurrentMusic,
            setValue: value => this.Config.RestartCurrentMusic = value,
            name: () => "Try to restart current music",
            tooltip: () => "Attempts to restart the current music after refreshing the audio device."
        );

        configMenu.AddBoolOption(
            mod: this.ModManifest,
            getValue: () => this.Config.PlayTestSoundAfterRefresh,
            setValue: value => this.Config.PlayTestSoundAfterRefresh = value,
            name: () => "Play test sound after refresh",
            tooltip: () => "Plays the configured test sound after refreshing the audio device."
        );

        configMenu.AddTextOption(
            mod: this.ModManifest,
            getValue: () => this.Config.TestSoundName,
            setValue: value => this.Config.TestSoundName = value,
            name: () => "Test sound name",
            tooltip: () => "Internal Stardew Valley sound name to play for testing. Default: coin."
        );

        configMenu.AddNumberOption(
            mod: this.ModManifest,
            getValue: () => this.Config.DelayedTestSoundTicks,
            setValue: value => this.Config.DelayedTestSoundTicks = value,
            name: () => "Test sound delay",
            tooltip: () => "Delay before playing the test sound after refresh, in game ticks. 60 ticks is about one second.",
            min: 1,
            max: 180,
            interval: 1
        );

        configMenu.AddNumberOption(
            mod: this.ModManifest,
            getValue: () => this.Config.TemporaryVolume,
            setValue: value => this.Config.TemporaryVolume = value,
            name: () => "Temporary pulse volume",
            tooltip: () => "Temporary SoundEffect.MasterVolume used during pulse. Usually leave at 0.",
            min: 0f,
            max: 1f,
            interval: 0.05f
        );

        configMenu.AddParagraph(
            mod: this.ModManifest,
            text: () => "Recommended use: switch your Windows output device, then press the refresh key. Avoid using old hard reset methods; this mod uses OpenAL Soft device reopen instead."
        );
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (this.PendingTestSoundTicks <= 0)
            return;

        this.PendingTestSoundTicks--;

        if (this.PendingTestSoundTicks == 0)
        {
            this.Monitor.Log("Playing delayed test sound after audio reset.", LogLevel.Info);
            this.PlayTestSound();
        }
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (this.Config.RefreshAudioKey.JustPressed())
        {
            this.RefreshAudio();
            return;
        }

        if (this.Config.TestSoundKey.JustPressed())
        {
            this.PlayTestSound();
            return;
        }
    }

    private void RefreshAudio()
    {
        this.Monitor.Log("Audio refresh requested.", LogLevel.Info);

        try
        {
            if (this.Config.PulseVolumeBeforeReopen)
                this.PulseSoundEffectVolume();

            bool reopened = this.TryReopenOpenALDevice();
            this.Monitor.Log($"OpenAL reopen attempt finished. reopened={reopened}", LogLevel.Info);

            if (this.Config.RestartCurrentMusic)
                this.TryRestartCurrentMusic();

            if (this.Config.PlayTestSoundAfterRefresh)
            {
                this.PendingTestSoundTicks = Math.Max(1, this.Config.DelayedTestSoundTicks);
                this.Monitor.Log("Delayed test sound scheduled.", LogLevel.Info);
            }

            this.Monitor.Log("Audio refresh attempt finished.", LogLevel.Info);
        }
        catch (Exception ex)
        {
            this.Monitor.Log($"Audio refresh failed: {ex}", LogLevel.Error);
        }
    }

    private bool TryReopenOpenALDevice()
    {
        try
        {
            Type? controllerType = Type.GetType(
                "Microsoft.Xna.Framework.Audio.OpenALSoundController, MonoGame.Framework"
            );

            if (controllerType is null)
            {
                this.Monitor.Log("OpenALSoundController type not found.", LogLevel.Warn);
                return false;
            }

            FieldInfo? instanceField = controllerType.GetField(
                "_instance",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
            );

            FieldInfo? deviceField = controllerType.GetField(
                "_device",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            );

            if (instanceField is null || deviceField is null)
            {
                this.Monitor.Log("Could not find OpenALSoundController._instance or _device.", LogLevel.Warn);
                return false;
            }

            object? instance = instanceField.GetValue(null);

            if (instance is null)
            {
                this.Monitor.Log("OpenALSoundController._instance is null.", LogLevel.Warn);
                return false;
            }

            IntPtr device = (IntPtr)(deviceField.GetValue(instance) ?? IntPtr.Zero);

            this.Monitor.Log($"Current OpenAL device pointer: 0x{device.ToInt64():X}", LogLevel.Info);

            if (device == IntPtr.Zero)
            {
                this.Monitor.Log("OpenAL device pointer is zero.", LogLevel.Warn);
                return false;
            }

            bool hasReopenExtension = AlcIsExtensionPresent(device, "ALC_SOFT_reopen_device");

            this.Monitor.Log($"ALC_SOFT_reopen_device present: {hasReopenExtension}", LogLevel.Info);

            if (!hasReopenExtension)
            {
                this.Monitor.Log("This OpenAL Soft build doesn't support alcReopenDeviceSOFT.", LogLevel.Warn);
                return false;
            }

            this.Monitor.Log("Calling alcReopenDeviceSOFT(device, null, null)...", LogLevel.Warn);

            bool result = AlcReopenDeviceSOFT(device, IntPtr.Zero, IntPtr.Zero);

            this.Monitor.Log($"alcReopenDeviceSOFT result: {result}", LogLevel.Info);

            return result;
        }
        catch (DllNotFoundException ex)
        {
            this.Monitor.Log($"soft_oal.dll not found: {ex.Message}", LogLevel.Error);
            return false;
        }
        catch (EntryPointNotFoundException ex)
        {
            this.Monitor.Log($"OpenAL Soft function not found: {ex.Message}", LogLevel.Error);
            return false;
        }
        catch (Exception ex)
        {
            this.Monitor.Log($"TryReopenOpenALDevice failed: {ex}", LogLevel.Error);
            return false;
        }
    }

    private void PulseSoundEffectVolume()
    {
        try
        {
            float oldVolume = SoundEffect.MasterVolume;

            this.Monitor.Log($"Current SoundEffect.MasterVolume = {oldVolume}", LogLevel.Trace);

            SoundEffect.MasterVolume = this.Config.TemporaryVolume;

            SoundEffect.MasterVolume = oldVolume;

            this.Monitor.Log("SoundEffect master volume pulsed.", LogLevel.Trace);
        }
        catch (Exception ex)
        {
            this.Monitor.Log($"Could not pulse SoundEffect.MasterVolume: {ex.Message}", LogLevel.Warn);
        }
    }

    private void PlayTestSound()
    {
        try
        {
            Game1.playSound(this.Config.TestSoundName);
            this.Monitor.Log($"Played test sound: {this.Config.TestSoundName}", LogLevel.Info);
        }
        catch (Exception ex)
        {
            this.Monitor.Log($"Could not play test sound '{this.Config.TestSoundName}': {ex.Message}", LogLevel.Warn);
        }
    }

    private void TryRestartCurrentMusic()
    {
        try
        {
            object? currentSong = GetStaticField(typeof(Game1), "currentSong");

            if (currentSong is null)
            {
                this.Monitor.Log("Game1.currentSong is null; no music to restart.", LogLevel.Trace);
                return;
            }

            this.Monitor.Log($"Found current song object: {currentSong.GetType().FullName}", LogLevel.Trace);

            bool stopped = TryCallMethod(currentSong, "Stop")
                || TryCallMethod(currentSong, "stop")
                || TryCallMethod(currentSong, "Pause")
                || TryCallMethod(currentSong, "pause");

            bool resumed = TryCallMethod(currentSong, "Resume")
                || TryCallMethod(currentSong, "resume")
                || TryCallMethod(currentSong, "Play")
                || TryCallMethod(currentSong, "play");

            this.Monitor.Log(
                $"Music restart attempt finished. stopped={stopped}, resumed={resumed}",
                LogLevel.Info
            );
        }
        catch (Exception ex)
        {
            this.Monitor.Log($"Could not restart current music: {ex.Message}", LogLevel.Warn);
        }
    }

    private static object? GetStaticField(Type type, string fieldName)
    {
        FieldInfo? field = type.GetField(
            fieldName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
        );

        return field?.GetValue(null);
    }

    private static bool TryCallMethod(object target, string methodName)
    {
        MethodInfo? method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null
        );

        if (method is null)
            return false;

        method.Invoke(target, null);
        return true;
    }
}