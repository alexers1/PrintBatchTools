using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Printing;

namespace PrintBatchTool.Services
{
    public static class SpoolerInteropService
    {
        /// <summary>
        /// 获取本机所有已安装的可用打印机
        /// </summary>
        public static List<PrintQueue> GetAllPrinters()
        {
            using var server = new LocalPrintServer();
            return server.GetPrintQueues().ToList();
        }

        /// <summary>
        /// 获取系统默认打印机
        /// </summary>
        public static PrintQueue GetDefaultPrinter()
        {
            try
            {
                return LocalPrintServer.GetDefaultPrintQueue();
            }
            catch
            {
                return null; // 防止某些纯净系统没有默认打印机报错
            }
        }

        /// <summary>
        /// 利用操作系统的 ShellExecute 唤起关联程序 (Word/PDF阅读器) 进行静默打印
        /// </summary>
        public static bool PrintFileSilently(string filePath, string printerName)
        {
            try
            {
                ProcessStartInfo info = new ProcessStartInfo
                {
                    FileName = filePath,
                    Verb = "printto",
                    Arguments = $"\"{printerName}\"",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = true
                };

                using Process p = Process.Start(info);
                // 等待关联程序响应，防止并发过高导致卡死
                p?.WaitForInputIdle(5000);

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"物理打印发生系统级异常: {ex.Message}");
                return false;
            }
        }
    }
}