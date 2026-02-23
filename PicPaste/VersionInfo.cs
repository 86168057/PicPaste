namespace PicPaste
{
    public static class VersionInfo
    {
        public const string Version = "1.0.0";
        public const string VersionName = "v1.0.0";
        public const string ReleaseDate = "2026-02-23";
        public const string UpdateSourceGitHub = "GitHub";
        public const string UpdateSourceGitee = "Gitee";

        public static string GetFullVersionInfo()
        {
            return $"PicPaste {VersionName} - 智能剪贴板图片处理工具";
        }
    }
}
