using SQLite;
using System;

namespace PrintBatchTool.Models
{
    /// <summary>
    /// 📝 打印任务记录实体，将自动映射到 SQLite 的 PrintTaskRecord 表
    /// 这是我们实现“持久化账本”和“断点续传”的核心数据结构
    /// </summary>
    [Table("PrintTaskRecord")]
    public class PrintTaskRecord
    {
        /// <summary>
        /// 唯一自增主键
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// 文件的绝对物理路径 (例如：D:\Docs\report.pdf)
        /// 加上 [Indexed] 索引，方便系统在排查重复任务时快速查询
        /// </summary>
        [Indexed]
        public string FilePath { get; set; }

        /// <summary>
        /// 目标打印机名称
        /// 记录在表里是为了防止程序崩溃重启后，忘了当初用户选的是哪台打印机
        /// </summary>
        public string PrinterName { get; set; }

        /// <summary>
        /// 任务加入队列的时间
        /// 确保后台消费时严格遵循 FIFO (先进先出) 的排队原则
        /// </summary>
        public DateTime AddTime { get; set; }

        /// <summary>
        /// 当前任务状态：0 = 队列等待中，1 = 正在提交打印机，2 = 打印失败
        /// 崩溃恢复时，我们只重新加载状态为 0 和 1 的任务
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 失败重试次数
        /// 如果某个文件一直导致驱动卡死，重试超过一定次数后可以直接放弃，防止死循环
        /// </summary>
        public int AttemptCount { get; set; }
    }
}