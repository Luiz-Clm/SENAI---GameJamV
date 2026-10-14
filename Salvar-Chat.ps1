# Script para salvar e fazer backup completo desta conversa do Antigravity no repositório Git.
# Uso: No terminal PowerShell, execute: .\Salvar-Chat.ps1

$ErrorActionPreference = "Stop"

$conversationId = "0e16ef42-642e-4aa0-9486-d48668a3d192"
$userProfile = [System.Environment]::GetFolderPath('UserProfile')
$sourceAgyDir = Join-Path $userProfile ".gemini\antigravity"
$backupDir = Join-Path $PSScriptRoot ".chat_backup"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   BACKUP DO CHAT ANTIGRAVITY - GAMEJAM V" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "ID da Conversa: $conversationId" -ForegroundColor Yellow

if (-not (Test-Path $sourceAgyDir)) {
    Write-Warning "Diretório do Antigravity não encontrado em $sourceAgyDir"
    exit 1
}

# Cria diretórios de destino
$convBackup = Join-Path $backupDir "conversations"
$brainBackup = Join-Path $backupDir "brain\$conversationId"

if (-not (Test-Path $convBackup)) { New-Item -ItemType Directory -Path $convBackup -Force | Out-Null }
if (-not (Test-Path (Join-Path $backupDir "brain"))) { New-Item -ItemType Directory -Path (Join-Path $backupDir "brain") -Force | Out-Null }

# 1. Copia arquivos de banco SQLite da conversa (.db, .db-wal, .db-shm)
$dbFiles = Get-ChildItem -Path (Join-Path $sourceAgyDir "conversations") -Filter "$conversationId.db*" -File
foreach ($file in $dbFiles) {
    Copy-Item -Path $file.FullName -Destination $convBackup -Force
    Write-Host "[OK] Copiado banco de dados: $($file.Name)" -ForegroundColor Green
}

# 2. Copia resumos globais se existirem
$summaryFiles = Get-ChildItem -Path $sourceAgyDir -Filter "conversation_summaries.db*" -File
foreach ($file in $summaryFiles) {
    Copy-Item -Path $file.FullName -Destination $backupDir -Force
    Write-Host "[OK] Copiado resumo: $($file.Name)" -ForegroundColor Green
}

# 3. Copia pasta brain da conversa (logs, transcrições, arquivos gerados)
$brainSource = Join-Path $sourceAgyDir "brain\$conversationId"
if (Test-Path $brainSource) {
    Copy-Item -Path $brainSource -Destination (Join-Path $backupDir "brain") -Recurse -Force
    Write-Host "[OK] Copiada pasta brain com transcripts e logs completos." -ForegroundColor Green
}

Write-Host ""
Write-Host "Backup concluído com sucesso em: $backupDir" -ForegroundColor Green
Write-Host "Ao fazer git add, commit e push, seu chat estará salvo na nuvem (GitHub)!" -ForegroundColor Yellow
Write-Host "No novo PC, basta rodar: .\Restaurar-Chat.ps1" -ForegroundColor Cyan
