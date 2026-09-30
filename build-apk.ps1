<#
    Compile l'application .NET MAUI pour Android, puis installe l'APK
    sur l'appareil Android connecte ou sur le premier emulateur disponible.

    Usage :  .\build-apk.ps1 [Release|Debug]
#>

param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$projectDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$project      = Join-Path $projectDir "CalculatriceMaui\CalculatriceMaui.csproj"
$targetFramework = "net10.0-android"

Write-Host "==> Configuration : $Configuration"

$workloads = dotnet workload list
if ($workloads -notmatch "maui-android") {
    Write-Host "==> Installation de la charge de travail maui-android..."
    dotnet workload install maui-android
}

Write-Host "==> Nettoyage et compilation..."
dotnet clean $project -f $targetFramework -c $Configuration
dotnet build $project -f $targetFramework -c $Configuration -p:AndroidPackageFormat=apk

$outputDir  = Join-Path $projectDir "CalculatriceMaui\bin\$targetFramework\$Configuration"
$apk        = Get-ChildItem -Path $outputDir -Filter *.apk -Recurse | Select-Object -First 1

if (-not $apk) {
    throw "Aucun APK n'a été généré dans $outputDir"
}

Write-Host "==> APK généré : $($apk.FullName)"

if (-not (Get-Command adb -ErrorAction SilentlyContinue)) {
    Write-Host "==> 'adb' est introuvable : copiez manuellement l'APK sur votre téléphone."
    exit 0
}

$devices = adb devices | Select-Object -Skip 1 | Where-Object { $_ -match "device$" }

if (-not $devices) {
    Write-Host "==> Aucun appareil détecté. Démarrez un émulateur puis relancez le script."
    exit 0
}

$device = ($devices[0] -split "\s+")[0]

Write-Host "==> Installation sur l'appareil $device..."
adb -s $device install -r $apk.FullName
adb -s $device shell monkey -p com.djogukameni.calculatrice -c android.intent.category.LAUNCHER 1 | Out-Null

Write-Host "==> Terminé : la calculatrice est lancée."
