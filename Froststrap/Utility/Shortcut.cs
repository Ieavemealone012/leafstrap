// SPDX-FileCopyrightText: 2026 Froststrap
// Copyright (C) Froststrap Team
//
// SPDX-License-Identifier: MPL-2.0

using System.Runtime.InteropServices;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Froststrap.Utility
{
    internal static class Shortcut
    {
        private static GenericTriState _loadStatus = GenericTriState.Unknown;

        private static string? _froststrapIconPath;
        private static readonly Lock _iconLock = new();

        public static string GetFroststrapIconPath()
        {
            if (_froststrapIconPath != null && File.Exists(_froststrapIconPath))
                return _froststrapIconPath;

            lock (_iconLock)
            {
                if (_froststrapIconPath != null && File.Exists(_froststrapIconPath))
                    return _froststrapIconPath;

                try
                {
                    string iconPath = Path.Combine(Paths.Base, "froststrap.png");

                    if (File.Exists(iconPath))
                    {
                        _froststrapIconPath = iconPath;
                        return iconPath;
                    }

                    var uri = new Uri("avares://Leafstrap/Leafstrap.png");
                    using var pngStream = AssetLoader.Open(uri);
                    _ = pngStream ?? throw new FileNotFoundException("Embedded Leafstrap.png not found.");

                    using var fileStream = File.Create(iconPath);
                    pngStream.CopyTo(fileStream);

                    _froststrapIconPath = iconPath;
                    return iconPath;
                }
                catch (Exception ex)
                {
                    App.Logger.Error($"Failed to extract icon: {ex.Message}");
                    return "application-x-executable";
                }
            }
        }

        public static string ResolvePath(string lnkPath)
        {
            if (OperatingSystem.IsLinux())
                return Path.ChangeExtension(lnkPath, ".desktop");
            if (OperatingSystem.IsMacOS() && !lnkPath.EndsWith(".app", StringComparison.Ordinal))
                return lnkPath + ".app";
            return lnkPath;
        }

        public static bool Create(string exePath, string exeArgs, string lnkPath, string? iconPath = null, bool integrateWithLinuxMenu = true)
        {
            string resolvedPath = ResolvePath(lnkPath);

            try
            {
                if (OperatingSystem.IsWindows())
                    CreateWindowsShortcut(exePath, exeArgs, lnkPath, iconPath);
                else if (OperatingSystem.IsMacOS())
                    CreateMacOsShortcut(exePath, exeArgs, lnkPath);
                else if (OperatingSystem.IsLinux())
                    CreateLinuxShortcut(exePath, exeArgs, lnkPath, iconPath, integrateWithLinuxMenu);

                if (_loadStatus != GenericTriState.Successful)
                    _loadStatus = GenericTriState.Successful;

                return true;
            }
            catch (Exception ex)
            {
                App.Logger.Error($"Failed to create a shortcut for {resolvedPath}! {ex}");

                if (_loadStatus == GenericTriState.Failed)
                    return false;

                _loadStatus = GenericTriState.Failed;

                _ = Frontend.ShowMessageBox(Strings.Dialog_CannotCreateShortcuts, MessageBoxImage.Warning);
                return false;
            }
        }

        public static void Delete(string lnkPath)
        {
            // Always try to clean up the raw path too in case an old .lnk was left behind.
            if (File.Exists(lnkPath))
                File.Delete(lnkPath);

            string resolvedPath = ResolvePath(lnkPath);
            if (File.Exists(resolvedPath))
                File.Delete(resolvedPath);

            if (OperatingSystem.IsLinux())
            {
                string appMenuPath = Path.Combine(
                    GetLinuxAppMenuDir(),
                    Path.GetFileName(resolvedPath)
                );
                if (File.Exists(appMenuPath))
                    File.Delete(appMenuPath);
            }
        }

        public static async Task CreateGameShortcut(
            string appPath,
            string placeId,
            string? jobId,
            string? accessCode,
            Bitmap? icon,
            Action<string>? onStatus = null)
        {
            string argData = placeId;
            if (!string.IsNullOrEmpty(jobId)) argData += $";{jobId}";
            if (!string.IsNullOrEmpty(accessCode)) argData += $";{accessCode}";

            string safeName = SanitizeFileName(placeId); // caller should pass display name; kept generic here
            string lnkPath = Path.Combine(Paths.Desktop, $"{safeName}.lnk");

            string? finalIconPath = null;

            if (icon != null)
            {
                try
                {
                    onStatus?.Invoke("Saving icon...");

                    string shortcutsIconDir = Path.Combine(Paths.Cache, "Game Shortcuts");
                    Directory.CreateDirectory(shortcutsIconDir);

                    using var ms = new MemoryStream();
                    icon.Save(ms, PngBitmapEncoderOptions.Default);
                    byte[] imageBytes = ms.ToArray();

                    string hash = ComputeHash(imageBytes);
                    string icoPath = Path.Combine(shortcutsIconDir, $"{hash}.ico");

                    if (!File.Exists(icoPath))
                    {
                        onStatus?.Invoke("Converting icon...");
                        using var icoFile = File.Create(icoPath);
                        SaveBitmapAsIcon(icon, icoFile);
                    }

                    finalIconPath = icoPath;
                }
                catch (Exception ex)
                {
                    App.Logger.Error($"Icon processing failed: {ex.Message}");
                }
            }

            onStatus?.Invoke("Creating...");
            Create(appPath, $"-gameshortcut \"{argData}\"", lnkPath, finalIconPath);
        }

        public static Task CreateGameShortcut(
            string appPath,
            string displayName,
            string placeId,
            string? jobId,
            string? accessCode,
            Bitmap? icon,
            Action<string>? onStatus = null)
        {
            string safeName = SanitizeFileName(displayName);
            string lnkPath = Path.Combine(Paths.Desktop, $"{safeName}.lnk");

            // Delegate with the resolved lnk path baked in via a local wrapper.
            return CreateGameShortcutInternal(appPath, placeId, jobId, accessCode, icon, lnkPath, onStatus);
        }

        private static async Task CreateGameShortcutInternal(
            string appPath,
            string placeId,
            string? jobId,
            string? accessCode,
            Bitmap? icon,
            string lnkPath,
            Action<string>? onStatus)
        {
            string deeplink = $"roblox://experiences/start?placeId={placeId}";
            if (!string.IsNullOrEmpty(accessCode))
                deeplink += $"&accessCode={accessCode}";
            else if (!string.IsNullOrEmpty(jobId))
                deeplink += $"&gameInstanceId={jobId}";

            string? finalIconPath = null;

            if (icon != null)
            {
                try
                {
                    onStatus?.Invoke("Saving icon...");

                    string shortcutsIconDir = Path.Combine(Paths.Cache, "Game Shortcuts");
                    Directory.CreateDirectory(shortcutsIconDir);

                    using var ms = new MemoryStream();
                    icon.Save(ms, PngBitmapEncoderOptions.Default);
                    byte[] imageBytes = ms.ToArray();

                    string hash = ComputeHash(imageBytes);
                    string icoPath = Path.Combine(shortcutsIconDir, $"{hash}.ico");

                    if (!File.Exists(icoPath))
                    {
                        onStatus?.Invoke("Converting icon...");
                        using var icoFile = File.Create(icoPath);
                        SaveBitmapAsIcon(icon, icoFile);
                    }

                    finalIconPath = icoPath;
                }
                catch (Exception ex)
                {
                    App.Logger.Error($"Icon processing failed: {ex.Message}");
                }
            }

            onStatus?.Invoke("Creating...");
            Create(appPath, $"-p \"{deeplink}\"", lnkPath, finalIconPath);
        }

        private static void CreateWindowsShortcut(
            string exePath,
            string exeArgs,
            string lnkPath,
            string? iconPath
        )
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell")
                ?? throw new InvalidOperationException("Windows Script Host is unavailable.");

            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic shortcut = shell.CreateShortcut(lnkPath);

            try
            {
                shortcut.TargetPath = exePath;
                shortcut.Arguments = exeArgs;
                shortcut.WorkingDirectory = Path.GetDirectoryName(exePath);

                var finalIconPath = string.IsNullOrWhiteSpace(iconPath)
                    ? exePath
                    : iconPath;

                if (Path.GetExtension(finalIconPath)
                    .Equals(".ico", StringComparison.OrdinalIgnoreCase))
                    shortcut.IconLocation = finalIconPath;
                else shortcut.IconLocation = $"{finalIconPath},0";
                shortcut.Save();
            }
            finally
            {
                Marshal.FinalReleaseComObject(shortcut);
                Marshal.FinalReleaseComObject(shell);
            }
        }

        // idk if these will work at all
        private static void CreateMacOsShortcut(string exePath, string exeArgs, string appBundlePath)
        {
            if (!appBundlePath.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
                appBundlePath += ".app";

            string contentsDir = Path.Combine(appBundlePath, "Contents");
            string macOsDir = Path.Combine(contentsDir, "MacOS");

            Directory.CreateDirectory(macOsDir);

            string scriptPath = Path.Combine(macOsDir, "launcher");
            File.WriteAllText(scriptPath,
                $"""
                #!/bin/bash
                exec "{exePath}" {exeArgs}
                """);

            Process.Start("chmod", $"+x \"{scriptPath}\"")?.WaitForExit();

            File.WriteAllText(Path.Combine(contentsDir, "Info.plist"),
                $"""
                <?xml version="1.0" encoding="UTF-8"?>
                <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN"
                    "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
                <plist version="1.0">
                <dict>
                    <key>CFBundleExecutable</key>  <string>launcher</string>
                    <key>CFBundleIdentifier</key>  <string>com.froststrap.shortcut</string>
                    <key>CFBundleName</key>         <string>{Path.GetFileNameWithoutExtension(appBundlePath)}</string>
                    <key>CFBundleVersion</key>      <string>1.0</string>
                </dict>
                </plist>
                """);
        }

        private static void CreateLinuxShortcut(string exePath, string exeArgs, string desktopPath, string? iconPath, bool integrateWithMenu)
        {
            // An AppImage executes its payload from /tmp/.mount_*. Desktop
            // entries must point at the original persistent AppImage instead.
            if (exePath.Contains("/.mount_", StringComparison.Ordinal))
            {
                string? appImage = Environment.GetEnvironmentVariable("LEAFSTRAP_APPIMAGE") ??
                                   Environment.GetEnvironmentVariable("APPIMAGE");
                if (!string.IsNullOrWhiteSpace(appImage) && File.Exists(appImage))
                    exePath = Path.GetFullPath(appImage);
            }

            string finalDesktopPath = Path.ChangeExtension(desktopPath, ".desktop");
            string appName = Path.GetFileNameWithoutExtension(finalDesktopPath);
            string icon = string.IsNullOrEmpty(iconPath) ? GetFroststrapIconPath() : iconPath;
            string? shortcutDirectory = Path.GetDirectoryName(finalDesktopPath);
            if (string.IsNullOrWhiteSpace(shortcutDirectory))
                throw new IOException("The shortcut location is invalid.");

            Directory.CreateDirectory(shortcutDirectory);

            string escapedExe = EscapeDesktopEntryValue(Path.GetFullPath(exePath));
            string escapedArgs = exeArgs.Trim();
            string workingDirectory = EscapeDesktopEntryValue(Path.GetDirectoryName(Path.GetFullPath(exePath)) ?? Paths.UserProfile);
            string escapedIcon = EscapeDesktopEntryValue(icon);

            string content =
                $"""
                [Desktop Entry]
                Version=1.0
                Type=Application
                Name={appName}
                Comment=Launch Roblox through Leafstrap
                Exec="{escapedExe}" {escapedArgs}
                Path={workingDirectory}
                Icon={escapedIcon}
                Terminal=false
                Categories=Game;Utility;
                StartupNotify=true
                X-KDE-StartupNotify=true
                """;

            File.WriteAllText(finalDesktopPath, content);
            File.SetUnixFileMode(finalDesktopPath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherExecute);

            MarkDesktopShortcutTrusted(finalDesktopPath);

            if (integrateWithMenu)
            {
                // Also put it in a directory that app launchers index.
                string appsDir = GetLinuxAppMenuDir();
                Directory.CreateDirectory(appsDir);
                string appMenuPath = Path.Combine(appsDir, Path.GetFileName(finalDesktopPath));
                File.WriteAllText(appMenuPath, content);
            }

            if (integrateWithMenu || Path.GetFullPath(finalDesktopPath).StartsWith(Path.GetFullPath(GetLinuxAppMenuDir()), StringComparison.Ordinal))
                RefreshLinuxApplicationMenu();
        }

        private static string EscapeDesktopEntryValue(string value) =>
            value.Replace("\\", "\\\\", StringComparison.Ordinal)
                 .Replace("\"", "\\\"", StringComparison.Ordinal);

        private static void MarkDesktopShortcutTrusted(string desktopPath)
        {
            string? gio = FindExecutable("gio");
            if (gio is null || !desktopPath.StartsWith(Path.GetFullPath(Paths.Desktop), StringComparison.Ordinal)) return;

            TryRunDesktopCommand(gio, ["set", desktopPath, "metadata::trusted", "true"]);
        }

        private static void RefreshLinuxApplicationMenu()
        {
            string appsDir = GetLinuxAppMenuDir();
            string? updateDesktopDatabase = FindExecutable("update-desktop-database");
            if (updateDesktopDatabase is not null)
                TryRunDesktopCommand(updateDesktopDatabase, [appsDir]);

            // Plasma normally notices desktop entries automatically. Explicitly
            // refresh its service cache so a newly installed AppImage appears
            // immediately on both Plasma 5 and Plasma 6.
            string? plasmaCache = FindExecutable("kbuildsycoca6") ?? FindExecutable("kbuildsycoca5");
            if (plasmaCache is not null)
                TryRunDesktopCommand(plasmaCache, ["--noincremental"]);
        }

        private static string? FindExecutable(string name)
        {
            foreach (string directory in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(directory)) continue;
                string candidate = Path.Combine(directory, name);
                if (File.Exists(candidate)) return candidate;
            }

            return null;
        }

        private static void TryRunDesktopCommand(string executable, IReadOnlyList<string> arguments)
        {
            try
            {
                var startInfo = new ProcessStartInfo(executable) { UseShellExecute = false };
                foreach (string argument in arguments)
                    startInfo.ArgumentList.Add(argument);
                using Process? process = Process.Start(startInfo);
                process?.WaitForExit(10_000);
            }
            catch (Exception ex)
            {
                // Cache/trust refreshes are best-effort; the desktop entry is
                // still valid and will normally be discovered on the next login.
                App.Logger.Warn($"Linux desktop integration command failed: {ex.Message}");
            }
        }

        private static string GetLinuxAppMenuDir() =>
            Path.Combine(Paths.UserProfile, ".local", "share", "applications");

        private static void SaveBitmapAsIcon(Bitmap bitmap, Stream output)
        {
            using var ms = new MemoryStream();
            bitmap.Save(ms, PngBitmapEncoderOptions.Default);
            ms.Position = 0;

            using var resized = Bitmap.DecodeToWidth(ms, 64);
            using var pngStream = new MemoryStream();
            resized.Save(pngStream, PngBitmapEncoderOptions.Default);
            byte[] pngBytes = pngStream.ToArray();

            using var writer = new BinaryWriter(output);
            writer.Write((short)0);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write((byte)64);
            writer.Write((byte)64);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((short)1);
            writer.Write((short)32);
            writer.Write(pngBytes.Length);
            writer.Write(22);
            writer.Write(pngBytes);
        }

        public static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            var sb = new System.Text.StringBuilder(name.Length);
            foreach (char c in name)
            {
                if (c >= ' ' && c <= '~')
                    sb.Append(c);
            }

            string result = sb.ToString().Trim().TrimEnd('.');
            return string.IsNullOrWhiteSpace(result) ? "Shortcut" : result;
        }

        private static string ComputeHash(byte[] data)
        {
            return FastHash.FromBytes(data);
        }
    }
}
