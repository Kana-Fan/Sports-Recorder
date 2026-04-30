using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SportsRecorder.Model
{
    public class TrainingJournalItem
    {
        public string Date { get; set; } // 日期

        public ObservableCollection<Cardio> Cardios { get; set; } = new();

        public ObservableCollection<Strengh> Strenghs { get; set; } = new();
    }

    // 有氧運動的一筆資料
    public class Cardio
    {
        public string Name { get; set; } // 運動名稱
        public double Distance { get; set; } = 0 ; // 運動距離 可為0
        public int Frequency { get; set; } = 0; // 運動次數 可為0
        public int Time { get; set; } // 運動時間
    }

    // 重量訓練的一筆資料
    public class Strengh
    {
        public string Part { get; set; } // 訓練部位
        public string Name { get; set; } // 訓練名稱
        public int Weight { get; set; } = 0; // 重量
        public int NumberOfGroups { get; set; } = 0; // 重量
        public int Frequency { get; set; } = 0; // 運動次數 可為0
    }
}
