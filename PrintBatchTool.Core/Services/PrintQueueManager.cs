using PrintBatchTool.Models;
using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace PrintBatchTool.Services
{
    public static class PrintQueueManager
    {
        // 📦 创建一个无界 Channel 作为内存异步队列
        private static readonly Channel<PrintTaskRecord> _taskChannel = Channel.CreateUnbounded<PrintTaskRecord>();

        // 📡 暴露事件给 MainWindow，用于同步 UI 状态 (进度条、文本等)
        public static event Action<PrintTaskRecord, string> OnTaskStatusChanged;
        public static event Action OnAllTasksCompleted;

        private static bool _isConsumerRunning = false;

        /// <summary>
        /// 🚀 启动后台消费者长任务 (Long-running Task)
        /// </summary>
        public static void StartConsumer()
        {
            if (_isConsumerRunning) return;
            _isConsumerRunning = true;

            Task.Run(async () =>
            {
                // ⏳ 持续监听 Channel，只要里面有任务就拿出来执行
                await foreach (var task in _taskChannel.Reader.ReadAllAsync())
                {
                    OnTaskStatusChanged?.Invoke(task, $"正在发送到 {task.PrinterName}...");

                    // 🖨️ 调用物理打印驱动
                    bool success = SpoolerInteropService.PrintFileSilently(task.FilePath, task.PrinterName);

                    // 🛡️ 模拟缓冲时间，防止瞬间发送过多任务导致打印机内存溢出
                    await Task.Delay(2500);

                    if (success)
                    {
                        // ✅ 打印成功，从持久化账本中安全抹除记录！
                        DatabaseService.RemoveTask(task.Id);
                        OnTaskStatusChanged?.Invoke(task, "✅ 成功");
                    }
                    else
                    {
                        // ❌ 打印失败，增加重试计数
                        DatabaseService.UpdateTaskFailure(task);
                        OnTaskStatusChanged?.Invoke(task, "❌ 失败");
                    }
                }
            });
        }

        /// <summary>
        /// 📥 将任务推入内存队列 (生产者调用)
        /// </summary>
        public static async Task EnqueueTaskAsync(PrintTaskRecord task)
        {
            await _taskChannel.Writer.WriteAsync(task);
        }

        /// <summary>
        /// 🔄 批量导入任务到队列
        /// </summary>
        public static async Task EnqueueTasksAsync(IEnumerable<PrintTaskRecord> tasks)
        {
            foreach (var task in tasks)
            {
                await EnqueueTaskAsync(task);
            }
        }
    }
}