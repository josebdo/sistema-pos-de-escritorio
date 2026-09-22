<#
.SYNOPSIS
    Script automatizado para compilar y generar el instalador de Veyra POS.
.DESCRIPTION
    1. Publica la aplicación .NET 8 en modo autocontenido (win-x64).
    2. Localiza Inno Setup Compiler (ISCC.exe) y genera el archivo Setup .exe.
#>

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "            VEYRA POS - GENERADOR DE INSTALADOR           " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host ""

$rootPath = $PSScriptRoot
$projectPath = Join-Path $rootPath "src\SistemaCelulares.App\SistemaCelulares.App.csproj"
$publishDir = Join-Path $rootPath "publish\win-x64"
$issScript = Join-Path $rootPath "installer.iss"
$outputDir = Join-Path $rootPath "installer-output"

# 1. Limpieza de publicación anterior
if (Test-Path $publishDir) {
    Write-Host "[1/3] Limpiando carpeta de publicación anterior..." -ForegroundColor Yellow
    Remove-Item -Path $publishDir -Recurse -Force -ErrorAction SilentlyContinue
}

# 2. Publicar .NET 8 Autocontenido
Write-Host "[2/3] Publicando aplicación .NET 8 (win-x64 autocontenida)..." -ForegroundColor Green
dotnet publish $projectPath -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o $publishDir

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Error durante la publicación de .NET." -ForegroundColor Red
    exit 1
}

Write-Host "✓ Publicación completada con éxito en: $publishDir" -ForegroundColor Green
Write-Host ""

# 3. Buscar ISCC.exe (Inno Setup Compiler)
Write-Host "[3/3] Buscando Inno Setup Compiler para generar el instalador .exe..." -ForegroundColor Green

$isccPaths = @(
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 5\ISCC.exe"
)

$isccExe = $null
foreach ($path in $isccPaths) {
    if (Test-Path $path) {
        $isccExe = $path
        break
    }
}

if (-not $isccExe) {
    $commandCheck = Get-Command iscc -ErrorAction SilentlyContinue
    if ($commandCheck) {
        $isccExe = "iscc"
    }
}

if ($isccExe) {
    Write-Host "✓ Inno Setup detectado en: $isccExe" -ForegroundColor Green
    Write-Host "Compilando instalador..." -ForegroundColor Cyan
    
    if (-not (Test-Path $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    }

    & $isccExe $issScript

    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "==========================================================" -ForegroundColor Green
        Write-Host "🎉 ¡INSTALADOR GENERADO CON ÉXITO!" -ForegroundColor Green
        Write-Host "Ubicación: $outputDir\SistemaCelulares_Setup_v1.0.0.exe" -ForegroundColor Green
        Write-Host "==========================================================" -ForegroundColor Green
    } else {
        Write-Host "❌ Error al compilar el script con Inno Setup." -ForegroundColor Red
    }
} else {
    Write-Host "⚠️ No se encontró Inno Setup instalado en la máquina." -ForegroundColor Yellow
    Write-Host "Para compilar el instalador final .exe:" -ForegroundColor Yellow
    Write-Host "1. Descarga e instala Inno Setup desde: https://jrsoftware.org/isdl.php" -ForegroundColor White
    Write-Host "2. Abre el archivo '$issScript' y presiona Compile (Ctrl + F9)" -ForegroundColor White
}
