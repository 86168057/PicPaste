# PicPaste v1.0.0 发布说明

## 版本信息
- **版本号**: v1.0.0
- **发布日期**: 2026-02-23
- **开发者**: 潇洒公子

## 原项目
本项目基于 [clipboard-image-watcher](https://github.com/citizenll/clipboard-image-watcher) 进行二次开发

## 主要功能

### 核心功能
- 智能剪贴板监控，仅在终端粘贴时保存图片
- 自动将图片替换为文件路径
- 自定义保存路径

### 缓存管理
- 最大文件数量限制（支持无限制）
- 文件保留时长设置（支持永久保留）
- 自动清理间隔（支持不自动清理）
- 退出/关机时自动清理截图文件

### 更新功能
- 支持手动和自动检查更新
- 双源更新：GitHub 和 Gitee
- 自动下载并安装更新

### 其他功能
- 开机自启动
- 日志查看功能
- 托盘图标支持
- 现代化设置界面

## 下载地址

### GitHub（国际）
https://github.com/86168057/PicPaste/releases
- 适合有代理或海外网络环境

### Gitee（国内推荐）
https://gitee.com/lfsnd/PicPaste/releases
- 服务器在国内，无需代理，下载速度更快

## 系统要求
- Windows 10/11 64位
- 无需安装 .NET Runtime（自包含单文件）

## 使用方法
1. 运行 PicPaste.exe
2. 截图后，在终端按 Ctrl+V 粘贴图片路径
3. 右键托盘图标打开设置面板
