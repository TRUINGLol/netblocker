# 🛠️ Build and Publish Guide

NetBlocker uses a unified PowerShell script to build and package the application for all supported platforms.

---

## 📋 Prerequisites

To build NetBlocker from source, you need:
1. **.NET 10 SDK** or higher installed on your machine.
2. **PowerShell 7+** (recommended for cross-platform support).
3. (Optional) **Visual Studio 2022** or **VS Code** with C# Dev Kit.

---

## 🏗️ Building for All Platforms

From the project root directory, run the following command:

```powershell
./build-all.ps1
```

### What the script does:
1. **Restores Dependencies:** Downloads all required NuGet packages.
2. **Builds Projects:** Compiles the Core, Infrastructure, and UI projects.
3. **Publishes Binaries:** Generates optimized, self-contained executables for:
   - `win-x64` (Windows 64-bit)
   - `linux-x64` (Linux 64-bit)
   - `osx-x64` (macOS Intel)
   - `osx-arm64` (macOS Apple Silicon)
4. **App Bundling (macOS):** Automatically creates a `.app` bundle structure with the correct `Info.plist` and executable permissions.

---

## 📂 Output Structure

After a successful build, you will find the results in the `publish/` folder:

- `publish/win-x64/NetBlocker.exe`
- `publish/linux-x64/NetBlocker`
- `publish/osx-x64/NetBlocker`
- `publish/NetBlocker-x64.app` (macOS Intel App Bundle)
- `publish/NetBlocker-arm64.app` (macOS Apple Silicon App Bundle)

---

## ⚠️ Important Notes

### Admin Privileges
The build script itself does not require admin privileges, but the **resulting application does**. This is because it needs to interact with the system's low-level firewall APIs (`netsh`, `iptables`, `pfctl`).

### macOS Code Signing
The `build-all.ps1` script creates the `.app` bundle structure but does **not** perform code signing or notarization. If you are distributing this app to others, you may need to sign it using your developer certificate:
```bash
codesign --force --deep --sign "Your Developer ID" publish/NetBlocker-x64.app
```

### Cross-Compilation
While .NET supports cross-compilation for most platforms, it's always best to verify the final build on the target operating system.
