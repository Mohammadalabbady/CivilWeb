# CivilWeb - Civil Registry System

A bilingual (Arabic/English) ASP.NET Core MVC web application for managing civil registry records with Cookie Authentication and role-based authorization.

## Project Information

- **Framework:** .NET 10.0
- **Database:** SQL Server (Entity Framework Core)
- **Authentication:** Cookie Authentication (with Anonymous access enabled)
- **Authorization:** Database-based role checking
- **Languages:** Arabic (RTL) / English (LTR)

---

## Deployment Information

### Paths
| Item | Path |
|------|------|
| Source Code | `C:\Users\Administrator\civil web\CivilWeb` |
| IIS Deployment | `C:\inetpub\wwwroot\CivilWeb` |
| Website URL | `https://mabbady.com` |

### IIS Configuration
- **App Pool Name:** `CivilWebPool`
- **Hosting Model:** In-Process
- **Authentication:** Cookie Authentication (Anonymous enabled, Windows Auth disabled)

### Database
- **Connection String:** Located in `appsettings.json`
- **Database Name:** Check connection string in appsettings.json
- **Tables:**
  - `CivilData` - Main civil records
  - `UserPermissions` - User roles and permissions

---

## User Roles

| Role | Permissions |
|------|-------------|
| **Admin** | Full access: Dashboard, Create, Edit, Delete, Export, User Management |
| **User** | View only: Civil List, Search, Details |

### Current Users in Database
| Username | Role | Notes |
|----------|------|-------|
| PROD\Administrator | Admin | System administrator |
| PROD\Admin | Admin | Admin account |
| PROD\mwtahm | Admin | - |
| PROD\m.amireh | User | Standard user |
| PROD\g.eriksousi | User | Standard user |

---

## Project Structure

```
CivilWeb/
├── Controllers/
│   ├── HomeController.cs      # Dashboard, AccessDenied, SignOut
│   ├── CivilController.cs     # CRUD for civil records
│   ├── UsersController.cs     # User management (Admin only)
│   └── LanguageController.cs  # Language switching
├── Models/
│   ├── CivilData.cs          # Civil record model
│   ├── UserPermission.cs     # User permissions model
│   └── ErrorViewModel.cs
├── Views/
│   ├── Home/
│   │   ├── Index.cshtml       # Admin Dashboard (statistics)
│   │   ├── Welcome.cshtml     # Welcome/Landing page after login
│   │   ├── Dashboard.cshtml   # Main dashboard for all users
│   │   ├── AccessDenied.cshtml # Professional access denied page
│   │   └── SignOutPage.cshtml  # Professional sign out page
│   ├── Civil/
│   │   ├── Index.cshtml       # Records list with search
│   │   ├── Details.cshtml     # Record details
│   │   ├── Create.cshtml      # Create record (Admin)
│   │   ├── Edit.cshtml        # Edit record (Admin)
│   │   └── Delete.cshtml      # Delete confirmation (Admin)
│   ├── Users/
│   │   ├── Index.cshtml       # User list (Admin)
│   │   ├── Create.cshtml      # Add user (Admin)
│   │   ├── Edit.cshtml        # Edit user (Admin)
│   │   └── Delete.cshtml      # Delete user (Admin)
│   └── Shared/
│       └── _Layout.cshtml     # Main layout with navigation
├── Authorization/
│   ├── DatabaseRoleHandler.cs    # Custom auth handler
│   └── DatabaseRoleRequirement.cs
├── Data/
│   └── ApplicationDbContext.cs   # EF Core DbContext
├── Program.cs                    # App configuration
├── appsettings.json             # Configuration settings
└── web.config                   # IIS configuration (in deployment folder)
```

---

## Key Files Configuration

### Program.cs - Authentication Setup
```csharp
// Windows Authentication
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();

// Custom database-based authorization
builder.Services.AddSingleton<IAuthorizationHandler, DatabaseRoleHandler>();

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Administrator", policy =>
        policy.Requirements.Add(new DatabaseRoleRequirement("Admin")));

    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Default route goes to Civil/Index (accessible to all users)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Civil}/{action=Index}/{id?}");
```

