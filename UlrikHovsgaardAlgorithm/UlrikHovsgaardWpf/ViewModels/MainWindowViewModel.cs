using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using Svg;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Mining;
using UlrikHovsgaardAlgorithm.Parsing;
using UlrikHovsgaardAlgorithm.QualityMeasures;
using UlrikHovsgaardAlgorithm.RedundancyRemoval;
using UlrikHovsgaardWpf.Utils;

namespace UlrikHovsgaardWpf.ViewModels
{
    public delegate void OpenStartOptions(StartOptionsWindowViewModel viewModel);

    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public event OpenStartOptions OpenStartOptionsEvent;
        public event Action RefreshDataContainer;
        public event Action<int> SelectTraceByIndex;

        #region Fields

        private DcrGraph _graphToDisplay;
        private ExhaustiveApproach _exhaustiveApproach;

        #endregion

        #region Properties

        private ObservableCollection<Activity> _activities;
        private ObservableCollection<ActivityNameWrapper> _activityButtons;
        private TrulyObservableCollection<LogTrace> _currentLog;
        private bool _isTraceAdditionAllowed;
        private LogTrace _selectedTrace;
        private string _tracesToGenerate;
        private bool _performPostProcessing;

        public ObservableCollection<Activity> Activities { get { return _activities; } set { _activities = value; OnPropertyChanged(); } }
        public ObservableCollection<ActivityNameWrapper> ActivityButtons { get { return _activityButtons; } set { _activityButtons = value; OnPropertyChanged(); } }
        public TrulyObservableCollection<LogTrace> CurrentLog { get { return _currentLog; } set { _currentLog = value; OnPropertyChanged(); } }
        public LogTrace SelectedTrace { get { return _selectedTrace; } set { _selectedTrace = value; OnPropertyChanged("IsTraceActive"); OnPropertyChanged("SelectedTraceString"); OnPropertyChanged("SelectedTraceId"); } }
        public string SelectedTraceId => SelectedTrace?.Id;
        public string SelectedTraceString => SelectedTrace?.ToString();
        public bool IsTraceAdditionAllowed { get { return _isTraceAdditionAllowed; } set { _isTraceAdditionAllowed = value; OnPropertyChanged(); } }
        public bool IsTraceActive => IsTraceAdditionAllowed && SelectedTrace != null && !SelectedTrace.IsFinished; // If trace addition not allowed, activeness doesn't matter
        public string CurrentGraphString => GraphToDisplay.ToString();
        public string TracesToGenerate { get { return _tracesToGenerate; } set { _tracesToGenerate = value; OnPropertyChanged(); } }
        public string QualityDimensions => QualityDimensionRetriever.Retrieve(GraphToDisplay, new Log {Traces = CurrentLog.ToList()}).ToString();
        public Bitmap CurrentGraphImage => GraphImageRetriever.Retrieve(GraphToDisplay).Draw();

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

        private DcrGraph GraphToDisplay { get { return _graphToDisplay; } set { _graphToDisplay = value; OnPropertyChanged("CurrentGraphString"); OnPropertyChanged("QualityDimensions"); } }

        #endregion

        #region Commands
        
        private ICommand _newTraceCommand;
        private ICommand _finishTraceCommand;
        private ICommand _saveLogCommand;
        private ICommand _autoGenLogCommand;
        private ICommand _resetCommand;
        private ICommand _saveGraphCommand;
        private ICommand _postProcessingCommand;
        
        public ICommand NewTraceCommand { get { return _newTraceCommand; } set { _newTraceCommand = value; OnPropertyChanged(); } }
        public ICommand FinishTraceCommand { get { return _finishTraceCommand; } set { _finishTraceCommand = value; OnPropertyChanged(); } }
        public ICommand SaveLogCommand { get { return _saveLogCommand; } set { _saveLogCommand = value; OnPropertyChanged(); } }
        public ICommand AutoGenLogCommand { get { return _autoGenLogCommand; } set { _autoGenLogCommand = value; OnPropertyChanged(); } }
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

            CurrentLog = new TrulyObservableCollection<LogTrace>();

            PerformPostProcessing = false;

            IsTraceAdditionAllowed = true;
            
            var startOptionsViewModel = new StartOptionsWindowViewModel();
            startOptionsViewModel.AlphabetSizeSelected += SetUpWithAlphabet;
            startOptionsViewModel.LogLoaded += SetUpWithLog;
            startOptionsViewModel.DcrGraphLoaded += SetUpWithGraph;
            
