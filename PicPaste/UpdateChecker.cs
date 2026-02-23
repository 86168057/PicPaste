using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace PicPaste
{
    public class UpdateInfo
    {
        public string Version { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string ReleaseNotes { get; set; } = "";
        public string ReleaseDate { get; set; } = "";
        public bool IsMandatory { get; set; } = false;
    }

    public static class UpdateChecker
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        public static async Task<UpdateInfo?> CheckUpdateAsync(string source)
        {
            try
            {
                HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("PicPaste-UpdateChecker");

                if (source == VersionInfo.UpdateSourceGitHub)
                {
                    return await CheckGitHubUpdateAsync();
                }
                else
                {
                    return await CheckGiteeUpdateAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检查更新失败: {ex.Message}");
                return null;
            }
        }

        private static async Task<UpdateInfo?> CheckGitHubUpdateAsync()
        {
            try
            {
                var url = "https://api.github.com/repos/86168057/PicPaste/releases/latest";
                var response = await HttpClient.GetStringAsync(url);
                var doc = JsonDocument.Parse(response);

                var latestVersion = doc.RootElement.GetProperty("tag_name").GetString()?.TrimStart('v') ?? "";
                var currentVersion = VersionInfo.Version;

                if (IsNewerVersion(latestVersion, currentVersion))
                {
                    var downloadUrl = "";
                    var assets = doc.RootElement.GetProperty("assets");
                    foreach (var asset in assets.EnumerateArray())
                    {
                        var name = asset.GetProperty("name").GetString() ?? "";
                        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        {
                            downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "";
                            break;
                        }
                    }

                    return new UpdateInfo
                    {
                        Version = latestVersion,
                        DownloadUrl = downloadUrl,
                        ReleaseNotes = doc.RootElement.GetProperty("body").GetString() ?? "",
                        ReleaseDate = doc.RootElement.GetProperty("published_at").GetString() ?? ""
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GitHub检查更新失败: {ex.Message}");
                return null;
            }
        }

        private static async Task<UpdateInfo?> CheckGiteeUpdateAsync()
        {
            try
            {
                var url = "https://gitee.com/api/v5/repos/lfsnd/PicPaste/releases/latest";
                var response = await HttpClient.GetStringAsync(url);
                var doc = JsonDocument.Parse(response);

                var latestVersion = doc.RootElement.GetProperty("tag_name").GetString()?.TrimStart('v') ?? "";
                var currentVersion = VersionInfo.Version;

                if (IsNewerVersion(latestVersion, currentVersion))
                {
                    var downloadUrl = "";
                    if (doc.RootElement.TryGetProperty("assets", out var assets))
                    {
                        foreach (var asset in assets.EnumerateArray())
                        {
                            var name = asset.GetProperty("name").GetString() ?? "";
                            if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                            {
                                downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "";
                                break;
                            }
                        }
                    }

                    return new UpdateInfo
                    {
                        Version = latestVersion,
                        DownloadUrl = downloadUrl,
                        ReleaseNotes = doc.RootElement.GetProperty("body").GetString() ?? "",
                        ReleaseDate = doc.RootElement.GetProperty("created_at").GetString() ?? ""
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Gitee检查更新失败: {ex.Message}");
                return null;
            }
        }

        private static bool IsNewerVersion(string latest, string current)
        {
            try
            {
                var latestParts = latest.Split('.');
                var currentParts = current.Split('.');

                for (int i = 0; i < Math.Max(latestParts.Length, currentParts.Length); i++)
                {
                    int latestNum = i < latestParts.Length && int.TryParse(latestParts[i], out var ln) ? ln : 0;
                    int currentNum = i < currentParts.Length && int.TryParse(currentParts[i], out var cn) ? cn : 0;

                    if (latestNum > currentNum) return true;
                    if (latestNum < currentNum) return false;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public static async Task DownloadAndInstallUpdateAsync(UpdateInfo updateInfo, IProgress<double> progress)
        {
            try
            {
                var tempPath = Path.Combine(Path.GetTempPath(), $"PicPaste_Update_{updateInfo.Version}.exe");

                using (var response = await HttpClient.GetAsync(updateInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;

                    await using (var contentStream = await response.Content.ReadAsStreamAsync())
                    await using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[8192];
                        long totalRead = 0;
                        int read;

                        while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, read);
                            totalRead += read;

                            if (totalBytes > 0)
                            {
                                progress.Report((double)totalRead / totalBytes * 100);
                            }
                        }
                    }
                }

                progress.Report(100);

                var currentExePath = Assembly.GetExecutingAssembly().Location;
                var currentDir = Path.GetDirectoryName(currentExePath);
                var backupPath = Path.Combine(currentDir ?? "", $"PicPaste_backup_{DateTime.Now:yyyyMMddHHmmss}.exe");

                File.Move(currentExePath, backupPath);
                File.Move(tempPath, currentExePath);

                Process.Start(currentExePath);
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                throw new Exception($"更新失败: {ex.Message}");
            }
        }
    }
}
