using System;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Fallout.Common;
using Fallout.Common.IO;
using Serilog;

public partial class Build : FalloutBuild
{
    const string MacPackageIdentifier = "io.github.ieavemealone012.leafstrap";

    void PublishMacOS(string outputDirectory)
    {
        AbsolutePath virtualbackendBuildRoot = GitRoot / "backend" / "virtualdisplay" / ".build";
        AbsolutePath macAppLocation = FalloutRoot / "Publish" / "macApp";
        AbsolutePath xcodeProjectLocation = macAppLocation / "macApp.xcodeproj";
        AbsolutePath entitlementsPath = macAppLocation / "Leafstrap.entitlements";
        AbsolutePath virtualDisplayDir = GitRoot / "backend" / "virtualdisplay";
        AbsolutePath dylibDest = (AbsolutePath)outputDirectory / "libvirtualdisplay.dylib";

        if (!File.Exists(dylibDest))
        {
            var source = FindVirtualDisplayDylib(virtualDisplayDir);
            Log.Information("Copying {Source} into {OutDir}", source, outputDirectory);
            File.Copy(source, dylibDest, overwrite: true);
        }

        Log.Information("Building {xcproj} with xcodebuild", xcodeProjectLocation);
        var xcbProc = new Process();
        xcbProc.StartInfo.FileName = "xcodebuild";
        xcbProc.StartInfo.Arguments = $"-project {xcodeProjectLocation} " +
                                      "-target Leafstrap " +
                                      $"-configuration {Configuration} " +
                                      "CODE_SIGNING_ALLOWED=NO " +
                                      "build";
        xcbProc.StartInfo.UseShellExecute = false;
        xcbProc.Start();
        xcbProc.WaitForExit();

        if (xcbProc.ExitCode != 0)
        {
            Log.Error("xcodebuild failed with exit code {ExitCode}", xcbProc.ExitCode);
            throw new Exception("xcodebuild failed");
        }

        var src = (AbsolutePath)macAppLocation / "build" / Configuration / "Leafstrap.app";
        var dest = (AbsolutePath)outputDirectory / "Leafstrap.app";
        Log.Information("Copying {src} artifact to {OutDir}", src, dest);

        var copyProc = new Process();
        copyProc.StartInfo.FileName = "cp";
        copyProc.StartInfo.Arguments = $"-r \"{(string)src}\" \"{(string)dest}\"";
        copyProc.StartInfo.UseShellExecute = false;
        copyProc.Start();
        copyProc.WaitForExit();

        bool sign = string.Equals(
            Environment.GetEnvironmentVariable("SIGN"),
            "true",
            StringComparison.OrdinalIgnoreCase
        );

        if (sign)
        {
            SignAndNotarizeMacApp(dest, entitlementsPath, outputDirectory);
        }
        else
        {
            BuildUnsignedPkg(dest, outputDirectory);
        }
    }

    AbsolutePath FindVirtualDisplayDylib(AbsolutePath packageDir)
    {
        var (exitCode, stdout, _) = RunProcessCaptured(
            "swift", $"build -c release --show-bin-path --package-path \"{packageDir}\"");

        if (exitCode == 0)
        {
            var binPath = stdout
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .LastOrDefault();
            var candidate = binPath is null ? null : Path.Combine(binPath, "libvirtualdisplay.dylib");
            if (candidate is not null && File.Exists(candidate))
                return (AbsolutePath)candidate;
        }

        var found = Directory
            .EnumerateFiles(packageDir / ".build", "libvirtualdisplay.dylib", SearchOption.AllDirectories)
            .FirstOrDefault(p => p.Contains("release", StringComparison.OrdinalIgnoreCase));

        return found is not null
            ? (AbsolutePath)found
            : throw new Exception($"libvirtualdisplay.dylib not found under {packageDir / ".build"}");
    }

