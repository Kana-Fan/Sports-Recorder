using SportsRecorder.Helpers;
using SportsRecorder.Model;
using SportsRecorder.Services;
using System.Collections.ObjectModel;

namespace SportsRecorder.ViewModel
{
    internal abstract class TrainingRecordsViewModel : BaseViewModel
    {
        public static readonly string RecordsFileName = "records.json";

        private ObservableCollection<TrainingJournalItem> _records;
        public ObservableCollection<TrainingJournalItem> Records
        {
            get => _records;
            protected set
            {
                _records = value;
                OnPropertyChanged(nameof(Records));
            }
        }

        private Dictionary<DateTime, List<TrainingType>> _hasRecord;
        public Dictionary<DateTime, List<TrainingType>> HasRecord
        {
            get => _hasRecord;
            set
            {
                _hasRecord = value;
                OnPropertyChanged(nameof(HasRecord));
            }
        }

        private List<int> _availableYears;
        public List<int> AvailableYears
        {
            get => _availableYears;
            private set
            {
                _availableYears = value;
                OnPropertyChanged(nameof(AvailableYears));
            }
        }

        protected TrainingRecordsViewModel()
        {
            Records = JsonStorageService.Load<ObservableCollection<TrainingJournalItem>>(RecordsFileName);
            RefreshHasRecord();
        }

        protected void RefreshHasRecord(bool needCardios = true, bool needStrengths = true)
        {
            var dict = new Dictionary<DateTime, List<TrainingType>>();

            foreach (var item in Records)
            {
                var types = new List<TrainingType>();

                if (needCardios && item.Cardios?.Count > 0)
                    types.Add(TrainingType.Cardio);

                if (needStrengths && item.Strengths?.Count > 0)
                    types.Add(TrainingType.Strength);

                if (types.Count > 0)
                    dict[item.Date.Date] = types;
            }

            HasRecord = dict;

            AvailableYears = Records
                .Select(r => r.Date.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();
        }
    }
}
