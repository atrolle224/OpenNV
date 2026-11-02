using Godot;

namespace OpenNV.Runtime.Presentation.Ui;

/// <summary>Handheld Android: storage access, folder rediscovery, controller keys and a frontend's start-game extra.</summary>
internal sealed partial class NativeGodotLauncher
{
    private const string AutostartContinue = "--opennv-continue";
    private const string AutostartNewGame = "--opennv-new-game";
    private bool _autostartChecked;

    private void ReadyAndroid()
    {
        if (!OS.HasFeature("android")) return;
        // Owned game folders live in shared storage; Android grants that through
        // its "All files access" screen, which returns here as a resume.
        GetTree().OnRequestPermissionsResult += (_, _) => RediscoverAndroidInstallations();
        OS.RequestPermissions();
        CallDeferred(MethodName.AndroidAutostart);
    }

    private void RediscoverAndroidInstallations()
    {
        _profiles.AutoDiscoverInstallations();
        Refresh();
    }

    private void NotificationAndroid(int what)
    {
        if (what == NotificationApplicationResumed && OS.HasFeature("android"))
            RediscoverAndroidInstallations();
    }

    private void AndroidAutostart()
    {
        if (_autostartChecked) return;
        _autostartChecked = true;
        var arguments = OS.GetCmdlineArgs().Concat(OS.GetCmdlineUserArgs()).ToArray();
        if (arguments.Contains(AutostartContinue))
            LaunchSelected(_continue.Disabled ? NativeGodotLauncherEntry.Menu : NativeGodotLauncherEntry.Continue);
        else if (arguments.Contains(AutostartNewGame))
            LaunchSelected(NativeGodotLauncherEntry.NewGame);
        else
            _launch.CallDeferred(Control.MethodName.GrabFocus);
    }

    /// <summary>Start launches, Y picks a folder, B closes a dialog. A and the d-pad move focus as usual.</summary>
    private bool HandleJoypad(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventJoypadButton { Pressed: true } button) return false;
        switch (button.ButtonIndex)
        {
            case JoyButton.Start when _fileDialog is null && _stackWindow is null:
                LaunchSelected();
                return true;
            case JoyButton.Y when _fileDialog is null && _stackWindow is null:
                ChooseInstallation();
                return true;
            case JoyButton.B when _fileDialog is not null:
                CloseFileDialog();
                return true;
            case JoyButton.B when _stackWindow is not null:
                CloseStackWindow();
                return true;
            default:
                return false;
        }
    }

    private static string? AndroidFolderPickerStart()
    {
        if (!OS.HasFeature("android")) return null;
        var games = "/storage/emulated/0/Games";
        return Directory.Exists(games) ? games : "/storage/emulated/0";
    }
}
