using DocumentFormat.OpenXml.Bibliography;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.VisualElements;
using SkiaSharp;
using SportsRecorder.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SportsRecorder.ViewModel
{
    class StatsCardioViewModel : BaseViewModel
    {
        private readonly IDialogService _dialogService;

        private static bool _sheetIsTime = true;

        private ObservableCollection<TrainingJournalItem> Records { get; set; } // 全部的資料

        private string _sheetTitle;
        public string SheetTitle
        {
            get => _sheetTitle;
            set => SetProperty(ref _sheetTitle, value);
        }

        // 區間內的資料(直方圖用) LiveChartsCore.Defaults 的type
        private ISeries[] _selectRecord;
        public ISeries[] SelectRecord
        {
            get => _selectRecord;
            set => SetProperty(ref _selectRecord, value);
        }

        // 區間內的總運動類型(表格用)
        private ObservableCollection<Cardio> _selectSheetData;
        public ObservableCollection<Cardio> SelectSheetData
        {
            get => _selectSheetData;
            set => SetProperty(ref _selectSheetData, value);
        }

        // 期間內的有氧運動總運動時間
        private double _totalTime = 0;
        public double TotalTime
        {
            get => _totalTime;
            set
            {
                SetProperty(ref _totalTime, value);
            }
        }

        // 開始日期
        private DateTime _startDate = DateTime.Today.AddDays(-7);
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                SetProperty(ref _startDate, value);
                // 如果區間<7 就強制 = 7
                if ((EndDate - StartDate).Days < 7)
                {
                    _endDate = StartDate.AddDays(7);
                    OnPropertyChanged(nameof(EndDate));
                    _dialogService.ShowMessage("~已自動調整為最小 7 天區間~");
                }
                // 如果區間>30 就強制 = 30
                else if ((EndDate - StartDate).Days > 30)
                {
                    _endDate = StartDate.AddDays(30);
                    OnPropertyChanged(nameof(EndDate));
                    _dialogService.ShowMessage("~已自動調整為最大 30 天區間~");
                }

                GetData();
            }
        }

        // 結束日期
        private DateTime _endDate = DateTime.Today;
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                SetProperty(ref _endDate, value);
                // 如果區間<7 就強制 = 7
                if ((EndDate - StartDate).Days < 7)
                {
                    _startDate = EndDate.AddDays(-7);
                    OnPropertyChanged(nameof(StartDate));
                    _dialogService.ShowMessage("~已自動調整為最小 7 天區間~");
                }
                // 如果區間>30 就強制 = 30
                else if ((EndDate - StartDate).Days > 30)
                {
                    _startDate = EndDate.AddDays(-30);
                    OnPropertyChanged(nameof(StartDate));
                    _dialogService.ShowMessage("~已自動調整為最大 30 天區間~");
                }

                GetData();
            }
        }

        private static readonly SKColor[] DarkPalette =
        [
            new SKColor(0x4D, 0x72, 0xC8), // 靛藍
            new SKColor(0x7A, 0x52, 0xB0), // 深紫
            new SKColor(0x3D, 0x8A, 0x9E), // 深青
            new SKColor(0xA0, 0x50, 0x80), // 暗玫紅
            new SKColor(0x66, 0x55, 0xA8), // 藍紫
            new SKColor(0xA0, 0x60, 0x40), // 暗橙棕
            new SKColor(0x4A, 0x7F, 0xA8), // 鋼藍
            new SKColor(0x8A, 0x4A, 0x6E), // 紫紅
            new SKColor(0x3D, 0x7A, 0x62), // 暗綠
            new SKColor(0x5A, 0x8A, 0x7A), // 灰綠
        ];

        // Commands
        public ICommand SwitchSheet { get; }

        public StatsCardioViewModel()
        {
            _dialogService = new DialogService();

            Records = JsonStorageService.Load<ObservableCollection<TrainingJournalItem>>("records.json");

            SheetTitle = "每日運動總時間";

            GetData();

            SwitchSheet = new RelayCommand(() =>
            {
                _sheetIsTime = !_sheetIsTime;

                GetData();
            });
        }

        // 獲取期間內的有氧運動資料與總運動時間
        private void GetData()
        {
            //  篩選區間內的紀錄
            var cardioRecords =
                Records
                .Where(r =>
                r.Date.Date >= StartDate.Date && r.Date.Date <= EndDate.Date &&
                r.Cardios != null && r.Cardios.Count > 0)
                .ToList();

            // 區間內的資料（直方圖用：日期 + 各運動時間or 各運動距離)
            UpdateBarChart(cardioRecords);

            // 區間內的總運動類型（表格用：同名有氧合併加總）
            UpdateSheet(cardioRecords);

            SheetTitle = _sheetIsTime ? "每日運動總時間" : "每日運動總距離";

            YAxisLabeler = _sheetIsTime ? value => $"{value:N0} min" : value => $"{value:N2} km";

            // 期間內的有氧運動總時間
            TotalTime = Math.Round(cardioRecords.Sum(r => r.CardiosTotalDuration) / 60.0, 2);
        }

        private void UpdateBarChart(List<TrainingJournalItem> cardioRecords)
        {
            // 先把所有運動找出來
            var allCardios = cardioRecords
                .SelectMany(r => r.Cardios)
                .Select(s => s.Name)
                .Distinct()
                .ToList();

            // 把日期資料攤平
            var dateGroups = cardioRecords
                .Select(r => new
                {
                    r.Date.Date,
                    DurationMins = r.Cardios
                        .GroupBy(s => s.Name)
                        .ToDictionary(
                            g => g.Key,
                            g => (int)g.Sum(s => s.DurationMin)),
                    DistanceKms = r.Cardios
                        .GroupBy(s => s.Name)
                        .ToDictionary(
                            g => g.Key,
                            g => (double)g.Sum(s => s.DistanceKm))
                })
                .ToList();

            SelectRecord = allCardios.Select((Name, index) =>
                (ISeries)new StackedColumnSeries<DateTimePoint>
                {
                    Name = Name,
                    Values = dateGroups.Select(d => new DateTimePoint
                    {
                        DateTime = d.Date,
                        Value = _sheetIsTime ?
                        (d.DurationMins.TryGetValue(Name, out var t) ? t : null) : (d.DistanceKms.TryGetValue(Name, out var w) ? w : null)
                    }).ToArray(),

                    StackGroup = 0,
                    ShowDataLabels = true,
                    DataLabelsPaint = new SolidColorPaint(SKColor.Parse("#FFA1A7CC")), // 數值的顏色
                    Fill = new SolidColorPaint(DarkPalette[index % DarkPalette.Length]) // 循環取色
                }
            ).ToArray();
        }

        private void UpdateSheet(List<TrainingJournalItem> cardioRecords)
        {
            SelectSheetData = new ObservableCollection<Cardio>(
                cardioRecords
                    .SelectMany(r => r.Cardios)
                    .GroupBy(c => c.Name)
                    .Select(g => new Cardio
                    {
                        Name = g.Key,
                        DistanceKm = g.Sum(c => c.DistanceKm),
                        Times = g.Sum(c => c.Times),
                        DurationMin = g.Sum(c => c.DurationMin)
                    })
                    .OrderBy(x => x.Times)
            );
        }

        public Func<DateTime, string> Formatter { get; set; } = date => date.ToString("MMMM dd");

        public Func<double, string> YAxisLabeler { get; set; } = _sheetIsTime ? value => $"{value:N0} min" : value => $"{value:N2} km";
    }
}
