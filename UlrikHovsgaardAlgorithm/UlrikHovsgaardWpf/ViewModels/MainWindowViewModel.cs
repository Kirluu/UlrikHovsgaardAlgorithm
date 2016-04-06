using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Mining;
using UlrikHovsgaardAlgorithm.Parsing;
using UlrikHovsgaardAlgorithm.RedundancyRemoval;

namespace UlrikHovsgaardWpf.ViewModels
{
    public delegate void OpenStartOptions(StartOptionsWindowViewModel viewModel);

    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public event OpenStartOptions OpenStartOptionsEvent;

        #region Fields

        private StartOptionsWindow _startOptionsWindow;
        private DcrGraph _graphToDisplay;
        private ExhaustiveApproach _exhaustiveApproach;

        #endregion

        #region Properties

        private ObservableCollection<Activity> _activities;
        private ObservableCollection<ActivityNameWrapper> _activityButtons;
        private ObservableCollection<LogTrace> _currentLog;
        private bool _isGuiEnabled;
        private LogTrace _currentTraceBeingAdded;
        private string _tracesToGenerate;
        private bool _performPostProcessing;

        public ObservableCollection<Activity> Activities { get { return _activities; } set { _activities = value; OnPropertyChanged(); } }
        public ObservableCollection<ActivityNameWrapper> ActivityButtons { get { return _activityButtons; } set { _activityButtons = value; OnPropertyChanged(); } }
        public ObservableCollection<LogTrace> CurrentLog { get { return _currentLog; } set { _currentLog = value; OnPropertyChanged(); } }
        private LogTrace CurrentTraceBeingAdded { get { return _currentTraceBeingAdded; } set { _currentTraceBeingAdded = value; OnPropertyChanged("CurrentTraceBeingAddedString"); } }
        public string CurrentTraceBeingAddedString { get { return _currentTraceBeingAdded.ToString(); } }
        public bool IsGuiEnabled { get { return _isGuiEnabled; } set { _isGuiEnabled = value; OnPropertyChanged(); } }
        public string CurrentGraphString { get { return GraphToDisplay.ToString(); } }
        public string TracesToGenerate { get { return _tracesToGenerate; } set { _tracesToGenerate = value; OnPropertyChanged(); } }
        public bool PerformPostProcessing
        {
            get
            {
                return _performPostProcessing;
            }
            set
            {
                _performPostProcessing = value;
                OnPropertyChanged();
                UpdateGraph();
            }
        }

        #region Private properties

        private DcrGraph GraphToDisplay { get { return _graphToDisplay; } set { _graphToDisplay = value; OnPropertyChanged("CurrentGraphString"); } }

        #endregion

        #region Commands

        private ICommand _addTraceCommand;
        private ICommand _saveLogCommand;
        private ICommand _autoGenLogCommand;
        private ICommand _clearTraceCommand;
        private ICommand _resetCommand;
        private ICommand _saveGraphCommand;
        private ICommand _postProcessingCommand;

        public ICommand AddTraceCommand { get { return _addTraceCommand; } set { _addTraceCommand = value; OnPropertyChanged(); } }
        public ICommand SaveLogCommand { get { return _saveLogCommand; } set { _saveLogCommand = value; OnPropertyChanged(); } }
        public ICommand AutoGenLogCommand { get { return _autoGenLogCommand; } set { _autoGenLogCommand = value; OnPropertyChanged(); } }
        public ICommand ClearTraceCommand { get { return _clearTraceCommand; } set { _clearTraceCommand = value; OnPropertyChanged(); } }
        public ICommand ResetCommand { get { return _resetCommand; } set { _resetCommand = value; OnPropertyChanged(); } }
        public ICommand SaveGraphCommand { get { return _saveGraphCommand; } set { _saveGraphCommand = value; OnPropertyChanged(); } }
        public ICommand PostProcessingCommand { get { return _postProcessingCommand; } set { _postProcessingCommand = value; OnPropertyChanged(); } }

