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
        public ObservableCollection<CommandWrapper> ActivityButtons { get; set; }


        // Two way properties
        public string AddActivityId { get; set; }
        public string AddActivityName { get; set; }

        

        public MainWindowViewModel()
        {
            Activities = new ObservableCollection<Activity> { new Activity("A", "somenameA") };

            foreach (var activity in Activities)
            {
                ActivityButtons.Add(new CommandWrapper(new ButtonActionCommand(ActivityButtonClicked), activity.Id));
            }
        }

        public void ActivityButtonClicked()
        {
            
        }
    }
}
