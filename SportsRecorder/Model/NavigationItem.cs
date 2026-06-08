using System.Collections.ObjectModel;

namespace SportsRecorder.Model
{
    public class NavigationItem
    {
        public string Title { get; set; }
        public string IconKey { get; set; }
        public string Key { get; set; }
        public ObservableCollection<NavigationItem> Children { get; set; } = new();
    }
}
