﻿using System;
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
        private ICommand _postProcessingCommand;

        #endregion

        private LogTrace _currentTraceBeingAdded;
        private ExhaustiveApproach _exhaustiveApproach;


        #endregion

        #region Properties

        public ObservableCollection<Activity> Activities { get { return _activities; } set { _activities = value; OnPropertyChanged(); } }
        public ObservableCollection<CommandWrapper> ActivityButtons { get { return _activityButtons; } set { _activityButtons = value; OnPropertyChanged(); } }
        public ObservableCollection<LogTrace> CurrentLog { get { return _currentLog; } set { _currentLog = value; OnPropertyChanged(); } }
        public string CurrentTraceBeingAddedString { get { return _currentTraceBeingAdded.ToString(); } }

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

            ActivityButtons = new ObservableCollection<CommandWrapper>();
            foreach (var activity in Activities)
            {
                ActivityButtons.Add(new CommandWrapper(new ButtonActionCommand(DummyMethod), activity.Id));
            }

            CurrentLog = new ObservableCollection<LogTrace>();

            AddActivityId = null;
            AddActivityName = null;

            _currentTraceBeingAdded = new LogTrace();
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

        public void DummyMethod() {} // TODO: Stop using CommandWrapper eventually...


        #region Command implementation methods

        public void AddTrace()
        {
            CurrentLog.Add(_currentTraceBeingAdded.Copy());
            _exhaustiveApproach.AddTrace(_currentTraceBeingAdded.Copy());
            OnPropertyChanged("CurrentGraphString");
            ClearTrace();
        }

        public void AddActivity()
        {
            // TODO: Need on the fly activity addition to ExhaustiveApproach
            Activities.Add(new Activity(AddActivityId, AddActivityName));
            ActivityButtons.Add(new CommandWrapper(new ButtonActionCommand(DummyMethod), AddActivityId));
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
            _currentTraceBeingAdded = new LogTrace();
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
            // TODO: Lock GUI
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
