using Microsoft.Win32;
using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace PicPaste;

public static class AutoStartManager
{
    private const string RegistryKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
    private const string AppName = "PicPaste";

    public static bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, false);
            if (key != null)
            {
                var value = key.GetValue(AppName);
                return value != null;
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"检查开机自启动状态时出错：{ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        return false;
    }

    public static void SetAutoStart(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
            if (key == null) return;

            if (enable)
            {
                var exePath = GetExecutablePath();
                key.SetValue(AppName, exePath);
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"设置开机自启动时出错：{ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private static string GetExecutablePath()
    {
        // 获取当前程序的路径
        var assembly = Assembly.GetExecutingAssembly();
        var location = assembly.Location;

        // 如果是单文件发布，使用进程路径
        if (string.IsNullOrEmpty(location) || location.EndsWith(".dll"))
        {
            location = Process.GetCurrentProcess().MainModule?.FileName ?? "";
        }

        return location;
    }
}
