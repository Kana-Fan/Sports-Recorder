# SportsRecorder

一款以 WPF (.NET 8) 打造的桌面版運動紀錄應用程式，讓你輕鬆記錄有氧與重量訓練、透過圖表與熱力圖檢視訓練成果，並支援範本與資料匯出匯入。

## 核心功能特點

- **訓練日誌**：以自訂樣式日曆檢視每日訓練紀錄（有紀錄的日期會依有氧／重訓標記），快速新增/編輯有氧運動與重量訓練項目，並可一鍵回到今天。表格輸入有數值限制，未儲存的變更離開頁面前會提示。
- **運動類型管理**：內建有氧運動、重訓部位/動作清單，可自訂新增項目。
- **訓練範本**：將常用的訓練組合存成範本（可新增、編輯、重新命名與刪除），訓練日誌可一鍵套用範本，也能把當日內容另存為範本。
- **統計圖表**：有氧、重訓分項統計圖表（LiveCharts），可自訂 7～31 天的日期區間，並支援深色/淺色主題自動配色。
- **訓練熱力圖**：以熱力圖呈現長期訓練頻率與強度分佈，可切換年份檢視。
- **資料管理**：支援資料匯出（Excel，透過 ClosedXML）與匯入，並提供合併／覆蓋兩種匯入模式與 JSON 格式驗證。
- **深色/淺色主題切換**：搭配自適應標題列（TitlebarAdaptive.WPF），主題切換即時套用至圖表座標軸配色。
- **可收合側邊導覽列**：支援展開/收合動畫，收合狀態下點選選單會自動展開。
- **啟動動畫**：開機顯示影片開機畫面，提升應用程式質感。

## 技術堆疊 (Tech Stack)

| 分類 | 技術 |
| --- | --- |
| 執行環境 | .NET 8 (`net8.0-windows`) |
| UI 框架 | WPF (Windows Presentation Foundation) |
| 架構模式 | MVVM（手寫 `INotifyPropertyChanged` + `RelayCommand`） |
| 屬性通知輔助 | PropertyChanged.Fody |
| 圖表 | LiveChartsCore.SkiaSharpView.WPF |
| Excel 匯出/匯入 | ClosedXML |
| 資料序列化 | System.Text.Json |
| 動畫 GIF 支援 | WpfAnimatedGif |
| 標題列自適應主題 | TitlebarAdaptive.WPF |
| 資料儲存 | 本機 JSON 檔案（`Assets/Data/`） |

## 快速開始

### 需求

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- （建議）Visual Studio 2022 17.8 以上版本

### 安裝依賴

於方案根目錄還原 NuGet 套件：

```bash
dotnet restore
```

### 配置環境

本專案無需額外的環境變數或設定檔即可運行；所有訓練資料、運動類型清單皆以 JSON 檔案儲存於執行目錄下的 `Assets/Data/`，首次啟動時若檔案不存在會自動建立空的資料集合。

### 啟動專案

```bash
dotnet build
dotnet run --project SportsRecorder/SportsRecorder.csproj
```

或直接以 Visual Studio 開啟 `SportsRecorder.sln`，將 `SportsRecorder` 設為啟動專案後按 F5 執行。

## 專案目錄結構

```
SportsRecorder/
├── .claude/                  # Claude Code 專案設定與知識庫，記錄架構慣例、開發指引，協助 AI 助理快速理解專案上下文
├── SportsRecorder.sln         # Visual Studio 方案檔
└── SportsRecorder/            # 主應用程式專案
    ├── Assets/                 # 靜態資源
    │   ├── Data/                 # 執行期 JSON 資料檔（訓練紀錄、範本、運動類型清單）
    │   ├── Fonts/                 # 自訂字型
    │   ├── Icons/                 # 深色/淺色模式圖示
    │   ├── Images/                # 圖片資源
    │   └── Videos/                # 開機畫面影片
    ├── Converters/              # WPF 值轉換器（XAML Binding 用）
    ├── Helpers/                 # 通用輔助類別（RelayCommand、CalendarMarker 等）
    ├── Model/                   # 資料模型（訓練紀錄、有氧/重訓項目、範本等）
    ├── Services/                # 服務層（JSON 儲存、對話框、主題切換、圖表主題）
    ├── Styles/                  # 深色/淺色主題資源字典
    ├── ViewModel/                # MVVM ViewModel（頁面邏輯、共用基底類別）
    ├── Views/                   # 各頁面與自訂對話框（確認、成功提示、輸入）XAML
    ├── App.xaml(.cs)            # 應用程式進入點與啟動流程
    ├── MainWindow.xaml(.cs)     # 主視窗與導覽容器
    └── SplashScreenWindow.xaml(.cs) # 開機畫面視窗
```