### web.config (IIS) - IMPORTANT!
The web.config gets overwritten on publish. Always restore the security section after deployment:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" arguments=".\CivilWeb.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
          <environmentVariable name="ASPNETCORE_HTTP_PORT" value="7016" />
        </environmentVariables>
      </aspNetCore>
      <security>
        <authentication>
          <anonymousAuthentication enabled="true" />
          <windowsAuthentication enabled="false" />
        </authentication>
      </security>
    </system.webServer>
  </location>
</configuration>
```

**Note:** The security section must be manually added after each publish as dotnet publish overwrites web.config without it.

---

## Deployment Commands

### Build and Publish
```powershell
# 1. Stop the app pool first
Import-Module WebAdministration
Stop-WebAppPool -Name 'CivilWebPool'

# 2. Publish the application
cd "C:\Users\Administrator\civil web\CivilWeb"
dotnet publish -c Release -o "C:\inetpub\wwwroot\CivilWeb" --force

# 3. IMPORTANT: Restore web.config with Windows Auth settings
# Copy the web.config content from above to: C:\inetpub\wwwroot\CivilWeb\web.config

# 4. Start the app pool
Start-WebAppPool -Name 'CivilWebPool'
```

### Quick Restart App Pool
```powershell
Import-Module WebAdministration
Restart-WebAppPool -Name 'CivilWebPool'
```

### Check App Pool Status
```powershell
Import-Module WebAdministration
Get-IISAppPool -Name 'CivilWebPool' | Format-Table Name,State
```

---

## Database Management

### UserPermissions Table Structure
```sql
CREATE TABLE [dbo].[UserPermissions] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [Username] NVARCHAR(256) NOT NULL,      -- Format: DOMAIN\username
    [DisplayName] NVARCHAR(256) NULL,
    [Role] NVARCHAR(50) NOT NULL DEFAULT 'User',  -- 'Admin' or 'User'
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
    [LastLoginDate] DATETIME2 NULL,
    [LoginCount] INT NOT NULL DEFAULT 0,
    [Notes] NVARCHAR(MAX) NULL
);
```

### Add New Admin User
```sql
INSERT INTO UserPermissions (Username, DisplayName, Role, IsActive)
VALUES ('PROD\newuser', 'Display Name', 'Admin', 1);
```

### Change User Role
```sql
UPDATE UserPermissions SET Role = 'Admin' WHERE Username = 'PROD\username';
-- or
UPDATE UserPermissions SET Role = 'User' WHERE Username = 'PROD\username';
```

### Get All Users via PowerShell (from AD)
```powershell
Get-ADUser -Filter * -SearchBase "DC=PROD,DC=local" | Select-Object SamAccountName, Name
```

---

## Pages and URLs

| URL | Access | Description |
|-----|--------|-------------|
| `/` or `/Home/Welcome` | All Users | Welcome/Landing page after login |
| `/Home/Dashboard` | All Users | Main dashboard with statistics |
| `/Civil` or `/Civil/Index` | All Users | Civil records list with search |
| `/Civil/Details/{id}` | All Users | View record details |
| `/Civil/Create` | Admin Only | Create new record |
| `/Civil/Edit/{id}` | Admin Only | Edit existing record |
| `/Civil/Delete/{id}` | Admin Only | Delete record |
| `/Civil/Export` | Admin Only | Export to Excel |
| `/Home` | Admin Only | Admin dashboard with advanced statistics |
| `/Users` | Admin Only | User management |
| `/Home/SignOutPage` | All Users | Sign out page |
| `/Home/AccessDenied` | All Users | Access denied page |
| `/Language/SetLanguage?lang=ar` | All Users | Switch to Arabic |
| `/Language/SetLanguage?lang=en` | All Users | Switch to English |

---

## Sign Out Notes

Windows Authentication caches credentials in the browser. The sign out process:
1. Shows confirmation page with user info
2. Attempts to clear credentials via JavaScript
3. Clears cookies and session storage
4. Shows success message with sign-in button

**Note:** To fully switch accounts, users may need to:
- Close ALL browser windows
- Use InPrivate/Incognito mode
- Or use a different browser

---

## Troubleshooting

### 403 Forbidden Error
1. Check if user exists in UserPermissions table
2. Check if user's IsActive = 1
3. Check if user has correct Role for the page
4. Verify Windows Authentication is enabled in IIS

### 401 Unauthorized Error
1. Check web.config has Windows Authentication enabled
2. Check IIS site has Windows Authentication enabled
3. Check Anonymous Authentication is disabled

### App Pool Crashes
1. Check logs at `C:\inetpub\wwwroot\CivilWeb\logs\`
2. Enable stdout logging in web.config: `stdoutLogEnabled="true"`
3. Check Windows Event Viewer

### Database Connection Issues
1. Verify connection string in appsettings.json
2. Check SQL Server is running
3. Verify app pool identity has database access

### Files Locked During Publish
```powershell
# Stop the app pool before publishing
Stop-WebAppPool -Name 'CivilWebPool'
# Publish...
# Start the app pool after publishing
Start-WebAppPool -Name 'CivilWebPool'
```

---

## Authorization Flow

1. User accesses the site
2. IIS performs Windows Authentication (NTLM/Kerberos)
3. `DatabaseRoleHandler` checks `UserPermissions` table for username
4. If found and active, uses Role from database
5. If not found, falls back to AD group checking (Domain Admins = Admin)
6. Admin users auto-added to database on first login

---

## Recent Changes (Latest Session - January 12, 2026)

### 1. Responsive Civil Data Page
- Added responsive CSS with media queries for mobile, tablet, and desktop
- Updated Bootstrap grid classes (col-6, col-md-3, etc.) for search fields
- Added `hide-mobile` and `hide-tablet` classes for table columns
- Table automatically hides less important columns on smaller screens

### 2. Registration Number Search
- Added new search field for Registration Number in Civil Data page
- Search fields layout: ID, Registration Number, First Name, Second Name, Third Name, Last Name

### 3. Database Indexes for Fast Search
Created indexes on CivilData table for optimized search performance:
```sql
CREATE NONCLUSTERED INDEX IX_CivilData_FirstName ON CivilData(FirstName);
CREATE NONCLUSTERED INDEX IX_CivilData_SecondName ON CivilData(SecondName);
CREATE NONCLUSTERED INDEX IX_CivilData_ThirdName ON CivilData(ThirdName);
CREATE NONCLUSTERED INDEX IX_CivilData_LastName ON CivilData(LastName);
CREATE NONCLUSTERED INDEX IX_CivilData_RegistrationNumber ON CivilData(RegistrationNumber);
```
- Search response time: 15-40ms

### 4. Dashboard Page
- Created new `/Home/Dashboard` page accessible to all authenticated users
- Statistics cards showing:
  - Total Records
  - Males count
  - Females count
  - Alive count
  - Deceased count
  - Total Users (Admin only)
  - Active Users (Admin only)
- Quick action buttons for Civil Data, Add Record, Export, User Management

### 5. Welcome Page Update
- Updated welcome text to Arabic: "مرحباً بك في نظام السجل المدني"
- Changed "Enter System" button to navigate to Dashboard instead of Civil Data

### 6. Privacy Page Removal
- Removed Privacy action from HomeController
- Deleted Privacy.cshtml view
- Removed Privacy link from sidebar navigation

### 7. Authentication Update
- Changed from Windows Authentication to Cookie Authentication
- web.config now requires: `anonymousAuthentication enabled="true"` and `windowsAuthentication enabled="false"`

---

## Previous Changes

1. **Professional SignOutPage** - Redesigned with:
   - Dark gradient animated background
   - Floating shapes animation
   - User avatar with initials
   - Multi-step UI (Confirm → Loading → Success)
   - Modern buttons and styling

2. **Professional AccessDenied** - Redesigned with:
   - Matching dark gradient background
   - Shield warning icon
   - User info display
   - List of allowed permissions
   - Contact admin section

---

## Contact

For issues or questions, contact the system administrator.

---

*Last Updated: January 12, 2026*
