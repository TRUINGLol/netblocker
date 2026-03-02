# NetBlocker Build Script
$desktopProject = "InternetBlocker.Desktop/InternetBlocker.Desktop.csproj"
$outputBase = "publish"
$appName = "NetBlocker"

if (-not (Test-Path $desktopProject)) {
    Write-Host "Error: run the script from the solution root folder!" -ForegroundColor Red
    Write-Host "Current folder: $(Get-Location)" -ForegroundColor Yellow
    exit 1
}

Write-Host "Projects found:" -ForegroundColor Green
Write-Host "  - Desktop: $desktopProject"
Write-Host "  - Core: InternetBlocker/InternetBlocker.csproj" 
Write-Host "  - Infrastructure: InternetBlocker.Infrastructure/InternetBlocker.Infrastructure.csproj"

Write-Host "`nRestoring packages..." -ForegroundColor Cyan
dotnet restore

$commonParams = @(
    "-c", "Release",
    "--self-contained", "true",
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-p:PublishTrimmed=false",
    "-p:DebugType=none",
    "-p:DebugSymbols=false"
)

# Windows
Write-Host "`nBuilding for Windows..." -ForegroundColor Cyan
dotnet publish $desktopProject -r win-x64 @commonParams -o "${outputBase}/win-x64"
if ($LASTEXITCODE -eq 0 -and (Test-Path "${outputBase}/win-x64/InternetBlocker.Desktop.exe")) {
    $winTarget = "${outputBase}/win-x64/${appName}.exe"
    if (Test-Path $winTarget) { Remove-Item $winTarget -Force }
    Rename-Item "${outputBase}/win-x64/InternetBlocker.Desktop.exe" "${appName}.exe" -Force
    Write-Host "  Renamed to ${appName}.exe" -ForegroundColor Green
}

# Linux
Write-Host "`nBuilding for Linux..." -ForegroundColor Cyan
dotnet publish $desktopProject -r linux-x64 @commonParams -o "${outputBase}/linux-x64"
if ($LASTEXITCODE -eq 0 -and (Test-Path "${outputBase}/linux-x64/InternetBlocker.Desktop")) {
    $linuxTarget = "${outputBase}/linux-x64/${appName}"
    if (Test-Path $linuxTarget) { Remove-Item $linuxTarget -Force }
    Rename-Item "${outputBase}/linux-x64/InternetBlocker.Desktop" "${appName}" -Force
    Write-Host "  Renamed to ${appName}" -ForegroundColor Green
}

# macOS Intel
Write-Host "`nBuilding for macOS Intel..." -ForegroundColor Cyan
dotnet publish $desktopProject -r osx-x64 @commonParams -o "${outputBase}/osx-x64"
if ($LASTEXITCODE -eq 0 -and (Test-Path "${outputBase}/osx-x64/InternetBlocker.Desktop")) {
    $osx64Target = "${outputBase}/osx-x64/${appName}"
    if (Test-Path $osx64Target) { Remove-Item $osx64Target -Force }
    Rename-Item "${outputBase}/osx-x64/InternetBlocker.Desktop" "${appName}" -Force
    Write-Host "  Renamed to ${appName}" -ForegroundColor Green
}

# macOS Apple Silicon
Write-Host "`nBuilding for macOS Apple Silicon..." -ForegroundColor Cyan
dotnet publish $desktopProject -r osx-arm64 @commonParams -o "${outputBase}/osx-arm64"
if ($LASTEXITCODE -eq 0 -and (Test-Path "${outputBase}/osx-arm64/InternetBlocker.Desktop")) {
    $osxArmTarget = "${outputBase}/osx-arm64/${appName}"
    if (Test-Path $osxArmTarget) { Remove-Item $osxArmTarget -Force }
    Rename-Item "${outputBase}/osx-arm64/InternetBlocker.Desktop" "${appName}" -Force
    Write-Host "  Renamed to ${appName}" -ForegroundColor Green
}

Write-Host "`nBuild complete! Check the ${outputBase} folder." -ForegroundColor Green

Write-Host "`nContents of ${outputBase} folder:" -ForegroundColor Yellow
Get-ChildItem -Path ${outputBase} -Recurse | Where-Object { -not $_.PSIsContainer } | ForEach-Object {
    Write-Host "  $($_.FullName)" -ForegroundColor Gray
}

function Create-MacAppBundle {
    param($sourcePath, $arch)
    
    $bundleName = "NetBlocker"
    $bundlePath = "${outputBase}/${bundleName}-${arch}.app"
    $contentsPath = "$bundlePath/Contents"
    $macOSPath = "$contentsPath/MacOS"
    $resourcesPath = "$contentsPath/Resources"
    
    New-Item -ItemType Directory -Force -Path $macOSPath | Out-Null
    New-Item -ItemType Directory -Force -Path $resourcesPath | Out-Null
    
    Copy-Item "${sourcePath}/*" -Destination $macOSPath -Recurse -Force

    $infoPlist = @"
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key>
    <string>${bundleName}</string>
    <key>CFBundleDisplayName</key>
    <string>${bundleName}</string>
    <key>CFBundleIdentifier</key>
    <string>com.netblocker.app</string>
    <key>CFBundleVersion</key>
    <string>1.0.0</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleExecutable</key>
    <string>${bundleName}</string>
    <key>CFBundleIconFile</key>
    <string>AppIcon</string>
    <key>LSMinimumSystemVersion</key>
    <string>10.15</string>
    <key>NSHighResolutionCapable</key>
    <true/>
</dict>
</plist>
"@
    
    $infoPlist | Out-File -FilePath "$contentsPath/Info.plist" -Encoding UTF8
    
    # Try to copy icon if exists
    $iconSource = "InternetBlocker/Assets/AppIcon.icns"
    if (Test-Path $iconSource) {
        Copy-Item $iconSource -Destination "$resourcesPath/AppIcon.icns" -Force
    }

    Write-Host "  .app bundle created: $bundlePath" -ForegroundColor Green
}

if (Test-Path "${outputBase}/osx-x64/${appName}") {
    Create-MacAppBundle -sourcePath "${outputBase}/osx-x64" -arch "x64"
}

if (Test-Path "${outputBase}/osx-arm64/${appName}") {
    Create-MacAppBundle -sourcePath "${outputBase}/osx-arm64" -arch "arm64"
}