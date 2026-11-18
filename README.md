# Leafstrap 🍃

Leafstrap is a Roblox bootstrapper maintained by Leaf, with customization, settings management, and launch tools. It builds on work from [Froststrap](https://github.com/RealMeddsam/Froststrap), [Fishstrap](https://github.com/fishstrap/fishstrap), and [Bloxstrap](https://github.com/bloxstraplabs/bloxstrap).

## Download

Download Leafstrap from [this repository's Releases page](https://github.com/Ieavemealone012/leafstrap/releases). Choose the package for your operating system and read that release's installation notes.

The v1.0.7 release provides these packages:

| Platform | Release assets |
| --- | --- |
| Windows x64 | `Leafstrap-windows-x64.msi`, `Leafstrap.exe` |
| macOS Apple Silicon | `Leafstrap-macos-arm64.pkg` |
| macOS Intel | `Leafstrap-macos-x64.pkg` |
| Linux x64 | `Leafstrap-linux-x64.AppImage`, `Leafstrap-linux-x64.deb`, `Leafstrap-linux-x64.rpm` |

Package availability and features can differ between platforms. This repository's current `main` branch contains the Windows WPF application and its build tooling; it does not contain the macOS or Linux application source. The build instructions below apply to that Windows source.

## Features

The Windows application includes:

- Roblox Player and Studio launch integration
- Custom bootstrapper appearance, themes, backgrounds, and fonts
- Cursor, sound, and other mod options
- FastFlag settings, editing, profiles, and undo/redo
- Discord Rich Presence and optional activity integrations
- Settings import/export and log export
- Stable and prerelease update preferences

See [release notes](https://github.com/Ieavemealone012/leafstrap/releases) for the changes and limitations of each release. Features inherited from upstream projects may evolve independently in Leafstrap.

## Updates

Leafstrap checks its own [GitHub releases](https://github.com/Ieavemealone012/leafstrap/releases) for application updates. Update preferences are configurable in the application.

For the v1.0.7 update flow, launch Roblox or Studio through Leafstrap to trigger its bootstrapper update check. Leaving the settings window open is not a silent background update check. See the notes for your installed release when testing prerelease updates.

## Build the Windows application

### Requirements

- Windows with support for the .NET 10 SDK and Windows desktop applications
- The **.NET 10 SDK**
- Git, including this repository's submodules

Clone the repository with its UI dependencies:

```powershell
git clone --recurse-submodules https://github.com/Ieavemealone012/leafstrap.git
cd leafstrap
```

For an existing checkout, initialize or update the submodules first:

```powershell
git submodule update --init --recursive
```

Build and publish using the included script:

```powershell
.\build-leafstrap.ps1
```

The script restores dependencies, builds the Windows x64 application, and publishes to:

```text
Bloxstrap\bin\Release\net10.0-windows\publish\win-x64\
```

The publish directory includes `Leafstrap.exe` and its release companion files. The current publish profile is framework-dependent (`SelfContained=false`), so the target Windows computer needs the matching .NET 10 Desktop Runtime.

### Development build

To restore and build the solution without publishing:

```powershell
dotnet restore .\Bloxstrap.sln
dotnet build .\Bloxstrap.sln -c Debug --no-restore
```

The internal solution, project directory, and namespace retain the `Bloxstrap` name. The application assembly is named `Leafstrap`. Those internal names do not mean you downloaded the wrong project.

## Report a problem or suggest a feature

Use [Leafstrap Issues](https://github.com/Ieavemealone012/leafstrap/issues). For a bug report, include:

- Your operating system, Leafstrap version, and installation package
- What you did, what you expected, and what happened instead
- Relevant error messages or logs, with account information removed

## Credits and license files

Leafstrap builds on contributions from Froststrap, Fishstrap, Bloxstrap, and their contributors. Their acknowledgements and third-party notices remain relevant.

See the repository's [LICENSE](LICENSE), [LICENSE-MIT](LICENSE-MIT), [LICENSE-UNLICENSE](LICENSE-UNLICENSE), and bundled third-party notices for the license text included with the source. This README does not replace those documents.
