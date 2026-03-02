# 🛡️ NetBlocker

**NetBlocker** is a powerful, cross-platform application designed to give you total control over your network privacy. Inspired by "Radio Silence," it provides a sleek and intuitive interface to monitor active connections and block any application from accessing the internet with a single click.

Built with **Avalonia UI** and **.NET**, NetBlocker offers a native-like experience on Windows, macOS, and Linux, ensuring your data stays exactly where you want it.

---

## ✨ Key Features

- **🚀 Real-time Monitoring:** Instantly see every outgoing connection from your system, including process names, remote addresses, and ports.
- **🚫 One-Click Blocking:** Stop any application from accessing the network immediately. No complex firewall rules to manage.
- **🍏 macOS optimization:** Native support for macOS including `pfctl` integration, system notifications, and high-performance monitoring.
- **📊 Analytics Dashboard:** A dedicated "Broadway" view to track your network history, total connections, and most active applications.
- **🔄 Auto-Persistence:** Your blocking rules are stored in a secure local database and automatically re-applied on system startup.
- **🌙 Modern UI:** A beautiful, dark-themed interface with smooth animations and high-resolution icons.

---

## 🚀 Quick Start (User Guide)

NetBlocker is distributed as a standalone portable application. No complex installation is required.

### 1. Download
Head over to the [GitHub Releases](https://github.com/yourusername/netblocker/releases) page and download the archive for your operating system:
- `NetBlocker-win-x64.zip` (Windows)
- `NetBlocker-linux-x64.tar.gz` (Linux)
- `NetBlocker-osx-x64.app.zip` / `NetBlocker-osx-arm64.app.zip` (macOS)

### 2. Run
Extract the archive and run the executable. **Note: NetBlocker requires administrative privileges to manage firewall rules.**

#### 🪟 Windows
- Right-click `NetBlocker.exe` and select **"Run as Administrator"**.

#### 🍎 macOS
- Drag `NetBlocker.app` to your Applications folder.
- Because it modifies system firewall rules, you may need to run it with elevated permissions or authorize it via System Settings. 
- *Tip:* If launching from terminal, use `sudo ./NetBlocker.app/Contents/MacOS/NetBlocker`.

#### 🐧 Linux
- Ensure `iptables` is installed.
- Run with `sudo ./NetBlocker`.

---

## 🛠 For Developers

Want to build NetBlocker from source or understand how it works? Check out our documentation:

- 🏗️ **[Architecture Docs](docs/architecture.md):** Deep dive into the project structure and cross-platform implementation.
- ╓ **[Build Guide](docs/build.md):** How to use our automated build scripts for all platforms.

---

## 📜 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
