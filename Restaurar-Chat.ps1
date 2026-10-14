# Script para restaurar a conversa do Antigravity neste novo PC.
# Uso: No novo computador, abra o PowerShell nesta pasta e execute: .\Restaurar-Chat.ps1

$ErrorActionPreference = "Stop"

$conversationId = "0e16ef42-642e-4aa0-9486-d48668a3d192"
$userProfile = [System.Environment]::GetFolderPath('UserProfile')
$targetAgyDir = Join-Path $userProfile ".gemini\antigravity"
$backupDir = Join-Path $PSScriptRoot ".chat_backup"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   RESTAURAÇÃO DO CHAT ANTIGRAVITY - GAMEJAM V" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "Destino: $targetAgyDir" -ForegroundColor Yellow

if (-not (Test-Path $backupDir)) {
    Write-Error "Pasta de backup .chat_backup não encontrada. Certifique-se de que clonou o repositório completo."
    exit 1
}

# Garante estrutura de pastas do Antigravity
$targetConvDir = Join-Path $targetAgyDir "conversations"
$targetBrainDir = Join-Path $targetAgyDir "brain"

if (-not (Test-Path $targetConvDir)) { New-Item -ItemType Directory -Path $targetConvDir -Force | Out-Null }
if (-not (Test-Path $targetBrainDir)) { New-Item -ItemType Directory -Path $targetBrainDir -Force | Out-Null }

# 1. Restaura bancos de dados da conversa
$backupConvDir = Join-Path $backupDir "conversations"
if (Test-Path $backupConvDir) {
    $dbFiles = Get-ChildItem -Path $backupConvDir -Filter "$conversationId.db*" -File
    foreach ($file in $dbFiles) {
        Copy-Item -Path $file.FullName -Destination $targetConvDir -Force
        Write-Host "[OK] Restaurado banco: $($file.Name)" -ForegroundColor Green
    }
}

# 2. Restaura summaries se existirem
$summaryFiles = Get-ChildItem -Path $backupDir -Filter "conversation_summaries.db*" -File
foreach ($file in $summaryFiles) {
    Copy-Item -Path $file.FullName -Destination $targetAgyDir -Force
    Write-Host "[OK] Restaurado resumo: $($file.Name)" -ForegroundColor Green
}

# 3. Restaura pasta brain da conversa
$backupBrainDir = Join-Path $backupDir "brain\$conversationId"
if (Test-Path $backupBrainDir) {
    $targetConvBrain = Join-Path $targetBrainDir $conversationId
    Copy-Item -Path $backupBrainDir -Destination $targetBrainDir -Recurse -Force
    Write-Host "[OK] Restaurada pasta brain com logs e histórico." -ForegroundColor Green
}

Write-Host ""
Write-Host "==========================================================" -ForegroundColor Green
Write-Host "   CHAT RESTAURADO COM SUCESSO NESTE PC!" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
Write-Host "Abra o aplicativo ou extensão do Antigravity e seu chat estará pronto para continuar!" -ForegroundColor Cyan
