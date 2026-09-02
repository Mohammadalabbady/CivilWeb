# IIS Deployment & Implementation Guide - CivilWeb

Comprehensive setup and deployment guide for hosting the **CivilWeb** registry application on Windows Server using **IIS**.

---

## 1. System & Server Prerequisites

Before deploying, ensure your server meets the following requirements:

| Requirement | Details |
| :--- | :--- |
| **OS** | Windows Server 2016 / 2019 / 2022 (or Windows 10/11) |
| **Web Server** | IIS 10.0+ |
| **.NET Runtime** | **ASP.NET Core 10.0 Hosting Bundle** (Includes `AspNetCoreModuleV2`) |
| **Database** | SQL Server (reachable via network) |
| **Permissions** | Administrator privileges on the target server |

---

## 2. Server Installation Steps

### Step 1: Install IIS (Internet Information Services)
Run PowerShell as **Administrator** on the server:

```powershell
Install-WindowsFeature -Name Web-Server, Web-Mgmt-Tools -IncludeManagementTools
```

### Step 2: Download & Install ASP.NET Core Hosting Bundle
1. Download the **.NET 10.0 Hosting Bundle** from [Microsoft .NET Download Center](https://dotnet.microsoft.com/download/dotnet/10.0).
2. Run the installer on the server.
3. Restart IIS so the IIS module detects ASP.NET Core:
   ```powershell
   iisreset
   ```

---

## 3. Database Connection Configuration

Open `appsettings.json` (or `appsettings.Production.json`) and verify your connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=YOUR_SQL_SERVER;Initial Catalog=Civil2006;User Id=YOUR_USER;Password=YOUR_PASS;TrustServerCertificate=true"
  }
}
```

---

## 4. IIS Website & App Pool Setup

### Step 1: Create Application Pool
1. Open **IIS Manager** (`inetmgr`).
2. Right-click **Application Pools** -> **Add Application Pool...**
3. Configure settings:
   - **Name:** `CivilWebPool`
   - **.NET CLR Version:** **No Managed Code** *(Essential for ASP.NET Core)*
   - **Managed Pipeline Mode:** Integrated
4. Click **OK**.
5. *(Optional for high performance)* Right-click `CivilWebPool` -> **Advanced Settings...**:
   - Set **Start Mode** to `AlwaysRunning`.

### Step 2: Create Website & Physical Directory
1. Create the deployment directory: `C:\inetpub\wwwroot\CivilWeb`.
2. Create logs folder: `C:\inetpub\wwwroot\CivilWeb\logs`.
3. In IIS Manager, right-click **Sites** -> **Add Website...**
4. Configure settings:
   - **Site name:** `CivilWeb`
   - **Application pool:** `CivilWebPool`
   - **Physical path:** `C:\inetpub\wwwroot\CivilWeb`
   - **Binding Type:** `http` / `https` and desired Port (e.g. `80`, `7016`, or `7014`).

### Step 3: Configure Folder Permissions
Grant the IIS AppPool identity read/write permissions:

```powershell
icacls "C:\inetpub\wwwroot\CivilWeb" /grant "IIS_IUSRS:(OI)(CI)(RX)"
icacls "C:\inetpub\wwwroot\CivilWeb\logs" /grant "IIS_IUSRS:(OI)(CI)(M)"
```

---

## 5. Publishing & Deploying the Application

### Option A: Manual Publish & Deploy

1. Publish the project from PowerShell:
   ```powershell
   cd "C:\Users\Laptop\Desktop\civil web\CivilWeb"
   dotnet publish -c Release -o publish
   ```

2. Stop the App Pool to release locked files:
   ```powershell
   Stop-WebAppPool -Name 'CivilWebPool'
   ```

3. Copy the published output into `C:\inetpub\wwwroot\CivilWeb\`.

4. Start the App Pool:
   ```powershell
   Start-WebAppPool -Name 'CivilWebPool'
   ```

### Option B: Automated PowerShell Deployment Script

You can save and execute this automated script whenever you publish updates:

```powershell
# Automated IIS Deploy Script for CivilWeb
$sourcePath = "C:\Users\Laptop\Desktop\civil web\CivilWeb"
$publishPath = "$sourcePath\publish"
$destPath = "C:\inetpub\wwwroot\CivilWeb"
$poolName = "CivilWebPool"

Write-Host "1. Building and Publishing..." -ForegroundColor Cyan
cd $sourcePath
dotnet publish -c Release -o $publishPath

Write-Host "2. Stopping IIS Application Pool..." -ForegroundColor Yellow
Import-Module WebAdministration
Stop-WebAppPool -Name $poolName
Start-Sleep -Seconds 3

Write-Host "3. Copying Files to IIS Directory..." -ForegroundColor Cyan
Copy-Item -Path "$publishPath\*" -Destination $destPath -Recurse -Force

Write-Host "4. Starting Application Pool..." -ForegroundColor Green
Start-WebAppPool -Name $poolName

Write-Host "Deployment Complete!" -ForegroundColor Green
```

---

## 6. `web.config` Configuration Reference

Verify `C:\inetpub\wwwroot\CivilWeb\web.config` is configured properly for IIS Hosting:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" arguments=".\CivilWeb.dll" stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
```

---

## 7. Windows Firewall Configuration

Allow inbound HTTP/HTTPS traffic through Windows Firewall:

```powershell
New-NetFirewallRule -DisplayName "CivilWeb HTTP" -Direction Inbound -Protocol TCP -LocalPort 7016 -Action Allow
New-NetFirewallRule -DisplayName "CivilWeb HTTPS" -Direction Inbound -Protocol TCP -LocalPort 7014 -Action Allow
```

---

## 8. Verification & Troubleshooting

- **Check logs**: Logs will be written to `C:\inetpub\wwwroot\CivilWeb\logs\stdout_*.log`.
- **HTTP 502.5 / 500.19**: Ensure App Pool is set to **No Managed Code** and **ASP.NET Core 10.0 Hosting Bundle** is installed.
- **File Access Lock**: Always stop `CivilWebPool` before copying new DLLs during deployment.
