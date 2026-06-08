namespace SportsRecorder.Model
{
    /// <summary>
    /// 統計分析用的class。
    /// </summary>
    public class LegendItem : BaseModel
    {
        public string Name { get; set; }
        public string Color { get; set; } // "#4D72C8" 格式

        public string SvgPath { get; set; }

        private bool _isVisible = true;
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;
                OnPropertyChanged(nameof(IsVisible));
            }
        }
    }

}
