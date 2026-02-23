using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WinForms = System.Windows.Forms;

namespace PicPaste;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        LoadSettings();
        SetupEventHandlers();
    }

    private void SetupEventHandlers()
    {
        // 无限制选项改变时禁用/启用输入框
        CbUnlimitedFiles.Checked += (s, e) =>
        {
            TxtMaxFiles.IsEnabled = false;
            TxtMaxFiles.Text = "∞";
        };
        CbUnlimitedFiles.Unchecked += (s, e) =>
        {
            TxtMaxFiles.IsEnabled = true;
            TxtMaxFiles.Text = "3";
        };

        CbUnlimitedRetention.Checked += (s, e) =>
        {
            TxtRetention.IsEnabled = false;
            TxtRetention.Text = "∞";
        };
        CbUnlimitedRetention.Unchecked += (s, e) =>
        {
            TxtRetention.IsEnabled = true;
            TxtRetention.Text = "1";
        };

        // 不自动清理选项
        CbNoAutoCleanup.Checked += (s, e) =>
        {
            TxtCleanupInterval.IsEnabled = false;
            TxtCleanupInterval.Text = "-";
        };
        CbNoAutoCleanup.Unchecked += (s, e) =>
        {
            TxtCleanupInterval.IsEnabled = true;
            TxtCleanupInterval.Text = "5";
        };

        // 只允许输入数字
        TxtMaxFiles.PreviewTextInput += (s, e) =>
        {
            e.Handled = !e.Text.All(char.IsDigit);
        };
        TxtRetention.PreviewTextInput += (s, e) =>
        {
            e.Handled = !e.Text.All(char.IsDigit);
        };
        TxtCleanupInterval.PreviewTextInput += (s, e) =>
        {
            e.Handled = !e.Text.All(char.IsDigit);
        };
    }

    private void LoadSettings()
    {
        var settings = SettingsManager.Current;

        // 保存路径
        TxtSavePath.Text = settings.SavePath;

        // 处理模式
        if (settings.ProcessOnPasteOnly)
        {
            RbProcessOnPaste.IsChecked = true;
        }
        else
        {
            RbProcessImmediate.IsChecked = true;
        }

        // 开机自启动
        CbAutoStart.IsChecked = settings.AutoStart;

        // 加载日志内容
        LoadLogContent();

        // 缓存管理 - 最大文件数量
        if (settings.MaxFiles <= 0)
        {
            CbUnlimitedFiles.IsChecked = true;
            TxtMaxFiles.IsEnabled = false;
            TxtMaxFiles.Text = "∞";
        }
        else
        {
            CbUnlimitedFiles.IsChecked = false;
            TxtMaxFiles.IsEnabled = true;
            TxtMaxFiles.Text = settings.MaxFiles.ToString();
        }

        // 缓存管理 - 保留时长
        if (settings.FileRetentionHours <= 0)
        {
            CbUnlimitedRetention.IsChecked = true;
            TxtRetention.IsEnabled = false;
            TxtRetention.Text = "∞";
        }
        else
        {
            CbUnlimitedRetention.IsChecked = false;
            TxtRetention.IsEnabled = true;
            TxtRetention.Text = settings.FileRetentionHours.ToString();
        }

        // 缓存管理 - 清理间隔
        if (settings.CleanupIntervalMinutes <= 0)
        {
            CbNoAutoCleanup.IsChecked = true;
            TxtCleanupInterval.IsEnabled = false;
            TxtCleanupInterval.Text = "-";
        }
        else
        {
            CbNoAutoCleanup.IsChecked = false;
            TxtCleanupInterval.IsEnabled = true;
            TxtCleanupInterval.Text = settings.CleanupIntervalMinutes.ToString();
        }

        CbCleanupOnExit.IsChecked = settings.CleanupOnExit;
        CbCleanupOnShutdown.IsChecked = settings.CleanupOnShutdown;

        // 其他设置
        CbShowNotifications.IsChecked = settings.ShowTrayNotifications;
    }

    private void LoadLogContent()
    {
        try
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");
            if (File.Exists(logPath))
            {
                // 读取最后 100 行日志
                var lines = File.ReadAllLines(logPath);
                var lastLines = lines.Length > 100 ? lines.Skip(lines.Length - 100) : lines;
                TxtLogContent.Text = string.Join(Environment.NewLine, lastLines);

                // 滚动到底部
                LogScrollViewer.ScrollToEnd();
            }
            else
            {
                TxtLogContent.Text = "暂无日志记录";
            }
        }
        catch (Exception ex)
        {
            TxtLogContent.Text = $"加载日志失败: {ex.Message}";
        }
    }

    private void OnRefreshLog(object sender, RoutedEventArgs e)
    {
        LoadLogContent();
    }

    private void OnClearLog(object sender, RoutedEventArgs e)
    {
        try
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");
            if (File.Exists(logPath))
            {
                File.WriteAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 日志已清空{Environment.NewLine}");
                LoadLogContent();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"清空日志失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnBrowsePath(object sender, RoutedEventArgs e)
    {
        using var dialog = new WinForms.FolderBrowserDialog
        {
            Description = "选择截图保存文件夹",
            SelectedPath = TxtSavePath.Text
        };

        if (dialog.ShowDialog() == WinForms.DialogResult.OK)
        {
            TxtSavePath.Text = dialog.SelectedPath;
        }
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        var settings = SettingsManager.Current;

        // 保存路径
        settings.SavePath = TxtSavePath.Text;

        // 处理模式
        settings.ProcessOnPasteOnly = RbProcessOnPaste.IsChecked == true;

        // 开机自启动
        settings.AutoStart = CbAutoStart.IsChecked == true;
        AutoStartManager.SetAutoStart(settings.AutoStart);

        // 缓存管理 - 最大文件数量
        if (CbUnlimitedFiles.IsChecked == true)
        {
            settings.MaxFiles = 0; // 0 表示无限制
        }
        else
        {
            if (int.TryParse(TxtMaxFiles.Text, out int maxFiles) && maxFiles > 0)
            {
                settings.MaxFiles = maxFiles;
            }
            else
            {
                System.Windows.MessageBox.Show("最大文件数量必须是大于0的数字", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        // 缓存管理 - 保留时长
        if (CbUnlimitedRetention.IsChecked == true)
        {
            settings.FileRetentionHours = 0; // 0 表示永久保留
        }
        else
        {
            if (int.TryParse(TxtRetention.Text, out int retention) && retention > 0)
            {
                settings.FileRetentionHours = retention;
            }
            else
            {
                System.Windows.MessageBox.Show("保留时长必须是大于0的数字", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        // 缓存管理 - 清理间隔
        if (CbNoAutoCleanup.IsChecked == true)
        {
            settings.CleanupIntervalMinutes = 0; // 0 表示不自动清理
        }
        else
        {
            if (int.TryParse(TxtCleanupInterval.Text, out int cleanupInterval) && cleanupInterval > 0)
            {
                settings.CleanupIntervalMinutes = cleanupInterval;
            }
            else
            {
                System.Windows.MessageBox.Show("清理间隔必须是大于0的数字", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        settings.CleanupOnExit = CbCleanupOnExit.IsChecked == true;
        settings.CleanupOnShutdown = CbCleanupOnShutdown.IsChecked == true;

        // 其他设置
        settings.ShowTrayNotifications = CbShowNotifications.IsChecked == true;

        // 确保保存路径存在
        SettingsManager.EnsureSavePathExists();

        // 保存设置
        SettingsManager.Save();

        // 通知主窗口设置已更改
        SettingsSaved?.Invoke(this, EventArgs.Empty);

        // 显示保存成功提示（不关闭窗口）
        System.Windows.MessageBox.Show("设置已保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnResetDefaults(object sender, RoutedEventArgs e)
    {
        if (System.Windows.MessageBox.Show("确定要恢复默认设置吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            // 更新UI为默认值
            TxtSavePath.Text = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "临时截图"
            );
            RbProcessOnPaste.IsChecked = true;
            CbAutoStart.IsChecked = true;

            // 缓存管理默认值 - 按照截图设置
            CbUnlimitedFiles.IsChecked = true;
            TxtMaxFiles.IsEnabled = false;
            TxtMaxFiles.Text = "∞";

            CbUnlimitedRetention.IsChecked = true;
            TxtRetention.IsEnabled = false;
            TxtRetention.Text = "∞";

            CbNoAutoCleanup.IsChecked = true;
            TxtCleanupInterval.IsEnabled = false;
            TxtCleanupInterval.Text = "-";

            CbCleanupOnExit.IsChecked = true;
            CbCleanupOnShutdown.IsChecked = true;
            CbShowNotifications.IsChecked = true;
        }
    }

    public event EventHandler? SettingsSaved;
}
