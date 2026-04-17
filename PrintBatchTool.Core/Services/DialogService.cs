using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace PrintBatchTool.Services
{
    public static class DialogService
    {
        /// <summary>
        /// 启动时检测到残余任务，弹出断点续传询问框
        /// </summary>
        public static async Task<bool> ShowRecoveryPromptAsync(int taskCount)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = "⚠️ 发现未完成的打印任务",
                Content = $"检测到上次异常退出前，数据库中还有 {taskCount} 个未完成的打印任务。\n\n是否继续打印？",
                PrimaryButtonText = "恢复打印",
                CloseButtonText = "清除任务",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = App.MainWindow.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }

        /// <summary>
        /// 显示带滚动条的详细错误信息对话框
        /// </summary>
        public static async Task ShowErrorDetailsAsync(string title, string detailedError)
        {
            // 构建一个自带滚动条的只读多行文本框
            var textBox = new TextBox
            {
                Text = detailedError,
                IsReadOnly = true,
                TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                AcceptsReturn = true,
                MaxHeight = 250 // 限制最大高度，避免对话框失控
            };

            var scrollViewer = new ScrollViewer
            {
                Content = textBox,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            ContentDialog dialog = new ContentDialog
            {
                Title = "❌ " + title,
                Content = scrollViewer,
                CloseButtonText = "确定",
                XamlRoot = App.MainWindow.Content.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }
}