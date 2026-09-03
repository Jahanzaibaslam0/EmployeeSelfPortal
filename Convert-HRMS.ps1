# HRMS Razor -> ASP.NET Web Forms conversion script
$ErrorActionPreference = "Stop"
$root = "D:\Project\HRMS"
$data = "D:\Project\DATA"
$pagesDir = Join-Path $root "Pages"

# ── DATA folder ──────────────────────────────────────────────────────────────
New-Item -ItemType Directory -Force -Path $data | Out-Null
Copy-Item (Join-Path $root "Database\Script.sql") $data -Force -ErrorAction SilentlyContinue
Copy-Item (Join-Path $root "Database\UserSecurity_Script.sql") $data -Force -ErrorAction SilentlyContinue
@"
HRMS Database Folder
====================
1. Run Script.sql on SQL Server (LocalDB or full instance)
2. Run UserSecurity_Script.sql for users and permissions
3. Web.config AttachDbFilename points to: D:\Project\DATA\HRMSDB.mdf

Alternative: Change Web.config connection string to your SQL Server instance:
  Data Source=YOUR_SERVER;Initial Catalog=HRMSDB;Integrated Security=True;
"@ | Set-Content (Join-Path $data "README.txt")

# ── Static assets (wwwroot -> root) ───────────────────────────────────────────
foreach ($folder in @("css","js","images")) {
    $src = Join-Path $root "wwwroot\$folder"
    $dst = Join-Path $root $folder
    if (Test-Path $src) {
        New-Item -ItemType Directory -Force -Path $dst | Out-Null
        Copy-Item "$src\*" $dst -Recurse -Force
    }
}

# ── StartupMigrations from Program.cs ─────────────────────────────────────────
$program = Join-Path $root "Program.cs"
$migrations = Join-Path $root "StartupMigrations.cs"
if (Test-Path $program) {
    $content = Get-Content $program -Raw
    $content = $content -replace 'using Microsoft\.Data\.SqlClient', 'using System.Data.SqlClient'
    $content = $content -replace 'var builder = WebApplication\.CreateBuilder\(args\);[\s\S]*?app\.Run\(\);', ''
    $content = $content -replace 'static void RunStartupMigrations\(IConfiguration config\)', 'public static void Run()'
    $content = $content -replace 'static void SeedAppData\(IConfiguration config\)', 'public static void Seed()'
    $content = $content -replace 'config\.GetConnectionString\("HRMSConnection"\)', 'ConfigurationManager.ConnectionStrings["HRMSConnection"]?.ConnectionString'
    $content = $content -replace '(?m)^RunStartupMigrations\(app\.Configuration\);\s*\r?\n', ''
    $content = $content -replace '(?m)^SeedAppData\(app\.Configuration\);\s*\r?\n', ''
    $header = @"
using System;
using System.Configuration;
using System.Data.SqlClient;
using HRMS.Services;

namespace HRMS
{
    public static class StartupMigrations
    {
"@
    $footer = @"
    }
}
"@
    # Extract migration methods body
    if ($content -match '(?s)(public static void Run\(\).*?)(public static void Seed\(\).*?)(\r?\n\})') {
        $runBody = $Matches[1]
        $seedBody = $Matches[2]
        ($header + "`n" + $runBody + "`n" + $seedBody + $footer) | Set-Content $migrations -Encoding UTF8
    }
}