        #endregion

        #endregion

        public MainWindowViewModel()
        {
            SetUpCommands();
        }

        private void UpdateGraph()
        {
            if (PerformPostProcessing)
            {
                PostProcessing();
            }
            else
            {
                GraphToDisplay = _exhaustiveApproach.Graph;
            }
        }

        public void Init()
        {
            Activities = new ObservableCollection<Activity>();

            _exhaustiveApproach = new ExhaustiveApproach(new HashSet<Activity>(Activities));
            UpdateGraph();

            ActivityButtons = new ObservableCollection<ActivityNameWrapper>();

            CurrentLog = new ObservableCollection<LogTrace>();

            PerformPostProcessing = false;

            CurrentTraceBeingAdded = new LogTrace();
            
            var startOptionsViewModel = new StartOptionsWindowViewModel();
            startOptionsViewModel.AlphabetSizeSelected += SetUpWithAlphabet;
            startOptionsViewModel.LogLoaded += SetUpWithLog;
            startOptionsViewModel.DcrGraphLoaded += SetUpWithGraph;
            
            OpenStartOptionsEvent?.Invoke(startOptionsViewModel); // Invoke if not null

            IsGuiEnabled = true;
        }

        private void SetUpCommands()
        {
            AddTraceCommand = new ButtonActionCommand(AddTrace);
            SaveLogCommand = new ButtonActionCommand(SaveLog);
            AutoGenLogCommand = new ButtonActionCommand(AutoGenLog);
            ClearTraceCommand = new ButtonActionCommand(ClearTrace);
            ResetCommand = new ButtonActionCommand(Reset);
            SaveGraphCommand = new ButtonActionCommand(SaveGraph);
            PostProcessingCommand = new ButtonActionCommand(PostProcessing);
        }

        #region Initial state procedures

        private void SetUpWithLog(Log log)
        {
            MessageBox.Show("A log was parsed!");
        }

        private void SetUpWithAlphabet(int sizeOfAlphabet)
        {
            var a = 'A';
            for (int i = 0; i < sizeOfAlphabet; i++)
            {
                var currId = Convert.ToChar(a + i);
                Activities.Add(new Activity(currId+"", string.Format("Activity {0}", currId)));
            }
            foreach (var activity in Activities)
            {
                ActivityButtons.Add(new ActivityNameWrapper(activity.Id));
            }
            _exhaustiveApproach = new ExhaustiveApproach(new HashSet<Activity>(Activities));
            UpdateGraph();
        }

        private void SetUpWithGraph(DcrGraph graph)
        {
            MessageBox.Show(graph.ToString());
        }

        #endregion

        #region Command implementation methods

        public void PerformPostProcessingChanged()
        {
            
        }

        public void AddTrace()
        {
            if (CurrentTraceBeingAdded.Events.Count == 0) return;
            CurrentLog.Add(CurrentTraceBeingAdded.Copy());
            _exhaustiveApproach.AddTrace(CurrentTraceBeingAdded.Copy());
            UpdateGraph();
            ClearTrace();
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
            if (string.IsNullOrEmpty(TracesToGenerate))
                MessageBox.Show("Please enter an integer value.");
            int amount;
            if (int.TryParse(TracesToGenerate, out amount))
            {
                var logGen = new LogGenerator9001(40, _exhaustiveApproach.Graph);
                var log = logGen.GenerateLog(amount);
                CurrentLog = new ObservableCollection<LogTrace>(CurrentLog.Concat(log));
            }
            else
            {
                MessageBox.Show("Please enter an integer value.", "Error");
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
            var redundancyRemovedGraph = RedundancyRemover.RemoveRedundancy(_exhaustiveApproach.Graph);
            GraphToDisplay = ExhaustiveApproach.PostProcessingNotAffectingCurrentGraph(redundancyRemovedGraph);
            //DisableGui();
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
