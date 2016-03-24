using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
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
        private ICommand _addTraceCommand;
        private ICommand _addActivityCommand;
        private ICommand _saveLogCommand;
        private ICommand _autoGenLogCommand;
        private ICommand _browseLogCommand;
        private ICommand _addLogCommand;
        private ICommand _clearTraceCommand;
        private ICommand _resetCommand;
        private ICommand _saveGraphCommand;
        private ICommand _loadGraphCommand;

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

        #region Commands

        public ICommand AddTraceCommand { get { return _addTraceCommand; } set { _addTraceCommand = value; OnPropertyChanged(); } }
        public ICommand AddActivityCommand { get { return _addActivityCommand; } set { _addActivityCommand = value; OnPropertyChanged(); } }
        public ICommand SaveLogCommand { get { return _saveLogCommand; } set { _saveLogCommand = value; OnPropertyChanged(); } }
        public ICommand AutoGenLogCommand { get { return _autoGenLogCommand; } set { _autoGenLogCommand = value; OnPropertyChanged(); } }
        public ICommand BrowseLogCommand { get { return _browseLogCommand; } set { _browseLogCommand = value; OnPropertyChanged(); } }
        public ICommand AddLogCommand { get { return _addLogCommand; } set { _addLogCommand = value; OnPropertyChanged(); } }
        public ICommand ClearTraceCommand { get { return _clearTraceCommand; } set { _clearTraceCommand = value; OnPropertyChanged(); } }
        public ICommand ResetCommand { get { return _resetCommand; } set { _resetCommand = value; OnPropertyChanged(); } }
        public ICommand SaveGraphCommand { get { return _saveGraphCommand; } set { _saveGraphCommand = value; OnPropertyChanged(); } }
        public ICommand LoadGraphCommand { get { return _loadGraphCommand; } set { _loadGraphCommand = value; OnPropertyChanged(); } }

        #endregion

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

            SetupCommands();
        }

        private void SetupCommands()
        {
            AddTraceCommand = new ButtonActionCommand(AddTrace);
            AddActivityCommand = new ButtonActionCommand(AddActivity);
            SaveLogCommand = new ButtonActionCommand(SaveLog);
            AutoGenLogCommand = new ButtonActionCommand(AutoGenLog);
            BrowseLogCommand = new ButtonActionCommand(BrowseLog);
            AddLogCommand = new ButtonActionCommand(AddLog);
            ClearTraceCommand = new ButtonActionCommand(ClearTrace);
            ResetCommand = new ButtonActionCommand(Reset);
            LoadGraphCommand = new ButtonActionCommand(LoadGraph);
            SaveGraphCommand = new ButtonActionCommand(SaveGraph);
        }

        public void DummyMethod() {} // TODO: Stop using CommandWrapper eventually...


        #region Command implementation methods

        public void AddTrace()
        {
            CurrentLog.Add(_currentTraceBeingAdded.Copy());
            ClearTrace();
        }

        public void AddActivity()
        {
            Activities.Add(new Activity(AddActivityId, AddActivityName));
            ActivityButtons.Add(new CommandWrapper(new ButtonActionCommand(DummyMethod), AddActivityId));
        }

        public void SaveLog()
        {
            
        }

        public void AutoGenLog()
        {

        }

        public void BrowseLog()
        {

        }

        public void AddLog()
        {

        }

        public void ClearTrace()
        {
            _currentTraceBeingAdded = new LogTrace();
            OnPropertyChanged("CurrentTraceBeingAddedString");
        }

        public void Reset()
        {
            // TODO: Figure out everything that needs to be reset
        }

        public void LoadGraph()
        {

        }

        public void SaveGraph()
        {

        }

        #endregion

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
