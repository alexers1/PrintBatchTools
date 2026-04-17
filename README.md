# 🖨️ PrintBatchTool

![Platform](https://img.shields.io/badge/Platform-Windows-blue)
![Framework](https://img.shields.io/badge/Framework-WinUI%203-blueviolet)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/License-MIT-green)

A robust, asynchronous batch printing utility built with **WinUI 3** and **.NET 8**. Designed for high concurrency and enterprise-level reliability, it utilizes a "Persistent Ledger" architecture to ensure zero data loss during automated printing tasks.

---

## ✨ Key Features

### 🛡️ Persistent Ledger Pattern
All print tasks are immediately recorded into a local **SQLite** database upon selection. A task is only dequeued and removed from the database *after* successful handoff to the Windows Print Spooler. This provides "at-least-once" delivery semantics.

### 🔄 Crash Recovery & Break-point Resume
Unexpected power outage? App crash? No problem. Upon restart, PrintBatchTool automatically scans the local SQLite database for orphaned tasks and prompts the user to resume the queue right where it left off.

### ⚡ Asynchronous Producer-Consumer Queue
UI rendering and physical printing are strictly decoupled using `System.Threading.Channels`. The UI thread remains silky smooth while a long-running background worker feeds files to the printer at a safe, steady pace, preventing hardware congestion.

### 🎨 Modern Windows 11 Dashboard
Built entirely with WinUI 3, featuring:
* Responsive **Card-based Dashboard Layout** to maximize screen real estate.
* Native Light/Dark mode transitions.
* Fluent Design System integration with Acrylic and Mica materials.

### 🧠 Smart File Filtering
Select an entire directory, and the app intelligently filters and categorizes files. Supported formats include:
`.pdf`, `.doc`, `.docx`, `.xls`, `.xlsx`, `.ppt`, `.pptx`, `.txt`, `.md`

---

## 🏗️ Technology Stack

* **UI Framework:** [Windows App SDK (WinUI 3)](https://learn.microsoft.com/en-us/windows/apps/winui/)
* **Runtime:** [.NET 8.0](https://dotnet.microsoft.com/download)
* **Database ORM:** [sqlite-net-pcl](https://github.com/praeclarum/sqlite-net)
* **Concurrency:** `System.Threading.Channels`
* **Spooler Interop:** `System.Printing` (via WPF Desktop framework reference)

---

## 📂 Project Structure

```text
PrintBatchTool/
├── Helpers/               # Utilities (e.g., ProcessLockHelper for single-instance lock)
├── Models/                # Data entities (PrintTaskRecord table) and App Configuration
├── Services/              # Core logic (DatabaseService, PrintQueueManager, DialogService)
├── App.xaml               # Application lifecycle & Global Database Initialization
└── MainWindow.xaml        # Main Dashboard UI & Event Handlers
