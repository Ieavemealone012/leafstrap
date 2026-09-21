# Leafstrap macOS build and release guide

## Supported systems

Leafstrap targets macOS 14 or newer. The workflow builds both Apple Silicon (`arm64`) and Intel (`x64`) packages on native GitHub-hosted macOS runners.

## Unsigned test build

No secrets are required for a test build:

1. Push the repository and all Git submodules to GitHub.
2. Run **Build Leafstrap for macOS** from the Actions tab.
3. Download the package matching the test Mac's processor from the workflow artifacts.
4. On the Mac, right-click the downloaded `.pkg`, choose **Open**, and approve the prompt.

If macOS still quarantines an unsigned private test build, the tester can run:

```sh
xattr -dr com.apple.quarantine ~/Downloads/Leafstrap-macos-*.pkg
open ~/Downloads/Leafstrap-macos-*.pkg
```

Only use that command for a package downloaded directly from your own workflow.

## Signed and notarized release

For normal double-click installation, join the Apple Developer Program and add these GitHub Actions repository secrets:

| Secret | Value |
| --- | --- |
| `CERT_P12_BASE64` | Base64 Developer ID Application certificate (`.p12`) |
| `CERT_P12_INSTALLER_BASE64` | Base64 Developer ID Installer certificate (`.p12`) |
| `P12_PASSWORD` | Password used when exporting the certificates |
| `DEVELOPER_ID_APP` | Full `Developer ID Application: …` identity |
| `DEVELOPER_ID_INSTALLER` | Full `Developer ID Installer: …` identity |
| `APP_STORE_CONNECT_P8_CONTENT` | Contents of the App Store Connect API `.p8` key |
| `APPLE_KEY_ID` | App Store Connect API key ID |
| `APPLE_ISSUER_ID` | App Store Connect issuer ID |

On a `v*` tag, the workflow signs `Leafstrap.app`, signs both installer packages, submits them to Apple's notary service, staples the notarization result, and creates a draft GitHub release.

## Test checklist

On both architectures when possible:

- Install and open Leafstrap.
- Confirm the app title, About page, icon, and Discord Rich Presence say Leafstrap.
- Open a `roblox://` link and verify Leafstrap handles it.
- Launch Roblox Player, then Roblox Studio.
- Open `.rbxl`, `.rbxlx`, `.rbxm`, and `.rbxmx` files.
- Verify settings persist after reopening the app.
- Verify notifications appear after permission is granted.
- Test uninstalling by removing `/Applications/Leafstrap.app` and any Leafstrap data the user explicitly wants removed.
