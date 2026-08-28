# Deploy a framework-dependent build from the dev machine (Windows) to the device and run it.
# The one-way road: publish -> transfer -> run (book chapter 3).
# Framework-dependent keeps the transfer small; install .NET on the device once (see README).
#
# Example:
#   ./deploy/deploy.ps1 -PiHost pi@raspberrypi
param(
    [string]$PiHost = "pi@raspberrypi",
    [string]$Dest   = "/home/pi/rpiwatcher"
)

$ErrorActionPreference = "Stop"

# In PowerShell, backtick line-continuation is unreliable, so keep each command on one line.

# 1) Publish (framework-dependent -> small, fast transfer)
dotnet publish RpiWatcher/RpiWatcher.csproj -c Release -o ./publish

# 2) Transfer (copies the whole folder, so no mkdir; the folder is created on first run)
scp -r ./publish "${PiHost}:${Dest}"

# 3) Run (device .NET installed under ~/.dotnet)
ssh $PiHost "~/.dotnet/dotnet $Dest/RpiWatcher.dll"

# Self-contained alternative (no .NET needed on the device, but the transfer is heavy):
#   dotnet publish RpiWatcher/RpiWatcher.csproj -c Release -r linux-arm64 --self-contained -o ./publish
#   ssh $PiHost "chmod +x $Dest/RpiWatcher; $Dest/RpiWatcher"
#   # repeat deploys: rsync -az --delete ./publish/ ${PiHost}:${Dest}/   (from Git Bash / WSL)
