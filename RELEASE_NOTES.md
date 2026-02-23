# PicPaste v1.0.0 Release Notes

## Version Info
- **Version**: v1.0.0
- **Release Date**: 2026-02-23
- **Developer**: 潇洒公子

## Based On
This project is based on [clipboard-image-watcher](https://github.com/citizenll/clipboard-image-watcher) by [citizenll](https://github.com/citizenll).

## Features

### Core Features
- Smart clipboard monitoring for screenshots
- Save images only when pasting in terminal (not immediately on screenshot)
- Auto-replace clipboard with file path for easy terminal use
- Customizable save path

### Cache Management
- Max file count limit (0 = unlimited)
- File retention time (0 = permanent)
- Auto cleanup interval (0 = disabled)
- Cleanup on exit/shutdown

### Update System
- Manual update check
- Auto check on startup (can be disabled)
- Dual source: GitHub and Gitee
- Auto download and install

### Other Features
- Auto-start on Windows boot
- Log viewer
- Tray icon support
- Modern settings UI

## Download Sources

### GitHub (International)
https://github.com/86168057/PicPaste/releases
- For users with proxy or overseas network

### Gitee (Recommended for China)
https://gitee.com/lfsnd/PicPaste/releases
- Domestic server, faster download without proxy

## System Requirements
- Windows 10/11 64-bit
- No .NET Runtime required (self-contained)

## How to Use
1. Run PicPaste.exe
2. Take a screenshot, then press Ctrl+V in terminal to paste the image path
3. Right-click tray icon to open settings
