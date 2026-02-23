using System;
using System.IO;
using System.Text.Json;

namespace PicPaste;

public class AppSettings
{
    // 保存路径
    public string SavePath { get; set; } = "";

    // 开机自启动
    public bool AutoStart { get; set; } = true;

    // 处理模式：true = 仅在终端粘贴时处理, false = 截图后立即处理
    public bool ProcessOnPasteOnly { get; set; } = true;

    // 最大文件数量（0 = 无限制）
    public int MaxFiles { get; set; } = 0;

    // 文件保存时长（小时，0 = 永久保留）
    public int FileRetentionHours { get; set; } = 0;

    // 自动清理间隔（分钟，0 = 不自动清理）
    public int CleanupIntervalMinutes { get; set; } = 0;

    // 处理延迟（毫秒）
    public int ProcessingDelayMs { get; set; } = 500;

    // 退出时清理所有截图文件
    public bool CleanupOnExit { get; set; } = true;

    // 关机时清理所有截图文件
    public bool CleanupOnShutdown { get; set; } = true;

    // 显示托盘通知
    public bool ShowTrayNotifications { get; set; } = true;

    // 日志记录
    public bool EnableLogging { get; set; } = true;

    // 更新源（GitHub 或 Gitee）
    public string UpdateSource { get; set; } = "Gitee";

    // 启动时自动检查更新
    public bool AutoCheckUpdate { get; set; } = true;

    // 跳过的版本（用户选择暂不更新的版本）
    public string SkippedVersion { get; set; } = "";

    public AppSettings()
    {
        // 默认保存路径：桌面/临时截图
        if (string.IsNullOrEmpty(SavePath))
        {
            SavePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "临时截图"
            );
        }
    }
}

public static class SettingsManager
{
    private static readonly string SettingsPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "settings.json"
    );

    private static AppSettings? _current;

    public static AppSettings Current
    {
        get
        {
            if (_current == null)
            {
                Load();
            }
            return _current!;
        }
    }

    public static void Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                _current = JsonSerializer.Deserialize<AppSettings>(json);
            }
        }
        catch (Exception)
        {
            // 加载失败时使用默认设置
        }

        if (_current == null)
        {
            _current = new AppSettings();
            Save();
        }

        // 确保保存路径存在
        EnsureSavePathExists();
    }

    public static void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_current, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(SettingsPath, json);

            // 设置文件为隐藏
            var fileInfo = new FileInfo(SettingsPath);
            if (fileInfo.Exists)
            {
                fileInfo.Attributes |= FileAttributes.Hidden;
            }
        }
        catch (Exception)
        {
            // 保存失败时忽略
        }
    }

    public static void EnsureSavePathExists()
    {
        if (!string.IsNullOrEmpty(_current?.SavePath))
        {
            try
            {
                if (!Directory.Exists(_current.SavePath))
                {
                    Directory.CreateDirectory(_current.SavePath);
                }
            }
            catch (Exception)
            {
                // 创建失败时使用默认路径
                _current.SavePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "临时截图"
                );
                Directory.CreateDirectory(_current.SavePath);
            }
        }
    }
}