# ── Adapt service files ───────────────────────────────────────────────────────
$serviceFiles = Get-ChildItem (Join-Path $root "Services\*.cs") -File
foreach ($file in $serviceFiles) {
    if ($file.Name -eq "AuthService.cs") { continue }
    $c = Get-Content $file.FullName -Raw
    $c = $c -replace 'using Microsoft\.Data\.SqlClient', 'using System.Data.SqlClient'
    $c = $c -replace 'using Microsoft\.AspNetCore\.Http;', 'using System.Web;'
    $c = $c -replace 'using Microsoft\.AspNetCore\.Mvc\.Filters;', ''
    $c = $c -replace 'using Microsoft\.Extensions\.Configuration;', 'using System.Configuration;'
    $c = $c -replace 'IConfiguration config,?\s*', ''
    $c = $c -replace 'AuthService auth,?\s*', ''
    $c = $c -replace 'IHttpContextAccessor http\)?', ')'
    $c = $c -replace 'IHttpContextAccessor _http;', ''
    $c = $c -replace '_http\.HttpContext', 'HttpContext.Current'
    $c = $c -replace 'config\.GetConnectionString\("HRMSConnection"\)!', 'ConfigurationManager.ConnectionStrings["HRMSConnection"].ConnectionString'
    $c = $c -replace 'private readonly string _conn;\s*private readonly AuthService _auth;', 'private readonly string _conn;`n    private readonly AuthService _auth = new AuthService();'
    $c = $c -replace 'public PermissionService\(\)\s*\{\s*_conn = ConfigurationManager', 'public PermissionService()`n    {`n        _conn = ConfigurationManager'
    if ($c -notmatch 'ConfigurationManager') {
        $c = $c -replace '(namespace HRMS\.Services;)', "using System.Configuration;`n`n`$1"
    }
    if ($c -match 'public \w+\([^)]*IConfiguration') {
        $c = $c -replace 'public (\w+)\([^)]+\)\s*\{[^}]*_conn = ConfigurationManager[^;]+;', 'public $1()`n    {`n        _conn = ConfigurationManager.ConnectionStrings["HRMSConnection"].ConnectionString;'
    }
    Set-Content $file.FullName $c -Encoding UTF8 -NoNewline
}

# Fix AuditHelper
$auditHelper = Join-Path $root "Services\AuditHelper.cs"
@'
using System.Data.SqlClient;

namespace HRMS.Services
{
    public static class AuditHelper
    {
        public static void AddCreatedBy(SqlCommand cmd, int? userId)
        {
            cmd.Parameters.AddWithValue("@CreatedByUserID", userId.HasValue && userId.Value > 0 ? (object)userId.Value : DBNull.Value);
        }

        public static void AddModifiedBy(SqlCommand cmd, int? userId)
        {
            cmd.Parameters.AddWithValue("@ModifiedByUserID", userId.HasValue && userId.Value > 0 ? (object)userId.Value : DBNull.Value);
        }
    }
}
'@ | Set-Content $auditHelper -Encoding UTF8

# Fix PasswordHelper for net48
$pw = Join-Path $root "Services\PasswordHelper.cs"
@'
using System;
using System.Security.Cryptography;
using System.Text;

namespace HRMS.Services
{
    public static class PasswordHelper
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100000;

        public static string HashPassword(string password)
        {
            var salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(salt);

            var hash = DeriveKey(password, salt);
            return Convert.ToBase64String(salt) + "." + Convert.ToBase64String(hash);
        }

