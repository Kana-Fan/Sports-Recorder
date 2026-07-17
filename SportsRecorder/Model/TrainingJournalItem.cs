using System.Collections.ObjectModel;

namespace SportsRecorder.Model
{
    public class TrainingJournalItem : BaseModel
    {
        public DateTime Date { get; set; } // 日期

        public int CardiosTotalDuration { get; set; } // 有氧運動的總持續時間(min)

        private ObservableCollection<Cardio> _cardios = new();
        public ObservableCollection<Cardio> Cardios
        {
            get => _cardios;
            set
            {
                _cardios = value;
                OnPropertyChanged(nameof(Cardios));
            }
        }

        private ObservableCollection<Strength> _strengths = new();
        public ObservableCollection<Strength> Strengths
        {
            get => _strengths;
            set
            {
                _strengths = value;
                OnPropertyChanged(nameof(Strengths));
            }
        }
    }

    public class WorkoutTemplate : BaseModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
        public ExerciseType Type { get; set; }

        public ObservableCollection<Cardio> Cardios { get; set; } = new();
        public ObservableCollection<Strength> Strengths { get; set; } = new();
        public override string ToString() => Name;
    }

    public enum ExerciseType
    {
        Cardio,
        Strength
    }

    // 基底 用來做切換運動類型
    public abstract class ExerciseEntryBase : BaseModel { }

    // 有氧運動的一筆資料
    public class Cardio : ExerciseEntryBase
    {
        private string _name = "-選擇運動名稱-";// 運動名稱
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        private double _distanceKm = 1;// 運動距離
        public double DistanceKm
        {
            get => _distanceKm;
            set
            {
                _distanceKm = value;
                OnPropertyChanged(nameof(DistanceKm));
            }
        }

        private int _times = 1;// 運動名稱
        public int Times
        {
            get => _times;
            set
            {
                _times = value;
                OnPropertyChanged(nameof(Times));
            }
        }

        private int _durationMin = 10; // 運動時間
        public int DurationMin
        {
            get => _durationMin;
            set
            {
                if (value > 1440)
                {
                    _durationMin = 1440;
                }
                else
                {
                    _durationMin = value;
                }

                OnPropertyChanged(nameof(DurationMin));
            }
        }
    }

    // 重量訓練的一筆資料
    public class Strength : ExerciseEntryBase
    {
        private string _bodyPart = "-選擇部位-"; // 訓練部位
        public string BodyPart
        {
            get => _bodyPart;
            set
            {
                _bodyPart = value;
                _name = "-選擇訓練名稱-";
                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(BodyPart));
            }
        }

        private string _name = "-選擇訓練名稱-"; // 訓練名稱
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        private double _weightKg = 10; // 重量
        public double WeightKg
        {
            get => _weightKg;
            set
            {
                _weightKg = value;
                OnPropertyChanged(nameof(WeightKg));
            }
        }

        private int _sets = 1; // 組數
        public int Sets
        {
            get => _sets;
            set
            {
                _sets = value;
                OnPropertyChanged(nameof(Sets));
            }
        }

        private int _reps = 1; // 運動次數
        public int Reps
        {
            get => _reps;
            set
            {
                _reps = value;
                OnPropertyChanged(nameof(Reps));
            }
        }
    }
}
