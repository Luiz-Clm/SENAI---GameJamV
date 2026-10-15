@echo off
title Gerador de Audio - SpellGlide
cd /d "%~dp0"
echo ===================================================
echo   Gerando Efeitos Sonoros: Mini Fogo 1 e Gelo 2
echo ===================================================
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0gerar_audios.ps1"
echo.
echo Pressione qualquer tecla para sair...
pause >nul
