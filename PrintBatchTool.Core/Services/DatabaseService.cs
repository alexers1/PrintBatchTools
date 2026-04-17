using PrintBatchTool.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PrintBatchTool.Services
{
    public static class DatabaseService
    {
        /// <summary>
        /// 批量将用户选择的文件写入数据库 (生产者逻辑)
        /// </summary>
        public static void AddTasksToQueue(IEnumerable<string> filePaths, string printerName)
        {
            var tasks = filePaths.Select(path => new PrintTaskRecord
            {
                FilePath = path,
                PrinterName = printerName,
                AddTime = DateTime.Now,
                Status = 0, // 0 表示待处理
                AttemptCount = 0
            });

            // 使用 sqlite-net-pcl 的批量插入，性能极高
            App.Database.InsertAll(tasks);
        }

        /// <summary>
        /// 获取所有尚未完成的任务 (用于断点续传和后台消费)
        /// </summary>
        public static List<PrintTaskRecord> GetPendingTasks()
        {
            return App.Database.Table<PrintTaskRecord>()
                               .Where(t => t.Status == 0 || t.Status == 1)
                               .OrderBy(t => t.AddTime)
                               .ToList();
        }

        /// <summary>
        /// 打印成功后，彻底从账本中抹除记录
        /// </summary>
        public static void RemoveTask(int taskId)
        {
            App.Database.Delete<PrintTaskRecord>(taskId);
        }

        /// <summary>
        /// 打印失败时增加重试次数，超过上限则标记为彻底失败 (状态 2)
        /// </summary>
        public static void UpdateTaskFailure(PrintTaskRecord task)
        {
            task.AttemptCount += 1;
            if (task.AttemptCount >= 3)
            {
                task.Status = 2; // 超过 3 次，标记为死信任务，不再重试
            }
            App.Database.Update(task);
        }

        /// <summary>
        /// 清空所有历史或遗留任务
        /// </summary>
        public static void ClearAllPendingTasks()
        {
            var pending = GetPendingTasks();
            foreach (var task in pending)
            {
                App.Database.Delete<PrintTaskRecord>(task.Id);
            }
        }
    }
}