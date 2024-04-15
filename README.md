# Docker Volume Backup Worker
A Windows Service Worker that backs up local Docker Volumes to a specified directory.

## Overview
Docker Volume Backup Worker is a tool designed to automatically back up Docker volumes. It supports various configurations to manage backup intervals, retention policies, and ensures that backups are stored safely.

## Features
- **Automated Backups**: Schedule and automate backups of Docker volumes.
- **Retention Policies**: Automatically apply retention policies to manage disk space.
- **Error Handling**: Robust error handling and logging for troubleshooting and monitoring.

## Prerequisites
Before you begin, ensure you have the following installed:
- [Docker](https://docs.docker.com/get-docker/)
- [.NET 8.0 or higher](https://dotnet.microsoft.com/download)
- PowerShell

## Installation

### Clone the Repository
To get started, clone the repository to your local machine:

```bash
git clone https://github.com/yourusername/docker-volume-backup-worker.git
cd docker-volume-backup-worker
```

### Configuration
To configure the Docker Volume Backup Worker, modify the environment variables in the `.launchsettings.` file or set them directly in your environment:

- **DVB_BACKUP_PATHS**: Local directory for your backup files.
- **DVB_DISCORD_DVBBOT_WEBHOOK_URL**: URL to your Discord webhook for notifications.
- **DVB_RETENTION_POLICY_ENABLED**: Boolean to enable retention policy.
- **DVB_RETENTION_POLICY_LENGTH**: TimeSpan format (`dd:hh:mm:ss`) specifying how long backups are retained.
- **DVB_RETENTION_POLICY_MINIMUM_VOLUME_COUNT**: Minimum number of backup files to retain.
- **DVB_WORKER_INTERVAL_IN_MS**: Interval in milliseconds to determine how frequently backups are performed.
- Example
    - .launchsettings
    ```json
    {
        "$schema": "http://json.schemastore.org/launchsettings.json",
        "profiles": {
        "DockerVolumeBackup.Worker": {
            "commandName": "Project",
            "dotnetRunMessages": true,
            "environmentVariables": {
            "DVB_BACKUP_PATHS": "C:\\Backup\\Data",
            "DVB_DISCORD_DVBBOT_WEBHOOK_URL": "https://discord.com/api/webhooks/1234567890987654321/abcdefgijklmnopqrstuvwxyz_1234567890",
            "DVB_RETENTION_POLICY_ENABLED": "true",
            "DVB_RETENTION_POLICY_LENGTH": "7.00:00:00", // 7 days
            "DVB_RETENTION_POLICY_MINIMUM_VOLUME_COUNT": "24",
            "DVB_WORKER_INTERVAL_IN_MS": "3600000" // 1 hour
          }
        }
      }
    }
    ```
    - Powsershell
	```powershell
    $env:DVB_BACKUP_PATHS = "C:\Backup\Data"
    $env:DVB_DISCORD_DVBBOT_WEBHOOK_URL = "https://discord.com/api/webhooks/1234567890987654321/abcdefgijklmnopqrstuvwxyz_1234567890"
    $env:DVB_RETENTION_POLICY_ENABLED = "true"
    $env:DVB_RETENTION_POLICY_LENGTH = "7.00:00:00"  # 7 days
    $env:DVB_RETENTION_POLICY_MINIMUM_VOLUME_COUNT = "24"
    $env:DVB_WORKER_INTERVAL_IN_MS = "3600000"  # 1 hour
    ```

### Running as a Windows Service

To run the Docker Volume Backup Worker as a Windows service, use the following PowerShell commands as an Administrator:

### Create the Service
```powershell
sc.exe create "DockerVolumeBackup" binPath= "<PATH-TO-YOUR-EXE>"
```

### Start the Service
```powershell
Start-Service "DockerVolumeBackup"
```

### Check the Service Status
```powershell
Get-Service -Name "DockerVolumeBackup"
```

### Set the Service to Start Automatically
```powershell
Set-Service -Name "DockerVolumeBackup" -StartupType Automatic
```

### Stop the Service
```powershell
Stop-Service -Name "DockerVolumeBackup"
```

### Delete the Service
```powershell
sc.exe delete "DockerVolumeBackup"
```

## Support
If you encounter any issues or have questions, open an issue in the GitHub repository or contact support via email.