    void SignAndNotarizeMacApp(AbsolutePath appPath, AbsolutePath entitlementsPath, string outputDirectory)
    {
        string developerIdApp = EnvironmentInfo.GetVariable<string>("DEVELOPER_ID_APP");
        string developerIdInstaller = EnvironmentInfo.GetVariable<string>("DEVELOPER_ID_INSTALLER");
        string appleKeyId = EnvironmentInfo.GetVariable<string>("APPLE_KEY_ID");
        string appleIssuerId = EnvironmentInfo.GetVariable<string>("APPLE_ISSUER_ID");

        AbsolutePath unsignedPkg = (AbsolutePath)outputDirectory / "Leafstrap-unsigned.pkg";
        AbsolutePath finalPkg = (AbsolutePath)outputDirectory / "Leafstrap.pkg";

        Log.Information("Signing .app with {DeveloperIdApp}", developerIdApp);
        RunProcess("codesign", $"--force --deep --options runtime --entitlements \"{entitlementsPath}\" --sign \"{developerIdApp}\" \"{appPath}\"");
        RunProcess("codesign", $"--verify --verbose=4 \"{appPath}\"");

        BuildMacPkg(appPath, unsignedPkg);

        Log.Information("Signing PKG with {DeveloperIdInstaller}", developerIdInstaller);
        RunProcess("productsign", $"--sign \"{developerIdInstaller}\" \"{unsignedPkg}\" \"{finalPkg}\"");

        Log.Information("Submitting for notarization...");
        AbsolutePath keysDir = (AbsolutePath)Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) / ".private_keys";
        Directory.CreateDirectory(keysDir);
        AbsolutePath keyPath = keysDir / $"AuthKey_{appleKeyId}.p8";

        string p8Content = EnvironmentInfo.GetVariable<string>("APP_STORE_CONNECT_P8_CONTENT");
        File.WriteAllText(keyPath, p8Content);

        var (exitCode, stdout, stderr) = RunProcessCaptured(
            "xcrun",
            $"notarytool submit \"{finalPkg}\" --key-id {appleKeyId} --issuer {appleIssuerId} --key \"{keyPath}\" --wait");

        string submissionOutput = stdout + stderr;
        Log.Information(submissionOutput);

        if (exitCode != 0)
        {
            Log.Error("notarytool exited with status {ExitCode} (see output above)", exitCode);
            throw new Exception("notarytool failed");
        }

        var idMatch = Regex.Match(submissionOutput, @"id:\s*(?<id>[a-f0-9-]+)");
        string submissionId = idMatch.Success ? idMatch.Groups["id"].Value : null;

        if (submissionOutput.Contains("status: Invalid"))
        {
            Log.Error("Notarization failed. Fetching detailed log...");
            RunProcess("xcrun", $"notarytool log {submissionId} --key-id {appleKeyId} --issuer {appleIssuerId} --key \"{keyPath}\"");
            throw new Exception("Notarization failed");
        }

        RunProcess("xcrun", $"stapler staple \"{finalPkg}\"");

        File.Delete(keyPath);
        File.Delete(unsignedPkg);

        Log.Information("macOS build complete: {PkgPath}", finalPkg);
    }

    void BuildUnsignedPkg(AbsolutePath appPath, string outputDirectory)
    {
        Log.Information("Building unsigned PKG (skipping signing)");

        AbsolutePath finalPkg = (AbsolutePath)outputDirectory / "Leafstrap.pkg";

        BuildMacPkg(appPath, finalPkg);

        Log.Information("macOS build complete: {PkgPath}", finalPkg);
    }

    void BuildMacPkg(AbsolutePath appPath, AbsolutePath packagePath)
    {
        // An explicit package version is required for reliable upgrades and
        // reinstalls. Without it, Installer may decide that an earlier test
        // package with the same identifier has no newer software to install.
        // Component packaging is Apple's supported path for a single app and
        // makes /Applications the unambiguous destination.
        var projectFile = XDocument.Load(GitRoot / "Froststrap" / "Froststrap.csproj");
        string packageVersion = projectFile.Descendants("Version").First().Value;

        RunProcess(
            "pkgbuild",
            $"--component \"{appPath}\" " +
            "--install-location /Applications " +
            $"--identifier {MacPackageIdentifier} " +
            $"--version {packageVersion} " +
            $"\"{packagePath}\"");
    }
}
