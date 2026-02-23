using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace PicPaste
{
    public partial class UpdateWindow : Window
    {
        private UpdateInfo? _updateInfo;
        private readonly bool _isAutoCheck;

        public UpdateWindow(bool isAutoCheck = false)
        {
            InitializeComponent();
            _isAutoCheck = isAutoCheck;
            TxtCurrentVersion.Text = $"当前版本: {VersionInfo.VersionName}";

            if (isAutoCheck)
            {
                Loaded += async (s, e) => await AutoCheckUpdateAsync();
            }
        }

        private async Task AutoCheckUpdateAsync()
        {
            BtnCheckUpdate.IsEnabled = false;
            TxtStatus.Text = "正在检查更新...";

            var source = SettingsManager.Current.UpdateSource;
            if (string.IsNullOrEmpty(source))
            {
                source = VersionInfo.UpdateSourceGitee;
            }

            RbGitHub.IsChecked = source == VersionInfo.UpdateSourceGitHub;
            RbGitee.IsChecked = source == VersionInfo.UpdateSourceGitee;

            await PerformCheckUpdateAsync(source);

            BtnCheckUpdate.IsEnabled = true;
        }

        private async void OnCheckUpdate(object sender, RoutedEventArgs e)
        {
            BtnCheckUpdate.IsEnabled = false;
            TxtStatus.Text = "正在检查更新...";
            UpdateInfoPanel.Visibility = Visibility.Collapsed;
            ActionButtons.Visibility = Visibility.Collapsed;

            var source = RbGitHub.IsChecked == true ? VersionInfo.UpdateSourceGitHub : VersionInfo.UpdateSourceGitee;
            await PerformCheckUpdateAsync(source);

            BtnCheckUpdate.IsEnabled = true;
        }

        private async Task PerformCheckUpdateAsync(string source)
        {
            try
            {
                _updateInfo = await UpdateChecker.CheckUpdateAsync(source);

                if (_updateInfo != null)
                {
                    TxtNewVersion.Text = $"发现新版本: v{_updateInfo.Version}";
                    TxtReleaseDate.Text = $"发布日期: {_updateInfo.ReleaseDate:yyyy-MM-dd}";
                    TxtReleaseNotes.Text = string.IsNullOrEmpty(_updateInfo.ReleaseNotes) 
                        ? "暂无更新说明" 
                        : _updateInfo.ReleaseNotes;
                    UpdateInfoPanel.Visibility = Visibility.Visible;
                    ActionButtons.Visibility = Visibility.Visible;
                    TxtStatus.Text = "";
                }
                else
                {
                    TxtStatus.Text = "当前已是最新版本";
                    if (_isAutoCheck)
                    {
                        await Task.Delay(1500);
                        Close();
                    }
                }
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"检查更新失败: {ex.Message}";
            }
        }

        private async void OnDownloadUpdate(object sender, RoutedEventArgs e)
        {
            if (_updateInfo == null) return;

            ActionButtons.Visibility = Visibility.Collapsed;
            ProgressPanel.Visibility = Visibility.Visible;
            TxtStatus.Text = "正在下载更新...";

            try
            {
                var progress = new Progress<double>(value =>
                {
                    ProgressBar.Value = value;
                    TxtProgress.Text = $"{value:F1}%";
                });

                await UpdateChecker.DownloadAndInstallUpdateAsync(_updateInfo, progress);
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"下载失败: {ex.Message}";
                ProgressPanel.Visibility = Visibility.Collapsed;
                ActionButtons.Visibility = Visibility.Visible;
            }
        }

        private void OnRemindLater(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
