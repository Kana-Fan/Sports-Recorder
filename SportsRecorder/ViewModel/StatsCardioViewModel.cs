using LiveChartsCore.Defaults;
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
        private DateTimePoint[] _selectRecord;
        public DateTimePoint[] SelectRecord
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

            // 區間內的資料（直方圖用：日期 + 當日有氧總時間）
            SelectRecord = filteredRecords
                .Where(r => r.Cardios != null && r.Cardios.Count > 0)
                .Select(r => new DateTimePoint(r.Date.Date, r.CardiosTotalDuration))
                .ToArray();

            // 區間內的總運動類型（表格用：同名有氧合併加總）
            SelectSheetData = new ObservableCollection<Cardio>(
                filteredRecords
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
