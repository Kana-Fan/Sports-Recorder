using CommunityToolkit.Mvvm.Input;
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
using System.Windows.Input;

namespace SportsRecorder.ViewModel
{
    internal class StatsStrengthViewModel : BaseViewModel
    {
        private readonly IDialogService _dialogService;

        private ObservableCollection<TrainingJournalItem> Records { get; set; } // 全部的資料

        // 區間內的資料(直方圖用，一筆就是日期跟各部位的總重量)
        private ISeries[] _selectRecord;
        public ISeries[] SelectRecord
        {
            get => _selectRecord;
            set => SetProperty(ref _selectRecord, value);
        }

        // 區間內的總運動類型(表格用)
        private ObservableCollection<StrengthSheetData> _selectSheetData;
        public ObservableCollection<StrengthSheetData> SelectSheetData
        {
            get => _selectSheetData;
            set => SetProperty(ref _selectSheetData, value);
        }

        // 用來切換各運動的重量變化
        private ISeries[] _lineChartData;
        public ISeries[] LineChartData
        {
            get => _lineChartData;
            set => SetProperty(ref _lineChartData, value);
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
        public ICommand SwitchLineChartCommand { get; }

        public StatsStrengthViewModel()
        {
            _dialogService = new DialogService();

            Records = JsonStorageService.Load<ObservableCollection<TrainingJournalItem>>("records.json");

            GetData();

            // Commands
            SwitchLineChartCommand = new RelayCommand<StrengthSheetData>(entry =>
            {
                if (entry == null) return;

                LineChartData = new ISeries[]
                {
                    new LineSeries<DateTimePoint>
                    {
                        Values = entry.WeightChanges
                    }
                };
            });
        }

        private void GetData()
        {
            //  篩選區間內的紀錄
            var filteredRecords =
                Records
                .Where(r => r.Date.Date >= StartDate.Date && r.Date.Date <= EndDate.Date)
                .ToList();

            // 先把所有部位找出來
            var allBodyParts = filteredRecords
                .Where(r => r.Strengths != null && r.Strengths.Count > 0)
                .SelectMany(r => r.Strengths)
                .Select(s => s.BodyPart)
                .Distinct()
                .ToList();

            // 把日期資料攤平
            var dateGroups = filteredRecords
                .Where(r => r.Strengths != null && r.Strengths.Count > 0)
                .Select(r => new
                {
                    r.Date.Date,
                    ByPart = r.Strengths
                        .GroupBy(s => s.BodyPart)
                        .ToDictionary(
                            g => g.Key,
                            g => (double)g.Sum(s => s.WeightKg * s.Sets * s.Reps))
                })
                .ToList();

            // SelectRecord：每天各部位的總重量（直方圖用）
            SelectRecord = allBodyParts.Select((bodyPart, index) =>
                (ISeries)new ColumnSeries<DateTimePoint>
                {
                    Name = bodyPart,
                    Values = dateGroups.Select(d => new DateTimePoint
                    {
                        DateTime = d.Date,
                        Value = d.ByPart.TryGetValue(bodyPart, out var w) ? w : null
                    }).ToArray(),
                    Fill = new SolidColorPaint(DarkPalette[index % DarkPalette.Length]) // 循環取色
                }
            ).ToArray();

            // SelectSheetData：每個訓練動作的重量變化（表格用）
            // 跨所有日期，把同一 BodyPart + Name 的紀錄合併，記錄每天的最大重量
            SelectSheetData = new ObservableCollection<StrengthSheetData>(
                filteredRecords
                    .Where(r => r.Strengths != null && r.Strengths.Count > 0)
                    .SelectMany(r => r.Strengths.Select(s => new { r.Date.Date, Strength = s }))
                    .GroupBy(x => new { x.Strength.BodyPart, x.Strength.Name })
                    .Select(g => new StrengthSheetData
                    {
                        BodyPart = g.Key.BodyPart,
                        Name = g.Key.Name,
                        WeightChanges = g
                            .GroupBy(x => x.Date)
                            .Select(dayGroup => new DateTimePoint(dayGroup.Key, dayGroup.Max(x => x.Strength.WeightKg))) // 同一天同動作取最大重
                            .ToArray(),
                    })
            );

            // 初始化折線圖資料
            LineChartData = new ISeries[]
            {
                new LineSeries<DateTimePoint>
                {
                    Values = SelectSheetData.Count != 0 ? SelectSheetData[0].WeightChanges : []
                }
            };
        }

        public Func<DateTime, string> Formatter { get; set; } = date => date.ToString("MMMM dd");
    }
}
