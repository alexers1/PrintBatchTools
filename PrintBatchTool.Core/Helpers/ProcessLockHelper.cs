using System;
using System.Threading;

namespace PrintBatchTool.Helpers
{
    public static class ProcessLockHelper
    {
        // 声明一个静态的 Mutex，防止被垃圾回收器吃掉
        private static Mutex _appMutex;

        /// <summary>
        /// 检查当前是否为唯一实例
        /// </summary>
        public static bool EnsureSingleInstance()
        {
            // 创建一个全局唯一的命名互斥锁
            _appMutex = new Mutex(true, "PrintBatchTool_Global_Mutex_Lock_2026", out bool isNewInstance);

            // isNewInstance 为 true 代表这是第一个实例；为 false 说明已经有人在跑了
            return isNewInstance;
        }
    }
}