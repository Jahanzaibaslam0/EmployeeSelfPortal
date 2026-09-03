#Requires -Version 5.1
$ErrorActionPreference = "Stop"
$root = "D:\Project\HRMS"
$pages = Join-Path $root "Pages"

Write-Host "=== HRMS Web Forms full conversion ==="

# 1) Static assets
foreach ($folder in @("css","js","images")) {
    $src = Join-Path $root "wwwroot\$folder"
    $dst = Join-Path $root $folder
    if (Test-Path $src) {
        New-Item -ItemType Directory -Force -Path $dst | Out-Null
        Copy-Item "$src\*" $dst -Recurse -Force
        Write-Host "Copied $folder"
    }
}

# 2) DATA folder scripts
$data = "D:\Project\DATA"
New-Item -ItemType Directory -Force -Path $data | Out-Null
Copy-Item (Join-Path $root "Database\*.sql") $data -Force -ErrorAction SilentlyContinue

# 3) Lookup pages from Pages\*Setup.cshtml.cs inheriting LookupSetupPageModel
function New-LookupAspx($Name) {
@"
<%@ Page Language="C#" MasterPageFile="~/LookupSetup.Master" AutoEventWireup="true" CodeBehind="$Name.aspx.cs" Inherits="HRMS.${Name}Page" %>
"@
}

$lookupCount = 0
Get-ChildItem "$pages\*Setup.cshtml.cs" -File -ErrorAction SilentlyContinue | ForEach-Object {
    $raw = Get-Content $_.FullName -Raw
    if ($raw -notmatch 'LookupSetupPageModel') { return }
    $name = $_.BaseName -replace '\.cshtml$',''
    $table = if ($raw -match 'TableName\s*=>\s*"(\w+)"') { $Matches[1] } else { return }
    $idCol = if ($raw -match 'IdColumn\s*=>\s*"(\w+)"') { $Matches[1] } else { "${name}ID" }
    $nameCol = if ($raw -match 'NameColumn\s*=>\s*"(\w+)"') { $Matches[1] } else { "${name}Name" }
    $aliasCol = if ($raw -match 'AliasColumn\s*=>\s*"(\w+)"') { $Matches[1] } else { $null }
    $pageTitle = if ($raw -match 'PageTitle\s*=>\s*"([^"]+)"') { $Matches[1] } else { $name }
    $itemLabel = if ($raw -match 'ItemLabel\s*=>\s*"([^"]+)"') { $Matches[1] } else { $name }
    $pagePath = if ($raw -match 'PagePath\s*=>\s*"([^"]+)"') { $Matches[1].TrimStart('/') } else { $name }

    $aliasLine = if ($aliasCol) { "        protected override string AliasColumn => `"$aliasCol`";" } else { "" }
    $cs = @"
using HRMS.Services;

namespace HRMS
{
    public partial class ${name}Page : LookupSetupBasePage
    {
        protected override string TableName => `"$table`";
        protected override string IdColumn => `"$idCol`";
        protected override string NameColumn => `"$nameCol`";
$aliasLine
        public override string PageTitle => `"$pageTitle`";
        public override string ItemLabel => `"$itemLabel`";
        public override string PagePath => `"/$pagePath`";
    }
}
"@
    Set-Content (Join-Path $root "$name.aspx.cs") $cs -Encoding UTF8
    Set-Content (Join-Path $root "$name.aspx") (New-LookupAspx $name) -Encoding UTF8
    $lookupCount++
    Write-Host "Lookup: $name"
}
Write-Host "Generated $lookupCount lookup pages"