        public static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(storedHash)) return false;
            var parts = storedHash.Split('.');
            if (parts.Length != 2) return false;

            byte[] salt, expected;
            try
            {
                salt = Convert.FromBase64String(parts[0]);
                expected = Convert.FromBase64String(parts[1]);
            }
            catch { return false; }

            var actual = DeriveKey(password, salt);
            return FixedTimeEquals(actual, expected);
        }

        private static byte[] DeriveKey(string password, byte[] salt)
        {
            using (var derive = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
                return derive.GetBytes(HashSize);
        }

        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            var diff = 0;
            for (var i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
'@ | Set-Content $pw -Encoding UTF8

# ── Lookup setup .aspx template ───────────────────────────────────────────────
function New-LookupPage {
    param($Name, $CodeBehind)
    $aspx = @"
<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="$Name.aspx.cs" Inherits="HRMS.$($Name)Page" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>HRMS - <%= PageTitle %></title>
    <link rel="stylesheet" href="/css/style.css?v=9" />
</head>
<body>
<form id="form1" runat="server">
<%@ Register Src="~/Controls/AppHeader.ascx" TagPrefix="hrms" TagName="AppHeader" %>
<hrms:AppHeader runat="server" PageTitle="<%# PageTitle %>" />
<main class="container">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %>
    <div class="alert alert-<%= AlertType %>"><%= AlertMessage %></div>
<% } %>
<div class="card">
    <div class="card-header"><h2><%= Input.Id > 0 ? "Edit " + ItemLabel : "Add " + ItemLabel %></h2></div>
    <form method="post" action="<%= PagePath %>.aspx">
        <input type="hidden" name="__handler" value="Save" />
        <input type="hidden" name="itemId" value="<%= Input.Id %>" />
        <div class="card-body"><div class="form-grid">
            <div class="form-group">
                <label><%= ItemLabel %> <span class="required">*</span></label>
                <input type="text" name="itemName" class="form-control" value="<%= Input.Name %>" maxlength="100" />
            </div>
            <% if (ShowAlias) { %>
            <div class="form-group">
                <label><%= AliasLabel %></label>
                <input type="text" name="aliasName" class="form-control" value="<%= Input.AliasName %>" maxlength="<%= AliasMaxLength %>" />
            </div>
            <% } %>
            <div class="form-group">
                <label>Status</label>
                <label class="checkbox-label">
                    <input type="checkbox" name="isActive" value="true" <%= Input.IsActive ? "checked" : "" %> /> Active
                </label>
            </div>
        </div></div>
        <div class="card-footer">
            <button type="submit" class="btn btn-primary"><%= Input.Id > 0 ? "Update " + ItemLabel : "Save " + ItemLabel %></button>
            <a href="<%= PagePath %>.aspx" class="btn btn-secondary">Clear</a>
        </div>
    </form>
</div>
<div class="card mt-4">
    <div class="card-header"><h2><%= ItemLabel %> List</h2></div>
    <div class="card-body table-responsive">
        <table class="data-table">
            <thead class="grid-header"><tr><th><%= ItemLabel %></th><% if (ShowAlias) { %><th><%= AliasLabel %></th><% } %><th>Status</th><th>Actions</th></tr></thead>
            <tbody>
            <% if (Records.Count == 0) { %>
                <tr class="empty-row"><td colspan="4">No records found.</td></tr>
            <% } else { foreach (var item in Records) { %>
                <tr>
                    <td><%= item.Name %></td>
                    <% if (ShowAlias) { %><td><%= item.AliasName %></td><% } %>
                    <td><span class="badge <%= item.IsActive ? "badge-success" : "badge-danger" %>"><%= item.IsActive ? "Active" : "Inactive" %></span></td>
                    <td class="actions-col">
                        <a class="btn-icon btn-edit" href="<%= PagePath %>.aspx?editId=<%= item.Id %>">Edit</a>
                        <form method="post" action="<%= PagePath %>.aspx" style="display:inline">
                            <input type="hidden" name="__handler" value="Delete" />
                            <input type="hidden" name="deleteId" value="<%= item.Id %>" />
                            <button type="submit" class="btn-icon btn-delete">X</button>
                        </form>
                    </td>
                </tr>
            <% } } %>
            </tbody>
        </table>
    </div>
</div>
</main>
<%@ Register Src="~/Controls/AppFooter.ascx" TagPrefix="hrms" TagName="AppFooter" %>
<hrms:AppFooter runat="server" />
</form>
<script src="/js/app.js"></script>
<script src="/js/confirm-delete.js"></script>
</body>
</html>
"@
    Set-Content (Join-Path $root "$Name.aspx") $aspx -Encoding UTF8
}

# ── Process lookup setup pages ────────────────────────────────────────────────
$lookupFiles = Get-ChildItem "$pagesDir\*Setup.cshtml.cs" -File | Where-Object {
    (Get-Content $_.FullName -Raw) -match ': LookupSetupPageModel'
}

foreach ($lf in $lookupFiles) {
    $name = $lf.BaseName -replace '\.cshtml$',''
    $raw = Get-Content $lf.FullName -Raw

    $table = if ($raw -match 'TableName => "(\w+)"') { $Matches[1] } else { "" }
    $idCol = if ($raw -match 'IdColumn => "(\w+)"') { $Matches[1] } else { "" }
    $nameCol = if ($raw -match 'NameColumn => "(\w+)"') { $Matches[1] } else { "" }
    $aliasCol = if ($raw -match 'AliasColumn => "(\w+)"') { $Matches[1] } else { $null }
    $pageTitle = if ($raw -match 'PageTitle => "([^"]+)"') { $Matches[1] } else { $name }
    $itemLabel = if ($raw -match 'ItemLabel => "([^"]+)"') { $Matches[1] } else { $name }
    $pagePath = if ($raw -match 'PagePath => "([^"]+)"') { $Matches[1].TrimStart('/') } else { $name }

    $aliasOverride = if ($aliasCol) { "protected override string AliasColumn => `"$aliasCol`";" } else { "" }

    $cs = @"
using HRMS.Services;

namespace HRMS
{
    public partial class ${name}Page : LookupSetupBasePage
    {
        protected override string TableName => "$table";
        protected override string IdColumn => "$idCol";
        protected override string NameColumn => "$nameCol";
        $aliasOverride
        public override string PageTitle => "$pageTitle";
        public override string ItemLabel => "$itemLabel";
        public override string PagePath => "/$pagePath";
    }
}
"@
    Set-Content (Join-Path $root "$name.aspx.cs") $cs -Encoding UTF8
    New-LookupPage -Name $name -CodeBehind "${name}Page"
    Write-Host "Lookup: $name"
}

# ── Login page ─────────────────────────────────────────────────────────────────
@'
<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="HRMS.LoginPage" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="UTF-8" />
    <title>HRMS – Login</title>
    <link rel="stylesheet" href="/css/style.css?v=9" />
    <style>
        body { min-height:100vh; display:flex; align-items:center; justify-content:center;
               background:linear-gradient(135deg, var(--gb-blue) 0%, var(--gb-blue-dark) 55%, #1a1d5c 100%); }
        .login-card { width:100%; max-width:420px; background:#fff; border-radius:10px;
                      box-shadow:0 20px 60px rgba(46,49,146,.35); overflow:hidden; }
        .login-header { padding:1.5rem 2rem 1rem; text-align:center; border-bottom:4px solid var(--gb-red); }
        .login-body { padding:2rem; }
    </style>
</head>
<body>
<form id="form1" runat="server">
<div class="login-card">
    <div class="login-header">
        <img src="/images/gb-logo.png" alt="Ghazi Brothers" style="height:52px;margin-bottom:.75rem;" />
        <h1 style="margin:0;color:var(--gb-blue);">HRMS</h1>
        <p style="color:var(--gb-red);font-weight:600;text-transform:uppercase;font-size:.82rem;">Human Resource Management System</p>
    </div>
    <div class="login-body">
        <% if (!string.IsNullOrEmpty(ErrorMessage)) { %>
        <div class="alert alert-error" style="margin-bottom:1rem;"><%= ErrorMessage %></div>
        <% } %>
        <div class="form-group">
            <label>Username <span class="required">*</span></label>
            <asp:TextBox ID="txtUsername" runat="server" CssClass="form-control" />
        </div>
        <div class="form-group">
            <label>Password <span class="required">*</span></label>
            <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" CssClass="form-control" />
        </div>
        <asp:Button ID="btnLogin" runat="server" Text="Sign In" CssClass="btn btn-primary"
                    style="width:100%;margin-top:.5rem;" OnClick="btnLogin_Click" />
    </div>
</div>
</form>
</body>
</html>
'@ | Set-Content (Join-Path $root "Login.aspx") -Encoding UTF8

@'
using System;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public partial class LoginPage : AppBasePage
    {
        protected override bool IsPublicPage => true;

        public string ErrorMessage { get; private set; } = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Request.QueryString["logout"] == "1")
            {
                Audit.LogLogout();
                Auth.Logout();
                Response.Redirect("~/Login.aspx");
                return;
            }

            if (Auth.IsLoggedIn)
                Response.Redirect("~/Home.aspx");
        }

        protected void btnLogin_Click(object sender, EventArgs e)
        {
            var result = Auth.Login(txtUsername.Text, txtPassword.Text);
            if (!result.Success)
            {
                Audit.LogLogin(txtUsername.Text.Trim(), false, message: result.Message);
                ErrorMessage = result.Message;
                return;
            }

            Session["ShowNotificationPopup"] = 1;
            Session["ShowMemorandumPopup"] = 1;
            if (!Auth.IsAdmin && !Auth.LinkedEmployeeId.HasValue)
                Session["ShowProfileSyncWarning"] = 1;

            Response.Redirect("~/Home.aspx");
        }
    }
}
'@ | Set-Content (Join-Path $root "Login.aspx.cs") -Encoding UTF8

# ── Home page (from Index) ─────────────────────────────────────────────────────
Copy-Item (Join-Path $pagesDir "Index.cshtml") (Join-Path $root "Home.aspx.raw") -Force -ErrorAction SilentlyContinue
Write-Host "Conversion complete. Run database scripts in D:\Project\DATA"
