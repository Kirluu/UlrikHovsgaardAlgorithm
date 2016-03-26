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
using UlrikHovsgaardAlgorithm.Mining;
using UlrikHovsgaardAlgorithm.RedundancyRemoval;

namespace UlrikHovsgaardWpf.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        #region Fields

        #region Property fields

        private ObservableCollection<Activity> _activities;
        private ObservableCollection<ActivityNameWrapper> _activityButtons;
        private ObservableCollection<LogTrace> _currentLog;
        private bool _isGuiEnabled;
        private LogTrace _currentTraceBeingAdded;
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
        private ICommand _postProcessingCommand;

        #endregion
        
        private ExhaustiveApproach _exhaustiveApproach;


        #endregion

        #region Properties

        public ObservableCollection<Activity> Activities { get { return _activities; } set { _activities = value; OnPropertyChanged(); } }
        public ObservableCollection<ActivityNameWrapper> ActivityButtons { get { return _activityButtons; } set { _activityButtons = value; OnPropertyChanged(); } }
        public ObservableCollection<LogTrace> CurrentLog { get { return _currentLog; } set { _currentLog = value; OnPropertyChanged(); } }
        private LogTrace CurrentTraceBeingAdded { get { return _currentTraceBeingAdded; } set { _currentTraceBeingAdded = value; OnPropertyChanged("CurrentTraceBeingAddedString"); } }
        public string CurrentTraceBeingAddedString { get { return _currentTraceBeingAdded.ToString(); } }
        public bool IsGuiEnabled { get { return _isGuiEnabled; } set { _isGuiEnabled = value; OnPropertyChanged(); } }
        public string CurrentGraphString { get { return _exhaustiveApproach.Graph.ToString(); } }

        // Two way properties
        public string AddActivityId { get; set; }
        public string AddActivityName { get; set; }

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
        public ICommand PostProcessingCommand { get { return _postProcessingCommand; } set { _postProcessingCommand = value; OnPropertyChanged(); } }

        #endregion

        #endregion

        public MainWindowViewModel()
        {
            Init();

            SetupCommands();
        }

        private void Init()
        {
            Activities = new ObservableCollection<Activity>
            {
                new Activity("A", "somenameA"),
                new Activity("B", "somenameB"),
                new Activity("C", "somenameC")
            };

            _exhaustiveApproach = new ExhaustiveApproach(new HashSet<Activity>(Activities));
            OnPropertyChanged("CurrentGraphString");

            ActivityButtons = new ObservableCollection<ActivityNameWrapper>();
            foreach (var activity in Activities)
            {
                ActivityButtons.Add(new ActivityNameWrapper(activity.Id));
            }

            CurrentLog = new ObservableCollection<LogTrace>();

            AddActivityId = null;
            AddActivityName = null;

            CurrentTraceBeingAdded = new LogTrace();

            IsGuiEnabled = true;
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
            PostProcessingCommand = new ButtonActionCommand(PostProcessing);
        }

        #region Command implementation methods

        public void AddTrace()
        {
            CurrentLog.Add(CurrentTraceBeingAdded.Copy());
            _exhaustiveApproach.AddTrace(CurrentTraceBeingAdded.Copy());
            OnPropertyChanged("CurrentGraphString");
            ClearTrace();
        }

        public void AddActivity()
        {
            // TODO: Need on the fly activity addition to ExhaustiveApproach
            Activities.Add(new Activity(AddActivityId, AddActivityName));
            ActivityButtons.Add(new ActivityNameWrapper(AddActivityId));
            _exhaustiveApproach = new ExhaustiveApproach(new HashSet<Activity>(Activities));
            OnPropertyChanged("CurrentGraphString");
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
            CurrentTraceBeingAdded = new LogTrace();
            OnPropertyChanged("CurrentTraceBeingAddedString");
        }

        public void Reset()
        {
            Init();
        }

        public void LoadGraph()
        {

        }

        public void SaveGraph()
        {

        }

        public void PostProcessing()
        {
            _exhaustiveApproach.Graph = RedundancyRemover.RemoveRedundancy(_exhaustiveApproach.Graph);
            _exhaustiveApproach.PostProcessing();
            OnPropertyChanged("CurrentGraphString");
            DisableGui();
        }

        private void DisableGui()
        {
            IsGuiEnabled = false;
            CurrentTraceBeingAdded = new LogTrace();
        }

        #endregion

        public void ActivityButtonClicked(string buttonContentName)
        {
            if (IsGuiEnabled)
            {
                var activity = Activities.ToList().Find(x => x.Id == buttonContentName);
                if (activity != null)
                {
                    CurrentTraceBeingAdded.Add(new LogEvent(activity.Id, activity.Name));
                    OnPropertyChanged("CurrentTraceBeingAddedString");
                }
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
