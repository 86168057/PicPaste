using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Timer = System.Timers.Timer;
using Drawing = System.Drawing;

namespace PicPaste;

public partial class MainWindow : Window
{
    private const string FilePrefix = "capture_";

    private NotifyIcon? _notifyIcon;
    private ClipboardMonitor? _clipboardMonitor;
    private Timer? _cleanupTimer;

    private DateTime _lastScreenshotTime = DateTime.MinValue;

    private static string LogFilePath => Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "log.txt"
    );

    public MainWindow()
    {
        Log("PicPaste 启动中...");

        try
        {
            // 加载设置
            SettingsManager.Load();

            // 确保保存路径存在
            SettingsManager.EnsureSavePathExists();

            // 应用开机自启动设置
            if (SettingsManager.Current.AutoStart)
            {
                AutoStartManager.SetAutoStart(true);
            }

            InitializeComponent();
            InitializeTrayIcon();
            InitializeClipboardMonitor();
            InitializeCleanupTimer();
            InitializeShutdownHandler();

            Log("PicPaste 启动成功");
        }
        catch (Exception ex)
        {
            Log($"启动错误: {ex.Message}");
            System.Windows.MessageBox.Show($"启动失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            System.Windows.Application.Current.Shutdown();
        }
    }

    private void InitializeShutdownHandler()
    {
        // 监听系统关机事件
        SystemEvents.SessionEnding += (s, e) =>
        {
            if (e.Reason == SessionEndReasons.SystemShutdown)
            {
                Log("检测到系统关机，执行清理...");
                if (SettingsManager.Current.CleanupOnShutdown)
                {
                    CleanupAllImagesOnShutdown();
                }
            }
        };
    }

    private void CleanupAllImagesOnShutdown()
    {
        try
        {
            var files = GetCapturedFiles();
            foreach (var file in files)
            {
                try
                {
                    File.Delete(file.FullName);
                }
                catch { }
            }
            Log("关机时所有截图文件已清理");
        }
        catch { }
    }

    private void Log(string message)
    {
        if (!SettingsManager.Current.EnableLogging) return;

        try
        {
            File.AppendAllText(LogFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
        }
        catch { }
    }

    private void InitializeTrayIcon()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = LoadApplicationIcon(),
            Visible = true,
            Text = "PicPaste - 智能剪贴板图片处理"
        };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("设置...", null, OnSettings);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("退出", null, OnExit);

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += OnSettings;
    }

    private Drawing.Icon LoadApplicationIcon()
    {
        try
        {
            // 从嵌入资源加载
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();

            // 查找 PNG 资源
            var pngResource = resourceNames.FirstOrDefault(r => r.EndsWith("123.png"));
            if (pngResource != null)
            {
                using var stream = assembly.GetManifestResourceStream(pngResource);
                if (stream != null)
                {
                    using var bitmap = new Drawing.Bitmap(stream);
                    using var resized = new Drawing.Bitmap(bitmap, new Drawing.Size(32, 32));
                    var hIcon = resized.GetHicon();
                    return Drawing.Icon.FromHandle(hIcon);
                }
            }
        }
        catch (Exception ex)
        {
            Log($"加载图标失败: {ex.Message}");
        }

        return SystemIcons.Application;
    }

    private void InitializeClipboardMonitor()
    {
        _clipboardMonitor = new ClipboardMonitor();
        _clipboardMonitor.ClipboardChanged += OnClipboardChanged;
    }

    private void InitializeCleanupTimer()
    {
        var intervalMinutes = SettingsManager.Current.CleanupIntervalMinutes;

        // 如果设置为0，表示不自动清理，不启动定时器
        if (intervalMinutes <= 0)
        {
            Log("自动清理已禁用");
            return;
        }

        _cleanupTimer = new Timer(TimeSpan.FromMinutes(intervalMinutes).TotalMilliseconds);
        _cleanupTimer.Elapsed += (s, e) => CleanupOldFiles();
        _cleanupTimer.AutoReset = true;
        _cleanupTimer.Start();

        // 初始清理
        CleanupOldFiles();
    }

    private void OnClipboardChanged(object? sender, EventArgs e)
    {
        // 检查是否是内存图片
        if (System.Windows.Clipboard.ContainsImage() && IsMemoryImage())
        {
            var image = System.Windows.Clipboard.GetImage();
            if (image != null)
            {
                Log($"检测到内存图片: {image.PixelWidth}x{image.PixelHeight}");
                _lastScreenshotTime = DateTime.Now;

                // 原项目逻辑：立即处理图片
                SaveAndReplaceClipboard(image);
            }
        }
    }

    private void SaveAndReplaceClipboard(BitmapSource image)
    {
        try
        {
            CleanupAndMakeSpace();

            var filePath = Path.Combine(
                SettingsManager.Current.SavePath,
                $"{FilePrefix}{DateTime.Now:yyyyMMddHHmmssfff}.png"
            );

            SaveImage(image, filePath);
            ReplaceClipboardWithFile(filePath);

            UpdateTrayTooltip($"最后捕获: {DateTime.Now:HH:mm:ss}");
            Log($"图片已保存: {filePath}");
        }
        catch (Exception ex)
        {
            Log($"保存图片失败: {ex.Message}");
        }
    }

    private bool IsMemoryImage()
    {
        try
        {
            // 检查是否是文件拖拽
            if (System.Windows.Clipboard.ContainsFileDropList())
            {
                return false;
            }

            // 检查文件相关的剪贴板格式
            var dataObject = System.Windows.Clipboard.GetDataObject();
            if (dataObject != null)
            {
                string[] fileFormats = { "FileName", "FileNameW", "Shell IDList Array" };
                foreach (var format in fileFormats)
                {
                    if (dataObject.GetDataPresent(format))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        catch
        {
            return true;
        }
    }

    private void SaveImage(BitmapSource image, string filePath)
    {
        // 方法1: 尝试直接从剪贴板数据保存
        if (TrySaveFromClipboardData(filePath))
            return;

        // 方法2: 尝试替代剪贴板访问
        if (TryAlternativeClipboardAccess(filePath))
            return;

        // 方法3: 标准 PNG 编码
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        encoder.Save(stream);
    }

    private bool TrySaveFromClipboardData(string filePath)
    {
        try
        {
            var dataObject = System.Windows.Clipboard.GetDataObject();
            if (dataObject == null) return false;

            // 尝试 PNG 格式
            if (dataObject.GetDataPresent("PNG"))
            {
                var pngData = dataObject.GetData("PNG") as Stream;
                if (pngData != null)
                {
                    using var fileStream = new FileStream(filePath, FileMode.Create);
                    pngData.CopyTo(fileStream);
                    return true;
                }
            }

            // 尝试 DIB 格式
            if (dataObject.GetDataPresent("DeviceIndependentBitmap"))
            {
                var dibData = dataObject.GetData("DeviceIndependentBitmap");
                if (dibData is Stream stream)
                {
                    using var bitmap = new Drawing.Bitmap(stream);
                    bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private bool TryAlternativeClipboardAccess(string filePath)
    {
        try
        {
            var dataObject = System.Windows.Clipboard.GetDataObject();
            if (dataObject == null) return false;

            // 尝试 System.Drawing.Bitmap 格式
            if (dataObject.GetDataPresent("System.Drawing.Bitmap"))
            {
                var drawingBitmap = dataObject.GetData("System.Drawing.Bitmap") as Drawing.Bitmap;
                if (drawingBitmap != null)
                {
                    drawingBitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private void ReplaceClipboardWithFile(string filePath)
    {
        try
        {
            var fileList = new System.Collections.Specialized.StringCollection();
            fileList.Add(filePath);

            System.Windows.Clipboard.Clear();
            System.Windows.Clipboard.SetFileDropList(fileList);
        }
        catch (Exception ex)
        {
            Log($"替换剪贴板失败: {ex.Message}");
        }
    }

    private void CleanupAndMakeSpace()
    {
        var files = GetCapturedFiles();
        var maxFiles = SettingsManager.Current.MaxFiles;

        // 如果 maxFiles <= 0，表示无限制，不删除旧文件
        if (maxFiles <= 0) return;

        while (files.Count() >= maxFiles)
        {
            var oldestFile = files.First();
            try
            {
                File.Delete(oldestFile.FullName);
                Log($"删除旧文件: {oldestFile.Name}");
            }
            catch (Exception ex)
            {
                Log($"删除文件失败: {ex.Message}");
                break;
            }
            files = GetCapturedFiles();
        }
    }

    private void CleanupOldFiles()
    {
        var files = GetCapturedFiles();
        var retentionHours = SettingsManager.Current.FileRetentionHours;

        // 如果 retentionHours <= 0，表示永久保留，不删除
        if (retentionHours <= 0) return;

        var cutoffTime = DateTime.Now.AddHours(-retentionHours);

        foreach (var file in files)
        {
            if (file.CreationTime < cutoffTime)
            {
                try
                {
                    File.Delete(file.FullName);
                    Log($"清理过期文件: {file.Name}");
                }
                catch (Exception ex)
                {
                    Log($"清理文件失败: {ex.Message}");
                }
            }
        }
    }

    private void CleanupAllCachedImages()
    {
        if (!SettingsManager.Current.CleanupOnExit) return;

        try
        {
            var files = GetCapturedFiles();
            foreach (var file in files)
            {
                try
                {
                    File.Delete(file.FullName);
                }
                catch { }
            }
            Log("退出时所有截图文件已清理");
        }
        catch { }
    }

    private IEnumerable<FileInfo> GetCapturedFiles()
    {
        var savePath = SettingsManager.Current.SavePath;
        if (!Directory.Exists(savePath))
            return Enumerable.Empty<FileInfo>();

        var directory = new DirectoryInfo(savePath);
        return directory.GetFiles($"{FilePrefix}*.png")
                        .OrderBy(f => f.CreationTime);
    }

    private void UpdateTrayTooltip(string message)
    {
        try
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Text = $"PicPaste - {message}";

                // 3秒后恢复默认提示
                var resetTimer = new Timer(3000);
                resetTimer.Elapsed += (s, e) =>
                {
                    _notifyIcon.Text = "PicPaste - 智能剪贴板图片处理";
                    resetTimer.Dispose();
                };
                resetTimer.AutoReset = false;
                resetTimer.Start();
            }
        }
        catch { }
    }

    private void OnSettings(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.SettingsSaved += OnSettingsSaved;
            settingsWindow.ShowDialog();
        });
    }

    private void OnSettingsSaved(object? sender, EventArgs e)
    {
        // 重新初始化清理定时器
        _cleanupTimer?.Stop();
        _cleanupTimer?.Dispose();
        InitializeCleanupTimer();

        Log("设置已更新");
    }

    private void OnExit(object? sender, EventArgs e)
    {
        CleanupAllCachedImages();

        _cleanupTimer?.Stop();
        _cleanupTimer?.Dispose();
        _clipboardMonitor?.Dispose();
        _notifyIcon?.Dispose();

        System.Windows.Application.Current.Shutdown();
    }

    protected override void OnClosed(EventArgs e)
    {
        CleanupAllCachedImages();

        _cleanupTimer?.Stop();
        _cleanupTimer?.Dispose();
        _clipboardMonitor?.Dispose();
        _notifyIcon?.Dispose();

        base.OnClosed(e);
    }
}
