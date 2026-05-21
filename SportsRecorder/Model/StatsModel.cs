using LiveChartsCore;
using LiveChartsCore.Defaults;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SportsRecorder.Model
{
    /// <summary>
    /// 統計分析用的class。
    /// </summary>
    public class StrengthSheetData
    {
        public string BodyPart { get; set; } // 訓練部位

        public ISeries[] WeightChanges { get; set; } // 不同運動的重量變化
    }

}