# 4) Convert remaining PageModels to ASPX
function Convert-RazorMarkup([string]$html, [string]$pageTitle) {
    # Strip directives
    $html = $html -replace '(?m)^\s*@page.*\r?\n',''
    $html = $html -replace '(?m)^\s*@model.*\r?\n',''
    $html = $html -replace '(?m)^\s*@using.*\r?\n',''
    $html = $html -replace '(?m)^\s*@\{[\s\S]*?\}\s*\r?\n',''
    # Remove layout partials — Site.Master provides chrome
    $html = $html -replace '@await Html\.PartialAsync\("_AppHeader"[^)]*\)',''
    $html = $html -replace '@await Html\.PartialAsync\("_AppFooter"[^)]*\)',''
    $html = $html -replace '@await Html\.PartialAsync\("_NotificationPopup"[^)]*\)',''
    $html = $html -replace '@await Html\.PartialAsync\("_MasterExcelPanel"[^)]*\)',''
    $html = $html -replace '@await Html\.PartialAsync\("_LookupSetupPage"[^)]*\)','<!-- lookup UI via LookupSetup.Master -->'
    $html = $html -replace '@Html\.AntiForgeryToken\(\)',''
    # Remove outer html/body/head if present — use ContentPlaceHolder
    if ($html -match '(?is)<body[^>]*>([\s\S]*)</body>') {
        $html = $Matches[1]
    }
    if ($html -match '(?is)<main[^>]*>([\s\S]*)</main>') {
        $html = $Matches[1]
    }
    # Razor → ASPX inline
    $html = $html -replace '@Model\.', ''
    $html = $html -replace '@\(', '<%='
    # crude: @if ( -> <% if (
    $html = $html -replace '@if\s*\(', '<% if ('
    $html = $html -replace '@else\s*if\s*\(', '<% else if ('
    $html = $html -replace '@else\s*\{', '<% else {'
    $html = $html -replace '@foreach\s*\(', '<% foreach ('
    $html = $html -replace '@for\s*\(', '<% for ('
    $html = $html -replace '@while\s*\(', '<% while ('
    # Closing braces that were Razor code blocks: "} else {" already handled; lone "}" at line starts after code
    # Convert @{ ... } remnants
    $html = $html -replace '@\{', '<%'
    # Tag helpers (basic)
    $html = $html -replace '\sasp-for="[^"]*"',''
    $html = $html -replace '\sasp-page="[^"]*"',''
    $html = $html -replace '\sasp-page-handler="([^"]*)"',' name="__handler" value="$1"'
    $html = $html -replace '\sasp-route-[a-zA-Z]+="[^"]*"',''
    $html = $html -replace 'method="post"\s+action="[^"]*\?handler=([^"]+)"', 'method="post"'
    # Links without .aspx
    $html = $html -replace 'href="(/[A-Za-z][A-Za-z0-9]*)"', 'href="$1.aspx"'
    $html = $html -replace 'href="(/[A-Za-z][A-Za-z0-9]*)\?', 'href="$1.aspx?'
    # Fix double .aspx.aspx
    $html = $html -replace '\.aspx\.aspx', '.aspx'

    return @"
<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="PAGE_NAME.aspx.cs" Inherits="HRMS.PAGE_CLASS" %>
<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
$html
</asp:Content>
"@
}

