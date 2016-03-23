using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UlrikHovsgaardAlgorithm.Data;

namespace UlrikHovsgaardWpf.ViewModels
{
    public class MainWindowViewModel
    {
        public ObservableCollection<Activity> Activities { get; set; }

        public MainWindowViewModel()
        {
            Activities = new ObservableCollection<Activity> { new Activity("A", "somenameA") };
        }
    }
}
