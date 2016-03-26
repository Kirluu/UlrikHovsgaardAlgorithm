using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
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
        private string _tracesToGenerate;
        private LogChoices _logChosen;
        private string _selectedLogFileName;
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
        private string _selectedLogFilePath;


        #endregion

        public enum LogChoices { Hospital, BpiChallenge2015, BpiChallenge2016, BrowsedFile }

        #region Properties

        public ObservableCollection<Activity> Activities { get { return _activities; } set { _activities = value; OnPropertyChanged(); } }
        public ObservableCollection<ActivityNameWrapper> ActivityButtons { get { return _activityButtons; } set { _activityButtons = value; OnPropertyChanged(); } }
        public ObservableCollection<LogTrace> CurrentLog { get { return _currentLog; } set { _currentLog = value; OnPropertyChanged(); } }
        private LogTrace CurrentTraceBeingAdded { get { return _currentTraceBeingAdded; } set { _currentTraceBeingAdded = value; OnPropertyChanged("CurrentTraceBeingAddedString"); } }
        public string CurrentTraceBeingAddedString { get { return _currentTraceBeingAdded.ToString(); } }
        public bool IsGuiEnabled { get { return _isGuiEnabled; } set { _isGuiEnabled = value; OnPropertyChanged(); } }
        public string CurrentGraphString { get { return _exhaustiveApproach.Graph.ToString(); } }
        public string TracesToGenerate { get { return _tracesToGenerate; } set { _tracesToGenerate = value; OnPropertyChanged(); } }
        public LogChoices LogChosen { get { return _logChosen; } set { _logChosen = value; OnPropertyChanged(); } }
        public string SelectedLogFileName { get { return _selectedLogFileName; } set { _selectedLogFileName = value; OnPropertyChanged(); } }

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

            SelectedLogFileName = "Select a file...";

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
            var dialog = new SaveFileDialog();
            dialog.Title = "Save log file";
            dialog.FileName = "log " + DateTime.Now.Date.ToString("dd-MM-yyyy");
            dialog.InitialDirectory = Environment.SpecialFolder.MyDocuments.ToString();
            dialog.Filter = "XML files (*.xml)|*.xml";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                using (StreamWriter sw = new StreamWriter(dialog.FileName))
                {
                    foreach (var logTrace in CurrentLog)
                    {
                        sw.WriteLine(logTrace.ToString());
                    }
                }
            }
        }

        public void AutoGenLog()
        {
            int amount;
            if (int.TryParse(TracesToGenerate, out amount))
            {
                var logGen = new LogGenerator9001(40, _exhaustiveApproach.Graph);
                var log = logGen.GenerateLog(amount);
                CurrentLog = new ObservableCollection<LogTrace>(CurrentLog.Concat(log));
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Skriv venligst en talværdi", "Fejl");
            }
        }

        public void BrowseLog()
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Select a log file";
            dialog.Filter = "XML files (*.xml)|*.xml";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _selectedLogFilePath = dialog.FileName;
                SelectedLogFileName = Path.GetFileNameWithoutExtension(dialog.FileName);
            }
        }

        public void AddLog()
        {
            // TODO: Figure out log format to read and implement it
            switch (LogChosen)
            {
                case LogChoices.Hospital:
                    System.Windows.Forms.MessageBox.Show("Hospital");
                    break;
                case LogChoices.BpiChallenge2015:
                    System.Windows.Forms.MessageBox.Show("BpiChallenge2015");
                    break;
                case LogChoices.BpiChallenge2016:
                    System.Windows.Forms.MessageBox.Show("BpiChallenge2016");
                    break;
                case LogChoices.BrowsedFile:
                    if (_selectedLogFilePath != null)
                    {
                        System.Windows.Forms.MessageBox.Show(_selectedLogFilePath);
                    }
                    else
                    {
                        System.Windows.Forms.MessageBox.Show("Please select a file to be added.");
                    }
                    break;
            }
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
            // TODO: Implement ability to create graph FROM xml
        }

        public void SaveGraph()
        {
            var dialog = new SaveFileDialog();
            dialog.Title = "Save log file";
            dialog.FileName = "log " + DateTime.Now.Date.ToString("dd-MM-yyyy");
            dialog.InitialDirectory = Environment.SpecialFolder.MyDocuments.ToString();
            dialog.Filter = "XML files (*.xml)|*.xml";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                using (StreamWriter sw = new StreamWriter(dialog.FileName))
                {
                    sw.WriteLine(_exhaustiveApproach.Graph.ExportToXml());
                }
            }
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