function Convert-CodeBehind([string]$cs, [string]$className) {
    $cs = $cs -replace 'using Microsoft\.Data\.SqlClient;','using System.Data.SqlClient;'
    $cs = $cs -replace 'using Microsoft\.AspNetCore[^\r\n]*\r?\n',''
    $cs = $cs -replace 'using Microsoft\.Extensions\.Configuration;\r?\n',''
    $cs = $cs -replace 'namespace HRMS\.Pages;?','namespace HRMS'
    $cs = $cs -replace 'namespace HRMS\.Pages\s*\{','namespace HRMS {'
    # Class rename
    $cs = $cs -replace "public\s+(partial\s+)?class\s+\w+Model\s*:\s*PageModel","public partial class ${className} : AppBasePage"
    $cs = $cs -replace "public\s+(partial\s+)?class\s+\w+Model\s*:\s*LookupSetupPageModel","public partial class ${className} : LookupSetupBasePage"
    # Strip DI constructors - replace with empty
    $cs = $cs -replace '(?s)public\s+\w+Model\s*\([^)]*\)\s*\{[^}]*_conn\s*=\s*config\.GetConnectionString\("HRMSConnection"\)[^;]*;[^}]*\}', ''
    $cs = $cs -replace '(?s)public\s+\w+Model\s*\([^)]*IConfiguration[^)]*\)\s*\{[\s\S]*?\n\s*\}', ''
    # Fields
    $cs = $cs -replace 'private\s+readonly\s+string\s+_conn;?\r?\n',''
    $cs = $cs -replace 'private\s+readonly\s+AuthService\s+_auth;?\r?\n',''
    $cs = $cs -replace 'private\s+readonly\s+PermissionService\s+_perms;?\r?\n',''
    $cs = $cs -replace 'private\s+readonly\s+AuditService\s+_audit;?\r?\n',''
    $cs = $cs -replace 'private\s+readonly\s+InventoryService\s+_inventory;?\r?\n',"        private readonly InventoryService _inventory = new InventoryService();`r`n"
    $cs = $cs -replace 'private\s+readonly\s+MasterExcelService\s+_excel;?\r?\n',"        private readonly MasterExcelService _excel = new MasterExcelService();`r`n"
    $cs = $cs -replace 'private\s+readonly\s+NotificationService\s+_notif;?\r?\n',"        private readonly NotificationService _notif = new NotificationService();`r`n"
    $cs = $cs -replace 'private\s+readonly\s+MemorandumService\s+_memo;?\r?\n',"        private readonly MemorandumService _memo = new MemorandumService();`r`n"
    $cs = $cs -replace 'private\s+readonly\s+DashboardService\s+_dash;?\r?\n',"        private readonly DashboardService _dash = new DashboardService();`r`n"
    $cs = $cs -replace 'private\s+readonly\s+EmployeeProfileAccessService\s+_profile;?\r?\n',"        private readonly EmployeeProfileAccessService _profile = new EmployeeProfileAccessService();`r`n"
    $cs = $cs -replace 'private\s+readonly\s+DataAccessScopeService\s+_scope;?\r?\n',"        private readonly DataAccessScopeService _scope = new DataAccessScopeService();`r`n"
    $cs = $cs -replace '\b_conn\b','Conn'
    $cs = $cs -replace '\b_auth\b','Auth'
    $cs = $cs -replace '\b_perms\b','Perms'
    $cs = $cs -replace '\b_audit\b','Audit'
    # IActionResult / RedirectToPage
    $cs = $cs -replace 'public\s+IActionResult\s+','public void '
    $cs = $cs -replace 'public\s+async\s+Task<IActionResult>\s+','public void '
    $cs = $cs -replace 'return\s+RedirectToPage\(\);','return;'
    $cs = $cs -replace 'return\s+RedirectToPage\("([^"]+)"\);','Response.Redirect("~$1.aspx"); return;'
    $cs = $cs -replace 'return\s+Page\(\);','return;'
    $cs = $cs -replace 'return\s+Redirect\("([^"]+)"\);','Response.Redirect("$1"); return;'
    $cs = $cs -replace 'TempData\["Alert"\]','Session["Alert"]'
    $cs = $cs -replace 'TempData\["AlertType"\]','Session["AlertType"]'
    # OnGet / OnPost → Page_Load dispatcher
    if ($cs -notmatch 'Page_Load') {
        $handlers = [regex]::Matches($cs, 'public\s+void\s+(OnPost\w*|OnGet\w*)\s*\(') | ForEach-Object { $_.Groups[1].Value }
        $dispatch = @"

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack)
            {
                var handler = Request.Form["__handler"] ?? Request.QueryString["handler"] ?? "Save";
"@
        foreach ($h in $handlers) {
            if ($h -eq 'OnGet') { continue }
            $key = $h -replace '^OnPost','' -replace '^OnGet',''
            if ([string]::IsNullOrEmpty($key)) { $key = 'Save' }
            $dispatch += "`r`n                if (string.Equals(handler, `"$key`", StringComparison.OrdinalIgnoreCase)) { $h(); return; }"
        }
        $dispatch += @"

            }
            if (!IsPostBack)
            {
                OnGet();
            }
        }
"@
        # Insert Page_Load after class opening brace
        $cs = [regex]::Replace($cs, "(public partial class $className : AppBasePage\s*\{)", "`$1$dispatch", 1)
    }
    # Ensure usings
    if ($cs -notmatch 'using System;') { $cs = "using System;`r`n" + $cs }
    if ($cs -notmatch 'using System\.Web;') { $cs = "using System.Web;`r`n" + $cs }
    if ($cs -notmatch 'using HRMS\.Services;') { $cs = "using HRMS.Services;`r`n" + $cs }
    # File-scoped to block if needed
    if ($cs -match 'namespace HRMS\s*$') {
        $cs = $cs -replace 'namespace HRMS\s*\r?\n', ("namespace HRMS" + "`r`n{`r`n")
        if ($cs -notmatch '\}\s*$') { $cs = $cs + "`r`n}`r`n" }
    }
    return $cs
}

$skip = @('LookupSetupBase','Index','_ViewImports')
$converted = 0
Get-ChildItem "$pages\*.cshtml.cs" -File | Where-Object {
    $_.Name -notmatch '\.cshtml\.cshtml' -and $_.BaseName -notmatch '^_'
} | ForEach-Object {
    $base = $_.BaseName -replace '\.cshtml$',''
    if ($skip -contains $base) { return }
    $raw = Get-Content $_.FullName -Raw
    if ($raw -match 'LookupSetupPageModel') { return } # already handled

    $className = "${base}Page"
    $cs = Convert-CodeBehind $raw $className
    # Fix class name if still *Model
    $cs = $cs -replace "class\s+${base}Model","class $className"
    $cs = $cs -replace "class\s+\w+Model","class $className"

    Set-Content (Join-Path $root "$base.aspx.cs") $cs -Encoding UTF8

    $cshtmlPath = Join-Path $pages "$base.cshtml"
    if (Test-Path $cshtmlPath) {
        $markup = Get-Content $cshtmlPath -Raw
        $aspx = Convert-RazorMarkup $markup $base
        $aspx = $aspx -replace 'PAGE_NAME',$base -replace 'PAGE_CLASS',$className
        Set-Content (Join-Path $root "$base.aspx") $aspx -Encoding UTF8
    } else {
        # code-only page
        $aspx = @"
<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="$base.aspx.cs" Inherits="HRMS.$className" %>
<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">
<p>$base</p>
</asp:Content>
"@
        Set-Content (Join-Path $root "$base.aspx") $aspx -Encoding UTF8
    }
    $converted++
    Write-Host "Page: $base"
}

# Index -> already have Home; Default.aspx
if (-not (Test-Path (Join-Path $root "Default.aspx"))) {
@"
<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="HRMS.DefaultPage" %>
"@ | Set-Content (Join-Path $root "Default.aspx") -Encoding UTF8
@"
namespace HRMS
{
    public partial class DefaultPage : AppBasePage
    {
        protected void Page_Load(object sender, System.EventArgs e)
        {
            Response.Redirect(Auth.IsLoggedIn ? "~/Home.aspx" : "~/Login.aspx");
        }
    }
}
"@ | Set-Content (Join-Path $root "Default.aspx.cs") -Encoding UTF8
}

# InitDatabase / ResetAdmin public
foreach ($pub in @('InitDatabase','ResetAdmin','Login')) {
    $cf = Join-Path $root "$pub.aspx.cs"
    if (Test-Path $cf) {
        $c = Get-Content $cf -Raw
        if ($c -notmatch 'IsPublicPage') {
            $c = $c -replace "(public partial class ${pub}Page : AppBasePage\s*\{)", "`$1`r`n        protected override bool IsPublicPage => true;`r`n"
            Set-Content $cf $c -Encoding UTF8
        }
    }
}

Write-Host "Converted $converted non-lookup pages"
Write-Host "=== Done ==="
