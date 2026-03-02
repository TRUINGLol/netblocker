# 🏗️ Project Architecture

NetBlocker is designed as a modular, cross-platform .NET application following Clean Architecture principles to ensure scalability and ease of maintenance across multiple operating systems.

---

## 📂 Project Structure

### 1. **InternetBlocker.Core**
This is the heart of the application, containing no dependencies on external frameworks or platform-specific APIs.
- **Interfaces:** Defines the contracts for services like `IMonitorService`, `IFirewallService`, and `IBlockedEntityRepository`.
- **Models:** Contains data structures such as `ConnectionInfo` and `BlockedEntity`.

### 2. **InternetBlocker.Infrastructure**
Implementation of the core interfaces using platform-specific technologies.
- **Windows:** Uses `netsh advfirewall` for blocking and `GetExtendedTcpTable` via P/Invoke for monitoring.
- **Linux:** Uses `iptables` for firewalling and `ss` (Socket Statistics) for monitoring.
- **macOS:**
  - **Firewall:** Leverages `pfctl` (Packet Filter) with a custom anchor (`com.netblocker.rules`).
  - **Monitoring:** Uses `lsof` for high-performance connection tracking.
  - **Notifications:** Built with `osascript` for native system alerts.
- **Persistence:** Implementation of `IBlockedEntityRepository` using **SQLite** for lightweight, local data storage.

### 3. **InternetBlocker (UI)**
The presentation layer built with **Avalonia UI**, targeting desktop platforms.
- **MVVM Pattern:** Uses `CommunityToolkit.Mvvm` for clean separation of UI and logic.
- **DynamicData:** Provides powerful, reactive list management for the real-time connection monitor.
- **Service Injection:** Services are decoupled using a `ServiceFactory` which instantiates the correct platform implementation at runtime.

### 4. **InternetBlocker.Desktop**
The entry point project, responsible for initializing the Avalonia application and injecting the necessary infrastructure dependencies.

---

## 🔄 Core Workflows

### Connection Monitoring
The `MonitorViewModel` polls the `IMonitorService` every few seconds. On macOS, this is optimized by only fetching delta changes and caching process icons to maintain a low CPU footprint.

### Reactive Blocking (macOS)
Since macOS's `pfctl` doesn't natively support blocking by "Application Path" in the same way Windows does, NetBlocker uses a **hybrid approach**. When an app is flagged for blocking, the monitor service detects its active IP connections and dynamically adds those destination IPs to the `pfctl` block table.

### Persistent Rules
All blocked applications and IP addresses are saved to `settings.db`. On startup, the application synchronizes its internal database state with the system firewall to ensure protection is active even before the UI is fully loaded.
