using System.IO;
using Windows.Storage;

namespace PrintBatchTool.Helpers
{
    public static class PathHelper
    {
        /// <summary>
        /// 获取 SQLite 数据库的绝对存放路径
        /// </summary>
        public static string GetDatabasePath()
        {
            // 统一获取 AppData\Local 沙盒路径
            string localAppDataPath = ApplicationData.Current.LocalFolder.Path;

            // 拼接并返回最终的 .db 文件路径
            return Path.Combine(localAppDataPath, "printing.db");
        }

        /// <summary>
        /// 未来如果你想存一些错误日志，可以在这里扩展日志路径
        /// </summary>
        public static string GetLogFolderPath()
        {
            string localAppDataPath = ApplicationData.Current.LocalFolder.Path;
            string logDir = Path.Combine(localAppDataPath, "Logs");

            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
            return logDir;
        }
    }
}