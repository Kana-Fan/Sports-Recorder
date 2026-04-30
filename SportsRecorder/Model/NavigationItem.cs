using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SportsRecorder.Model
{
    public class NavigationItem
    {
        public string Title { get; set; }
        public string Key { get; set; }
        public ObservableCollection<NavigationItem> Children { get; set; } = new();
    }
}
