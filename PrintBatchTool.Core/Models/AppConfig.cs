using Windows.Storage;

namespace PrintBatchTool.Models
{
    public static class AppConfig
    {
        // 获取 WinUI 3 原生的本地设置容器
        private static readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;

        /// <summary>
        /// 记录用户上一次选择的打印机名称
        /// </summary>
        public static string LastSelectedPrinter
        {
            get => _localSettings.Values["LastSelectedPrinter"] as string;
            set => _localSettings.Values["LastSelectedPrinter"] = value;
        }

        /// <summary>
        /// 记录用户是否开启了深色模式
        /// </summary>
        public static bool IsDarkMode
        {
            get => _localSettings.Values["IsDarkMode"] as bool? ?? false;
            set => _localSettings.Values["IsDarkMode"] = value;
        }

        /// <summary>
        /// 记录用户上一次扫描的文件夹路径，方便下次直接打开
        /// </summary>
        public static string LastOpenedFolder
        {
            get => _localSettings.Values["LastOpenedFolder"] as string;
            set => _localSettings.Values["LastOpenedFolder"] = value;
        }
    }
}