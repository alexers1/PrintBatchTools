using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using PrintBatchTool.Services;
using PrintBatchTool.Models;

namespace PrintBatchTool
{
    public sealed partial class MainWindow : Window
    {
        // 完整扫描到的所有符合支持格式的文件
        private List<string> _allScannedFiles = new List<string>();
        // 经过复选框筛选后，真正要送去打印的文件
        private List<string> _filteredFiles = new List<string>();

        // 支持的完整扩展名阵列，加入了 .md 格式！
        private readonly string[] _supportedExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".md" };

        public MainWindow()
        {
            this.InitializeComponent();

            PrintQueueManager.OnTaskStatusChanged += (task, msg) =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    TxtGlobalStatus.Text = $"处理中: {Path.GetFileName(task.FilePath)} - {msg}";
                });
            };

            this.Activated += MainWindow_Activated;
        }

        private bool _isInitialized = false;
        private async void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (_isInitialized) return;
            _isInitialized = true;

            PrintQueueManager.StartConsumer();
            LoadPrinters();
            await CheckForPendingTasksAsync();
        }

        #region 打印机与恢复逻辑 🖨️
        private async Task CheckForPendingTasksAsync()
        {
            var pendingTasks = DatabaseService.GetPendingTasks();
            if (pendingTasks.Any())
            {
                bool shouldRecover = await DialogService.ShowRecoveryPromptAsync(pendingTasks.Count);
                if (shouldRecover)
                {
                    TxtGlobalStatus.Text = "正在恢复上一次未完成的打印任务...";
                    await PrintQueueManager.EnqueueTasksAsync(pendingTasks);
                }
                else
                {
                    DatabaseService.ClearAllPendingTasks();
                    TxtGlobalStatus.Text = "遗留任务已清除，系统就绪。";
                }
            }
        }

        private void LoadPrinters()
        {
            try
            {
                var printers = SpoolerInteropService.GetAllPrinters();
                var defaultPrinter = SpoolerInteropService.GetDefaultPrinter();

                ComboPrinters.ItemsSource = printers;
                ComboPrinters.DisplayMemberPath = "FullName";

                if (defaultPrinter != null)
                {
                    var target = printers.FirstOrDefault(p => p.FullName == defaultPrinter.FullName);
                    if (target != null) ComboPrinters.SelectedItem = target;
                }

                TxtPrinterStatus.Text = printers.Count > 0 ? "打印机加载完成" : "未检测到打印机";
            }
            catch (Exception ex)
            {
                TxtPrinterStatus.Text = "加载打印机失败";
            }
        }

        private void ComboPrinters_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateStartButtonState();

        #endregion

        #region 智能扫描与格式筛选逻辑 🔍

        private async void BtnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                TxtFolderPath.Text = folder.Path;
                ScanFiles(folder.Path);
            }
        }

        private void ScanFiles(string rootPath)
        {
            _allScannedFiles.Clear();

            try
            {
                // 1. 扫描目录下所有支持的基础文件
                var files = Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    string ext = Path.GetExtension(file).ToLower();
                    if (_supportedExtensions.Contains(ext))
                    {
                        _allScannedFiles.Add(file);
                    }
                }

                // 2. 统计各个格式的数量，并动态更新 CheckBox 的显示文本
                UpdateCheckBoxCount(ChkPdf, ".pdf");
                UpdateCheckBoxCount(ChkDoc, ".doc");
                UpdateCheckBoxCount(ChkDocx, ".docx");
                UpdateCheckBoxCount(ChkXls, ".xls");
                UpdateCheckBoxCount(ChkXlsx, ".xlsx");
                UpdateCheckBoxCount(ChkPpt, ".ppt");
                UpdateCheckBoxCount(ChkPptx, ".pptx");
                UpdateCheckBoxCount(ChkTxt, ".txt");
                UpdateCheckBoxCount(ChkMd, ".md");

                // 3. 扫描完毕后，默认全选有文件的格式，触发筛选逻辑
                AutoCheckValidExtensions();

                TxtGlobalStatus.Text = $"扫描完成，共发现 {_allScannedFiles.Count} 个支持的文件。请在上方微调格式筛选。";
            }
            catch (Exception ex)
            {
                _ = DialogService.ShowErrorDetailsAsync("文件扫描失败", ex.Message);
            }
        }

        private void UpdateCheckBoxCount(CheckBox chk, string ext)
        {
            int count = _allScannedFiles.Count(f => Path.GetExtension(f).ToLower() == ext);
            // 去掉点号并转大写，比如 .pdf 变成 PDF
            string displayName = ext.Replace(".", "").ToUpper();
            chk.Content = $"{displayName} ({count})";
            chk.IsEnabled = count > 0; // 如果数量为0，直接变灰，防止误触
        }

        private void AutoCheckValidExtensions()
        {
            // 如果某个格式扫描到了文件，就默认打勾，方便用户直接打印
            ChkPdf.IsChecked = _allScannedFiles.Any(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));
            ChkDoc.IsChecked = _allScannedFiles.Any(f => f.EndsWith(".doc", StringComparison.OrdinalIgnoreCase));
            ChkDocx.IsChecked = _allScannedFiles.Any(f => f.EndsWith(".docx", StringComparison.OrdinalIgnoreCase));
            ChkXls.IsChecked = _allScannedFiles.Any(f => f.EndsWith(".xls", StringComparison.OrdinalIgnoreCase));
            ChkXlsx.IsChecked = _allScannedFiles.Any(f => f.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase));
            ChkPpt.IsChecked = _allScannedFiles.Any(f => f.EndsWith(".ppt", StringComparison.OrdinalIgnoreCase));
            ChkPptx.IsChecked = _allScannedFiles.Any(f => f.EndsWith(".pptx", StringComparison.OrdinalIgnoreCase));
            ChkTxt.IsChecked = _allScannedFiles.Any(f => f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));
            ChkMd.IsChecked = _allScannedFiles.Any(f => f.EndsWith(".md", StringComparison.OrdinalIgnoreCase));

            // 筛选逻辑会在 Checked 事件中自动触发，不需要手动调用 ApplyFilter()
        }

        // 当任何一个筛选 CheckBox 的状态发生改变时，触发此方法
        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            // 1. 收集当前被打勾的扩展名
            var allowedExts = new HashSet<string>();
            if (ChkPdf.IsChecked == true) allowedExts.Add(".pdf");
            if (ChkDoc.IsChecked == true) allowedExts.Add(".doc");
            if (ChkDocx.IsChecked == true) allowedExts.Add(".docx");
            if (ChkXls.IsChecked == true) allowedExts.Add(".xls");
            if (ChkXlsx.IsChecked == true) allowedExts.Add(".xlsx");
            if (ChkPpt.IsChecked == true) allowedExts.Add(".ppt");
            if (ChkPptx.IsChecked == true) allowedExts.Add(".pptx");
            if (ChkTxt.IsChecked == true) allowedExts.Add(".txt");
            if (ChkMd.IsChecked == true) allowedExts.Add(".md");

            // 2. 根据勾选的扩展名过滤文件列表
            _filteredFiles.Clear();
            foreach (var file in _allScannedFiles)
            {
                string ext = Path.GetExtension(file).ToLower();
                if (allowedExts.Contains(ext))
                {
                    _filteredFiles.Add(file);
                }
            }

            // 3. 将过滤后的最终列表绑定到 UI
            ListFilesToPrint.ItemsSource = null;
            ListFilesToPrint.ItemsSource = _filteredFiles.ToList();

            UpdateStartButtonState();
        }

        #endregion

        #region 手动控制与执行 🚀

        private async void BtnAddManualFile_Click(object sender, RoutedEventArgs e)
        {
            var filePicker = new FileOpenPicker();
            filePicker.ViewMode = PickerViewMode.List;
            filePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            foreach (var ext in _supportedExtensions) filePicker.FileTypeFilter.Add(ext);

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(filePicker, hwnd);

            var files = await filePicker.PickMultipleFilesAsync();
            if (files.Count > 0)
            {
                foreach (var file in files)
                {
                    if (!_filteredFiles.Contains(file.Path))
                        _filteredFiles.Add(file.Path); // 手动添加的文件无视筛选规则，直接进打印列表
                }
                ListFilesToPrint.ItemsSource = null;
                ListFilesToPrint.ItemsSource = _filteredFiles.ToList();
                UpdateStartButtonState();
            }
        }

        private void BtnClearList_Click(object sender, RoutedEventArgs e)
        {
            _allScannedFiles.Clear();
            _filteredFiles.Clear();

            // 取消所有勾选
            ChkPdf.IsChecked = ChkDoc.IsChecked = ChkDocx.IsChecked = false;
            ChkXls.IsChecked = ChkXlsx.IsChecked = ChkPpt.IsChecked = false;
            ChkPptx.IsChecked = ChkTxt.IsChecked = ChkMd.IsChecked = false;

            ListFilesToPrint.ItemsSource = null;
            UpdateStartButtonState();
        }

        private void UpdateStartButtonState()
        {
            // 使用 _filteredFiles (过滤后的最终数量) 来决定是否可以开启打印
            BtnStartPrint.IsEnabled = _filteredFiles.Count > 0 && ComboPrinters.SelectedItem != null;
            BtnStartPrint.Content = _filteredFiles.Count > 0 ? $"🚀 开始打印 ({_filteredFiles.Count} 份)" : "🚀 暂无任务";
        }

        private async void BtnStartPrint_Click(object sender, RoutedEventArgs e)
        {
            if (ComboPrinters.SelectedItem == null || _filteredFiles.Count == 0) return;

            string selectedPrinterName = (ComboPrinters.SelectedItem as System.Printing.PrintQueue).FullName;

            // 这里只把 _filteredFiles (经过用户筛选的最终文件) 送进数据库！
            DatabaseService.AddTasksToQueue(_filteredFiles, selectedPrinterName);
            var tasksToPrint = DatabaseService.GetPendingTasks();
            await PrintQueueManager.EnqueueTasksAsync(tasksToPrint);

            TxtGlobalStatus.Text = $"已将 {tasksToPrint.Count} 个任务提交至后台队列！UI 可自由操作。";
            BtnClearList_Click(null, null); // 提交后清空列表
        }

        private void ThemeToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch themeSwitch && this.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = themeSwitch.IsOn ? ElementTheme.Dark : ElementTheme.Light;
            }
        }
        #endregion
    }
}