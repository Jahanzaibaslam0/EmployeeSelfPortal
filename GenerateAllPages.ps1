# Run: powershell -ExecutionPolicy Bypass -File D:\Project\HRMS\GenerateAllPages.ps1
$root = "D:\Project\HRMS"; $data = "D:\Project\DATA"
New-Item -Force -ItemType Directory $data | Out-Null
Copy-Item "$root\Database\*.sql" $data -Force
foreach ($f in 'css','js','images') { if (Test-Path "$root\wwwroot\$f") { robocopy "$root\wwwroot\$f" "$root\$f" /E /NFL /NDL /NJH /NJS | Out-Null } }
. "$root\GenerateLookupPages.ps1"
