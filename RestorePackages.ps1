# Restore NuGet packages without writing nuget.exe into the project folder.
# Downloads .nupkg files to %TEMP% and extracts into .\packages

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$packagesDir = Join-Path $root "packages"
$tempRoot = Join-Path $env:TEMP ("HRMS-nuget-" + [guid]::NewGuid().ToString("N"))

New-Item -ItemType Directory -Force -Path $packagesDir | Out-Null
New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null

$packages = @(
    @{ Id = "ClosedXML"; Version = "0.102.2" },
    @{ Id = "DocumentFormat.OpenXml"; Version = "2.16.0" },
    @{ Id = "ExcelNumberFormat"; Version = "1.1.0" },
    @{ Id = "Irony.NetCore"; Version = "1.0.11" },
    @{ Id = "SixLabors.Fonts"; Version = "1.0.0" },
    @{ Id = "System.IO.Packaging"; Version = "6.0.0" },
    @{ Id = "XLParser"; Version = "1.5.2" }
)

function Get-NupkgUrl([string]$id, [string]$version) {
    $idLower = $id.ToLowerInvariant()
    return "https://api.nuget.org/v3-flatcontainer/$idLower/$version/$idLower.$version.nupkg"
}

try {
    foreach ($p in $packages) {
        $folderName = "$($p.Id).$($p.Version)"
        $dest = Join-Path $packagesDir $folderName
        $markerDllPatterns = @(
            (Join-Path $dest "lib\*\*.dll"),
            (Join-Path $dest "lib\*.dll")
        )

        $already = $false
        foreach ($pat in $markerDllPatterns) {
            if (Get-ChildItem -Path $pat -ErrorAction SilentlyContinue | Select-Object -First 1) {
                $already = $true
                break
            }
        }
        if ($already) {
            Write-Host "OK (cached): $folderName"
            continue
        }

        $url = Get-NupkgUrl $p.Id $p.Version
        $nupkg = Join-Path $tempRoot "$folderName.nupkg"
        $zip = Join-Path $tempRoot "$folderName.zip"
        $extract = Join-Path $tempRoot $folderName

        Write-Host "Downloading $($p.Id) $($p.Version) ..."
        try {
            Invoke-WebRequest -Uri $url -OutFile $nupkg -UseBasicParsing
        }
        catch {
            Write-Host "FAILED download: $url"
            Write-Host $_.Exception.Message
            throw
        }

        Copy-Item $nupkg $zip -Force
        if (Test-Path $extract) { Remove-Item $extract -Recurse -Force }
        Expand-Archive -Path $zip -DestinationPath $extract -Force

        if (Test-Path $dest) { Remove-Item $dest -Recurse -Force }
        New-Item -ItemType Directory -Force -Path $dest | Out-Null
        Copy-Item -Path (Join-Path $extract "*") -Destination $dest -Recurse -Force

        Write-Host "Installed: $folderName"
    }

    $closedXml = Join-Path $packagesDir "ClosedXML.0.102.2\lib\netstandard2.0\ClosedXML.dll"
    if (-not (Test-Path $closedXml)) {
        # Some packages use netstandard2.1 or net46 — find any ClosedXML.dll
        $found = Get-ChildItem -Path (Join-Path $packagesDir "ClosedXML.0.102.2") -Filter ClosedXML.dll -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($found) {
            Write-Host "ClosedXML.dll found at: $($found.FullName)"
            Write-Host "If build still fails, update HintPath in HRMS.csproj to that path."
        }
        else {
            throw "ClosedXML.dll was not found after restore."
        }
    }
    else {
        Write-Host "SUCCESS: $closedXml"
    }
}
finally {
    if (Test-Path $tempRoot) {
        Remove-Item $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
