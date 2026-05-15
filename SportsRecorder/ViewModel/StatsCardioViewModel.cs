using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using SportsRecorder.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SportsRecorder.ViewModel
{
    class StatsCardioViewModel : BaseViewModel
    {
        private readonly IDialogService _dialogService;

        private ObservableCollection<TrainingJournalItem> Records { get; set; } // 全部的資料

        // 區間內的資料(直方圖用，一筆就是日期跟總時間) LiveChartsCore.Defaults 的type
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
            new SKColor(0x1A, 0x3A, 0x6E), // 深海藍
            new SKColor(0x2A, 0x52, 0xA0), // 靛藍
            new SKColor(0x3D, 0x6D, 0xB8), // 皇家藍
            new SKColor(0x2E, 0x7A, 0xA8), // 鋼藍
            new SKColor(0x1F, 0x6E, 0x8C), // 深青藍
            new SKColor(0x4A, 0x5E, 0x9E), // 灰藍
            new SKColor(0x2B, 0x5E, 0xA8), // 中藍
            new SKColor(0x1A, 0x52, 0x76), // 深孔雀藍
            new SKColor(0x3A, 0x7D, 0xBF), // 天藍
            new SKColor(0x35, 0x5E, 0x8A), // 牛仔藍
        ];

        public StatsCardioViewModel()
        {
            _dialogService = new DialogService();

            Records = JsonStorageService.Load<ObservableCollection<TrainingJournalItem>>("records.json");

            GetData();
        }

        // 獲取期間內的有氧運動資料與總運動時間
        private void GetData()
        {
            //  篩選區間內的紀錄
            var filteredRecords =
                Records
                .Where(r => r.Date.Date >= StartDate.Date && r.Date.Date <= EndDate.Date)
                .ToList();

            // 先把所有運動找出來
            var allCardios = filteredRecords
                .Where(r => r.Cardios != null && r.Cardios.Count > 0)
                .SelectMany(r => r.Cardios)
                .Select(s => s.Name)
                .Distinct()
                .ToList();

            // 把日期資料攤平
            var dateGroups = filteredRecords
                .Where(r => r.Cardios != null && r.Cardios.Count > 0)
                .Select(r => new
                {
                    r.Date.Date,
                    DurationMins = r.Cardios
                        .GroupBy(s => s.Name)
                        .ToDictionary(
                            g => g.Key,
                            g => (int)g.Sum(s => s.DurationMin))
                })
                .ToList();

            // 區間內的資料（直方圖用：日期 + 各運動時間)
            SelectRecord = allCardios.Select((Name, index) =>
                (ISeries)new StackedColumnSeries<DateTimePoint>
                {
                    Name = Name,
                    Values = dateGroups.Select(d => new DateTimePoint
                    {
                        DateTime = d.Date,
                        Value = d.DurationMins.TryGetValue(Name, out var w) ? w : null
                    }).ToArray(),

                    StackGroup = 0,
                    ShowDataLabels = true,
                    Fill = new SolidColorPaint(DarkPalette[index % DarkPalette.Length]) // 循環取色
                }
            ).ToArray();

            // 區間內的總運動類型（表格用：同名有氧合併加總）
            SelectSheetData = new ObservableCollection<Cardio>(
                filteredRecords
                    .Where(r => r.Cardios != null && r.Cardios.Count > 0)
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

            // 期間內的有氧運動總時間
            TotalTime = Math.Round(filteredRecords.Sum(r => r.CardiosTotalDuration) / 60.0, 2);
        }

        public Func<DateTime, string> Formatter { get; set; } = date => date.ToString("MMMM dd");
    }
}
