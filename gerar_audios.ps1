$ErrorActionPreference = "Stop"

function Create-Wav($type, $outputPath) {
    $sampleRate = 44100
    if ($type -eq "fire") {
        $duration = 0.18
        $totalSamples = [int]($sampleRate * $duration)
        $samples = New-Object float[] $totalSamples
        $rng = New-Object System.Random(101)
        for ($i = 0; $i -lt $totalSamples; $i++) {
            $t = [float]$i / $sampleRate
            $progress = $t / $duration
            $sparkSnap = ([float]($rng.NextDouble() * 2.0 - 1.0)) * [Math]::Exp(-60.0 * $t) * 0.7
            $flameWhistleFreq = 980.0 * [Math]::Exp(-12.0 * $t) + 320.0
            $flameWhistle = [Math]::Sin(2.0 * [Math]::PI * $flameWhistleFreq * $t) * [Math]::Exp(-16.0 * $t) * 0.55
            $sizzle = ([float]($rng.NextDouble() * 2.0 - 1.0)) * [Math]::Sin(2.0 * [Math]::PI * $flameWhistleFreq * $t) * [Math]::Exp(-22.0 * $t) * 0.35
            $punch = [Math]::Sin(2.0 * [Math]::PI * 140.0 * $t) * [Math]::Exp(-40.0 * $t) * 0.3
            $raw = $sparkSnap + $flameWhistle + $sizzle + $punch
            $samples[$i] = [Math]::Tanh($raw * 1.7) * 0.9
        }
    } else {
        $duration = 0.19
        $totalSamples = [int]($sampleRate * $duration)
        $samples = New-Object float[] $totalSamples
        $rng = New-Object System.Random(202)
        for ($i = 0; $i -lt $totalSamples; $i++) {
            $t = [float]$i / $sampleRate
            $progress = $t / $duration
            $crack = ([float]($rng.NextDouble() * 2.0 - 1.0)) * [Math]::Exp(-55.0 * $t) * 0.75
            $whistleFreq = 1800.0 * (1.0 - $progress * 0.5)
            $whistle = [Math]::Sin(2.0 * [Math]::PI * $whistleFreq * $t) * [Math]::Exp(-15.0 * $t) * 0.5
            $ping = [Math]::Sin(2.0 * [Math]::PI * 3400.0 * $t) * [Math]::Exp(-35.0 * $t) * 0.35
            $samples[$i] = ($crack + $whistle + $ping) * 0.85
        }
    }

    $ms = New-Object System.IO.MemoryStream
    $bw = New-Object System.IO.BinaryWriter($ms)

    $channels = 1
    $subChunk2Size = $totalSamples * $channels * 2
    $chunkSize = 36 + $subChunk2Size

    $bw.Write([System.Text.Encoding]::ASCII.GetBytes("RIFF"))
    $bw.Write([int]$chunkSize)
    $bw.Write([System.Text.Encoding]::ASCII.GetBytes("WAVE"))
    $bw.Write([System.Text.Encoding]::ASCII.GetBytes("fmt "))
    $bw.Write([int]16)
    $bw.Write([int16]1)
    $bw.Write([int16]$channels)
    $bw.Write([int]$sampleRate)
    $bw.Write([int]($sampleRate * $channels * 2))
    $bw.Write([int16]($channels * 2))
    $bw.Write([int16]16)
    $bw.Write([System.Text.Encoding]::ASCII.GetBytes("data"))
    $bw.Write([int]$subChunk2Size)

    for ($i = 0; $i -lt $totalSamples; $i++) {
        $val = [Math]::Max(-1.0, [Math]::Min(1.0, $samples[$i]))
        $shortVal = [int16]($val * 32767.0)
        $bw.Write($shortVal)
    }

    $dir = [System.IO.Path]::GetDirectoryName($outputPath)
    if (-not [System.IO.Directory]::Exists($dir)) {
        [System.IO.Directory]::CreateDirectory($dir) | Out-Null
    }

    [System.IO.File]::WriteAllBytes($outputPath, $ms.ToArray())
    $bw.Close()
    $ms.Close()

    Write-Host "[OK] Gerado com sucesso: $outputPath" -ForegroundColor Green
}

$scriptDir = $PSScriptRoot
$audioDir = Join-Path $scriptDir "Assets\Audio"
$downloadsDir = [System.IO.Path]::Combine($env:USERPROFILE, "Downloads")

Write-Host "=== Gerando Efeitos Sonoros: Mini Fogo 1 e Gelo 2 ===" -ForegroundColor Cyan

# 1. Salva na pasta Assets\Audio do projeto
Create-Wav "fire" (Join-Path $audioDir "PlayerFireShot.wav")
Create-Wav "ice" (Join-Path $audioDir "PlayerIceShot.wav")

# 2. Salva na pasta Downloads do computador
Create-Wav "fire" (Join-Path $downloadsDir "PlayerFire_DardoChamas.wav")
Create-Wav "ice" (Join-Path $downloadsDir "PlayerIce_LancaCongelada.wav")

Write-Host "`nArquivos instalados na pasta Assets\Audio e na sua pasta de Downloads!" -ForegroundColor Yellow
