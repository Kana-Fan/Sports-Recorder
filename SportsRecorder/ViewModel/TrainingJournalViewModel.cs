using SportsRecorder.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SportsRecorder.ViewModel
{
    internal class TrainingJournalViewModel : BaseViewModel
    {
        public ObservableCollection<TrainingJournalItem> TrainingJournalItems { get; }
    }
}
