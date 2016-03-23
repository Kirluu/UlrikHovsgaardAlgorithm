using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using UlrikHovsgaardAlgorithm.Data;

namespace UlrikHovsgaardWpf.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        #region Fields

        #region Property fields

        private ObservableCollection<Activity> _activities;
        private ObservableCollection<CommandWrapper> _activityButtons;
        private ObservableCollection<LogTrace> _currentLog;

        #endregion

        private LogTrace _currentTraceBeingAdded;

        #endregion

        #region Properties

        public ObservableCollection<Activity> Activities { get { return _activities; } set { _activities = value; OnPropertyChanged(); } }
        public ObservableCollection<CommandWrapper> ActivityButtons { get { return _activityButtons; } set { _activityButtons = value; OnPropertyChanged(); } }
        public ObservableCollection<LogTrace> CurrentLog { get { return _currentLog; } set { _currentLog = value; OnPropertyChanged(); } }
        public string CurrentTraceBeingAddedString { get { return _currentTraceBeingAdded.ToString(); } }

        // Two way properties
        public string AddActivityId { get; set; } = null;
        public string AddActivityName { get; set; } = null;

        #endregion

        public MainWindowViewModel()
        {
            Activities = new ObservableCollection<Activity>
            {
                new Activity("A", "somenameA"),
                new Activity("B", "somenameB"),
                new Activity("C", "somenameC"),
                new Activity("D", "somenameD"),
                new Activity("E", "somenameE"),
                new Activity("F", "somenameF"),
                new Activity("G", "somenameG"),
                new Activity("H", "somenameH"),
                new Activity("I", "somenameI")
            };

            ActivityButtons = new ObservableCollection<CommandWrapper>();
            foreach (var activity in Activities)
            {
                ActivityButtons.Add(new CommandWrapper(new ButtonActionCommand(DummyMethod), activity.Id));
            }

            CurrentLog = new ObservableCollection<LogTrace> { new LogTrace('A', 'B', 'C', 'D', 'E', 'F', 'A', 'B', 'C', 'D', 'E', 'F', 'A', 'B', 'C', 'D', 'E', 'F') };

            _currentTraceBeingAdded = new LogTrace();
        }

        public void DummyMethod() // TODO: Stop using CommandWrapper eventually...
        {
        }

        public void ActivityButtonClicked(string buttonContentName)
        {
            var activity = Activities.ToList().Find(x => x.Id == buttonContentName);
            if (activity != null)
            {
                _currentTraceBeingAdded.Add(new LogEvent(activity.Id, activity.Name));
                OnPropertyChanged("CurrentTraceBeingAddedString");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        //[NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
