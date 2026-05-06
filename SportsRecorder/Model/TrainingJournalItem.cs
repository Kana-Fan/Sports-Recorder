using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SportsRecorder.Model
{
    public class TrainingJournalItem
    {
        public DateTime Date { get; set; } // 日期

        public int CardiosTotalDuration { get; set; } // 有氧運動的總持續時間(min)

        public ObservableCollection<Cardio> Cardios { get; set; } = new();

        public ObservableCollection<Strength> Strengths { get; set; } = new();
    }

    public enum ExerciseType
    {
        Cardio,
        Strength
    }

    // 基底 用來做切換運動類型
    public abstract class ExerciseEntryBase { }

    // 有氧運動的一筆資料
    public class Cardio : ExerciseEntryBase
    {
        public string Name { get; set; } // 運動名稱
        public double DistanceKm { get; set; } = 0 ; // 運動距離 可為0
        public int Times { get; set; } = 0; // 運動次數 可為0
        public int DurationMin { get; set; } // 運動時間
    }

    // 重量訓練的一筆資料
    public class Strength : ExerciseEntryBase
    {
        public string BodyPart { get; set; } // 訓練部位
        public string Name { get; set; } // 訓練名稱
        public double WeightKg { get; set; } = 0; // 重量
        public int Sets { get; set; } = 0; // 組數
        public int Reps { get; set; } = 0; // 運動次數 可為0
    }
}
