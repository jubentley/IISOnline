# IISOnline

Version: 1.0<br>
Author: Justin Bentley – 2024

IISOnline is a lightweight, real-time console utility for monitoring IIS site states and active worker processes. It's built with performance in mind and is designed for sysadmins, devops engineers, and .NET developers that use IIS on Windows Server.



<img src="https://fs.jbn.ai/fs/host/iiso/capture556a.jpg" alt="IISOnline Demo" />

<a href="https://www.youtube.com/watch?v=IMKjcB8L6J0">
    <img src="https://fs.jbn.ai/fs/jbnai/images/youtube-logo-og-2.png" width="30%" alt="Youtube Link">
</a>


## 🚀 Features

    View live status of all IIS websites (Started/Stopped).

    Check if sites are actively loaded in memory (w3wp.exe).

    See associated Process IDs (PIDs) for each active site.

    Auto-refresh mode for passive background monitoring.

    Displays RAM usage using WMI queries.

    Color-coded terminal output:

        ✅ Green: Site running and loaded

        🕓 Gray: Site idle or stopped

        🔴 Red: High memory usage

    Full error handling with guidance for missing IIS services or permissions.

## 🖥️ Requirements

    Windows Server or Desktop with IIS (Internet Information Services) installed

    .NET 6.0+ (or .NET Framework if converted)

    Run as Administrator (required for IIS + WMI access)

## 🛠️ Usage
Launch from Command Line:

dotnet run

Keyboard Controls:
Key	Action
Any Key	Refresh view
R	Reload IIS sites list
A	Auto-refresh (every 5 seconds)
ESC / Q	Quit the application
## 🔍 Example Output

IISOnline v1.0 2024

+  IIS NAME                    +  READY    +  LOADED   +  PID/S 
+  Default Web Site__________  +  Started  +  Online   +  1234
+  BlogSite___________________  +  Started  +  Offline
+  TestAPI____________________  +  Stopped

+  RAM: 16384 MB: 8321 MB: 50.75%  +

   ** Any key to rescan, 'ESC' to kill, 'R' to refresh IIS, 'A' for Auto **

## 🧠 How It Works

    Uses Microsoft.Web.Administration to query IIS site states.

    Uses WMI (Win32_Process) to match running w3wp.exe processes with application pools.

    Matches pool names via command-line arguments of the processes.

    Monitors system memory via Win32_OperatingSystem.

## ❌ Troubleshooting
Error	Solution
Insufficient Privileges	Run as Administrator
COM Exception	IIS is not installed properly
Invalid Operation	W3SVC service may be stopped
WMI Error	WMI or permissions issue on server
🛡️ Permissions & Security

    No network access required.

    Uses only local system APIs.

    Does not persist or transmit any data.

## 📁 Structure Overview
Component	Purpose
QueryIIS()	Loads sites from IIS
QueryProcesses()	Maps IIS apps to running processes
FlushKeyBuffer()	Ensures key handling is clean
Terminate()	Fails gracefully with messaging
