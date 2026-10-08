using System;
using System.IO;
using System.Linq;
using Fallout.Common;
using Fallout.Common.IO;
using Serilog;

public partial class Build : FalloutBuild
{
    string GetVersion()
    {
        var (_, stdout, _) = RunProcessCaptured("git", "describe --tags --always --dirty");
        var raw = stdout.Trim();
        if (string.IsNullOrEmpty(raw))
            raw = "1.0.0";
        return raw.TrimStart('v').Replace('-', '~');
    }

    void PublishLinux(string outputDirectory)
    {
        var version = GetVersion();
        var rpmVersion = version.Replace('+', '_');
        Log.Debug("Detected build version as {ver}", version);
        Log.Debug("Detected RPM version as {ver}", rpmVersion);

        AbsolutePath outputDir      = outputDirectory;
        AbsolutePath appDir         = outputDir / "AppDir";
        AbsolutePath publishDir     = outputDir;

        if (Directory.Exists(appDir))
            Directory.Delete(appDir, recursive: true);

        Directory.CreateDirectory(appDir / "usr" / "bin");
        Directory.CreateDirectory(appDir / "usr" / "share" / "applications");
        Directory.CreateDirectory(appDir / "usr" / "share" / "icons" / "hicolor" / "512x512" / "apps");

        AbsolutePath icon = GitRoot / "Froststrap" / "Leafstrap.png";

        File.Copy(publishDir / "Leafstrap", appDir / "usr" / "bin" / "Leafstrap", overwrite: true);
        File.Copy(icon, appDir / "leafstrap.png", overwrite: true);
        File.Copy(icon, appDir / "usr" / "share" / "icons" / "hicolor" / "512x512" / "apps" / "leafstrap.png", overwrite: true);

        RunProcess("chmod", $"+x \"{appDir / "usr" / "bin" / "Leafstrap"}\"");

        var desktopEntry = $"""
            [Desktop Entry]
            Type=Application
            Name=Leafstrap
            Comment=A cross-platform Roblox bootstrapper focused on customization
            Exec=Leafstrap %u
            TryExec=Leafstrap
            Icon=leafstrap
            Terminal=false
            Categories=Game;
            MimeType=x-scheme-handler/roblox;x-scheme-handler/roblox-player;
            X-AppImage-Version={version}
            """;

        File.WriteAllText(appDir / "Leafstrap.desktop", desktopEntry);
        File.Copy(appDir / "Leafstrap.desktop",
                  appDir / "usr" / "share" / "applications" / "Leafstrap.desktop",
                  overwrite: true);

        var appRun = """
            #!/bin/sh
            HERE="$(dirname "$(readlink -f "$0")")"
            exec "$HERE/usr/bin/Leafstrap" "$@"
            """;

        File.WriteAllText(appDir / "AppRun", appRun);
        RunProcess("chmod", $"+x \"{appDir / "AppRun"}\"");

        BuildAppImage(outputDir, appDir);
        BuildRpm(outputDir, appDir, rpmVersion);
        BuildDeb(outputDir, appDir,  version);

        Directory.Delete(appDir, recursive: true);
        File.Delete(outputDir / "appimagetool.AppImage");
        Directory.Delete(outputDir / "rpmbuild", recursive: true);

        Log.Information("Linux builds complete");
    }

    void BuildAppImage(AbsolutePath buildDir, AbsolutePath appDir)
    {
        string tool = "appimagetool";

        if (!IsOnPath("appimagetool"))
        {
            AbsolutePath toolPath = buildDir / "appimagetool.AppImage";
            Log.Information("appimagetool not found on PATH, downloading to {path}", toolPath);
            RunProcess("curl",
                $"-L --fail -o \"{toolPath}\" " +
                "https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage");
            RunProcess("chmod", $"+x \"{toolPath}\"");
            tool = toolPath;
        }

        Environment.SetEnvironmentVariable("ARCH", "x86_64");
        Environment.SetEnvironmentVariable("SOURCE_DATE_EPOCH", null);

        Log.Information("Building AppImage");
        RunProcess(tool,
            $"--appimage-extract-and-run \"{appDir}\" \"{buildDir / "Leafstrap-linux-x64.AppImage"}\"");
    }

    void BuildRpm(AbsolutePath outputDir, AbsolutePath appDir, string rpmVersion)
    {
        AbsolutePath topDir = outputDir / "rpmbuild";

        foreach (var sub in new[] { "BUILD", "BUILDROOT", "RPMS", "SOURCES", "SPECS", "SRPMS" })
            Directory.CreateDirectory(topDir / sub);

        AbsolutePath spec = FalloutRoot / "Publish" / "fedora" / "froststrap-rpm.spec";

        Log.Information("Building RPM from {spec}", spec);
        RunProcess("rpmbuild",
            $"-bb \"{spec}\" " +
            $"--define \"_topdir {topDir}\" " +
            $"--define \"_leafstrap_appdir {appDir}\" " +
            $"--define \"leafstrap_version {rpmVersion}\"");

        var rpm = Directory
            .EnumerateFiles(topDir / "RPMS", "*.rpm", SearchOption.AllDirectories)
            .OrderBy(File.GetLastWriteTimeUtc)
            .LastOrDefault();

        if (rpm is null)
            throw new InvalidOperationException($"rpmbuild produced no .rpm under {topDir / "RPMS"}");

        File.Copy(rpm, outputDir / "Leafstrap-linux-x64.rpm", overwrite: true);
    }

    void BuildDeb(AbsolutePath outputDir, AbsolutePath appDir, string version)
    {
        AbsolutePath debianDir = appDir / "DEBIAN";
        Directory.CreateDirectory(debianDir);

        var control = $"""
            Package: leafstrap
            Version: {version}
            Architecture: amd64
            Maintainer: Ieavemealone012
            Depends: libicu-dev
            Description: Roblox bootstrapper and mod manager

            """;

        File.WriteAllText(debianDir / "control", control);

        File.Copy(FalloutRoot / "Publish" / "debian" / "postinst", debianDir / "postinst", overwrite: true);
        RunProcess("chmod", $"755 \"{debianDir / "postinst"}\"");

        Log.Information("Building .deb");
        RunProcess("dpkg-deb", $"--build \"{appDir}\" \"{outputDir / "Leafstrap-linux-x64.deb"}\"");
    }

    static bool IsOnPath(string exe) =>
        (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator)
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Any(d => File.Exists(Path.Combine(d, exe)));
}
