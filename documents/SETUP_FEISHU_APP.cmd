@echo off
"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoLogo -NoProfile -File "C:\Users\tangx\.codex\integrations\feishu\Setup-FeishuApp.ps1"
if errorlevel 1 pause