            OpenStartOptionsEvent?.Invoke(startOptionsViewModel);
        }

        private void SetUpCommands()
        {
            NewTraceCommand = new ButtonActionCommand(NewTrace);
            FinishTraceCommand = new ButtonActionCommand(FinishTrace);
            SaveLogCommand = new ButtonActionCommand(SaveLog);
            AutoGenLogCommand = new ButtonActionCommand(AutoGenLog);
            ResetCommand = new ButtonActionCommand(Reset);
            SaveGraphCommand = new ButtonActionCommand(SaveGraph);
            PostProcessingCommand = new ButtonActionCommand(PostProcessing);
        }

        #region State initialization procedures

        private void SetUpWithLog(Log log)
        {
            // TODO: _exhaustiveApproach.AddLog(log); - then add to GUI list etc? - test effectiveness - probably same deal
            _exhaustiveApproach = new ExhaustiveApproach(new HashSet<Activity>(log.Alphabet.Select(x => new Activity(x.IdOfActivity, x.Name))));
            foreach (var logTrace in log.Traces)
            {
                AddFinishedTrace(logTrace);
            }
            UpdateGraph();
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
            _exhaustiveApproach.Graph = graph;
            Activities = new ObservableCollection<Activity>(_exhaustiveApproach.Graph.Activities);
            UpdateGraph();
            // Lock GUI... Given graph, can Autogen Log. Cannot make own! Can save stuff. Can restart. Can post-process
            DisableTraceBuilding();
        }

        #endregion

        private void RefreshLogTraces()
        {
            OnPropertyChanged("CurrentLog");
            RefreshDataContainer?.Invoke(); // Inform View to update container of data
        }

        #region Command implementation methods

        /// <summary>
        /// Used when adding a full log of traces (All finished from the start)
        /// </summary>
        private void AddFinishedTrace(LogTrace logTrace) // TODO: Use at AddLog
        {
            if (logTrace.Events.Count == 0) return;
            var trace = logTrace.Copy();
            trace.IsFinished = true;
            if (string.IsNullOrEmpty(trace.Id))
            {
                trace.Id = Guid.NewGuid().ToString();
            }
            CurrentLog.Add(trace);
            _exhaustiveApproach.AddTrace(trace); // Actually adds finished trace (stops it immediately)
        }

        /// <summary>
        /// Adds a new, empty trace and selects it
        /// </summary>
        public void NewTrace()
        {
            var trace = new LogTrace();
            trace.Id = Guid.NewGuid().ToString();
            trace.EventAdded += RefreshLogTraces; // Register listening for changes to update grid when event added
            CurrentLog.Add(trace);

            RefreshDataContainer?.Invoke();
            // TODO: Programmatically select trace (focus)
            SelectedTrace = CurrentLog.First(t => t.Equals(trace));
            //SelectTraceByIndex?.Invoke(CurrentLog.IndexOf(trace));

        }


        // TODO: Add event into trace in list?
        /// <summary>
        /// Called by GUI code-behind when an event should be added to SelectedTrace
        /// </summary>
        /// <param name="buttonContentName"></param>
        public void ActivityButtonClicked(string buttonContentName)
        {
            if (IsTraceAdditionAllowed) // TODO: Maybe unnecessary - check if activity buttons are disabled based on binding or not
            {
                var activity = Activities.ToList().Find(x => x.Id == buttonContentName);
                if (activity != null)
                {
                    // TODO: Find trace in list
                    var traceInList = CurrentLog.First(t => t.Id == SelectedTraceId);
                    traceInList.Add(new LogEvent(activity.Id, activity.Name));
                    //SelectedTrace.Add(new LogEvent(activity.Id, activity.Name));
                    _exhaustiveApproach.AddEvent(activity.Id, SelectedTrace.Id); // "activity" run on instance "SelectedTrace"
                    OnPropertyChanged("SelectedTraceString");
                    //OnPropertyChanged("CurrentLog");
                    UpdateGraph();
                }
            }
        }

        /// <summary>
        /// Ends the currently selected trace, disabling further addition of events
        /// </summary>
        public void FinishTrace()
        {
            SelectedTrace.IsFinished = true;
            RefreshLogTraces(); // To reflect finished state
            OnPropertyChanged("IsTraceActive");
            SelectedTrace.EventAdded -= RefreshLogTraces; // Unregister event
            _exhaustiveApproach.Stop(SelectedTrace.Id);
            UpdateGraph();
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
                    sw.WriteLine(
                        Log.ExportToXml(new Log
                        {
                            Id = Path.GetFileNameWithoutExtension(dialog.FileName),
                            Traces = new List<LogTrace>(CurrentLog)
                        }));
                }
            }
        }

        /// <summary>
        /// Builds an amount of finished traces
        /// </summary>
        public void AutoGenLog()
        {
            if (string.IsNullOrEmpty(TracesToGenerate))
                MessageBox.Show("Please enter an integer value.");
            int amount;
            if (int.TryParse(TracesToGenerate, out amount))
            {
                var logGen = new LogGenerator9001(40, _exhaustiveApproach.Graph);
                var log = logGen.GenerateLog(amount);
                CurrentLog = new TrulyObservableCollection<LogTrace>(CurrentLog.Concat(log));
            }
            else
            {
                MessageBox.Show("Please enter an integer value.", "Error");
            }
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
        }

        private void DisableTraceBuilding()
        {
            IsTraceAdditionAllowed = false;
            SelectedTrace = new LogTrace();
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        //[NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
