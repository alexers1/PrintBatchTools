using Microsoft.UI.Xaml;
using System;
using System.IO;
using Windows.Storage;
using PrintBatchTool.Models;
using PrintBatchTool.Helpers; // 引入 Helper 命名空间 🧭
using SQLite;

namespace PrintBatchTool
{
    public partial class App : Application
    {
        // 全局暴露主窗口实例
        public static Window MainWindow { get; private set; }

        // 全局暴露 SQLite 持久化账本连接
        public static SQLiteConnection Database { get; private set; }

        public App()
        {
            this.InitializeComponent();

            // 🛡️ 核心改进：单实例检测
            // 如果发现已经有一个打印助手在运行，直接“劝退”当前进程，防止数据库冲突
            if (!ProcessLockHelper.EnsureSingleInstance())
            {
                System.Diagnostics.Process.GetCurrentProcess().Kill();
                return;
            }
        }

        /// <summary>
        /// 应用程序启动入口
        /// </summary>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // 1. 优先建立数据库连接，准备好“账本” 🗄️
            InitializeDatabase();

            // 2. 唤醒主界面
            MainWindow = new MainWindow();
            MainWindow.Activate();
        }

        private void InitializeDatabase()
        {
            try
            {
                // 🧭 核心改进：通过 PathHelper 统一获取沙盒路径，避免硬编码
                string dbPath = PathHelper.GetDatabasePath();

                // 建立连接，如果文件不存在会自动在 AppData\Local 创建
                Database = new SQLiteConnection(dbPath);

                // 🚀 开启 WAL 模式：支持多线程并发读写，非常适合我们的异步打印场景
                Database.EnableWriteAheadLogging();

                // 自动检查并创建任务表
                Database.CreateTable<PrintTaskRecord>();
            }
            catch (Exception ex)
            {
                // 在调试输出中打印异常，方便定位权限或磁盘问题
                System.Diagnostics.Debug.WriteLine($"[FATAL] 数据库初始化彻底失败: {ex.Message}");
            }
        }
    }
}