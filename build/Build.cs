using System.Diagnostics;
using System.Runtime.InteropServices;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Solutions;
using Microsoft.Build.Locator;
using Fallout.Common.Git;
using Serilog;

public partial class Build : FalloutBuild
{
    public static int Main() {
        MSBuildLocator.RegisterDefaults();
        return Execute<Build>(x => x.Compile);
    }

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;
    
    [GitRepository]
    readonly GitRepository Repository;

    [Solution]
    readonly Solution Solution;

    AbsolutePath GitRoot => Repository.LocalDirectory;
    AbsolutePath FalloutRoot => GitRoot / "build";
    AbsolutePath OutputRoot => GitRoot / ".build";

    Target BuildDebug => _ => _
        .Executes(() => {
            Log.Information("Git commit: {Value}", Repository.Commit);
            Log.Information("Git branch: {Value}", Repository.Branch);
            Log.Information("Git local dir: {Value}", GitRoot);
        });

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            if (System.IO.Directory.Exists(OutputRoot)) System.IO.Directory.Delete(OutputRoot, recursive: true);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            var project = Solution.GetProject("Froststrap");
            string arch = RuntimeInformation.OSArchitecture switch
            {
                Architecture.X64 => "x64",
                Architecture.Arm64 => "arm64",
                _ => throw new PlatformNotSupportedException("Unsupported architecture for restoring.")
            };
            string rid = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"win-{arch}" :
                         RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? $"linux-{arch}" :
                         RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? $"osx-{arch}" :
                         throw new PlatformNotSupportedException("Unsupported OS for restoring.");

            var process = new Process();
            process.StartInfo.FileName = "dotnet";
            process.StartInfo.Arguments = $"restore \"{project.Path}\" -r {rid} --nologo";
            process.StartInfo.UseShellExecute = false;            
            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new Exception($"Restore failed for {rid} with exit code {process.ExitCode}");
        });

    Target Publish => _ => _
        .DependsOn(Restore)
        .DependsOn(BuildDebug)
        .Executes(() => PublishMain());

    Target Compile => _ => _
        .DependsOn(Restore)
        .DependsOn(BuildDebug)
        .Executes(() => CompileMain());
}
