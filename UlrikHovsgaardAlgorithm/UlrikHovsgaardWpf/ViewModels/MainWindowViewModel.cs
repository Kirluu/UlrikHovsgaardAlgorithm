using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Imaging;
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
        private RedundancyRemover _redundancyRemover;
        private DcrGraph _postProcessingResultJustDone;

        #endregion

        #region Properties

        private ObservableCollection<Activity> _activities;
        private ObservableCollection<ActivityNameWrapper> _activityButtons;
        private TrulyObservableCollection<LogTrace> _entireLog; 
        private bool _isTraceAdditionAllowed;
        private LogTrace _selectedTrace;
        private string _tracesToGenerate;
        private bool _performPostProcessing;
        private BitmapImage _currentGraphImage;

        public ObservableCollection<Activity> Activities { get { return _activities; } set { _activities = value; OnPropertyChanged(); } }
        public ObservableCollection<ActivityNameWrapper> ActivityButtons { get { return _activityButtons; } set { _activityButtons = value; OnPropertyChanged(); } }
        public TrulyObservableCollection<LogTrace> EntireLog { get { return _entireLog; } set { _entireLog = value; OnPropertyChanged(); } }
        //public TrulyObservableCollection<LogTrace> CurrentLog
        //    =>
        //        _entireLog.Count >= 100
        //            ? new TrulyObservableCollection<LogTrace>(_entireLog.ToList().GetRange(_entireLog.Count - 101, 100)) // Last 100
        //            : new TrulyObservableCollection<LogTrace>(_entireLog.ToList().GetRange(0, _entireLog.Count)); // All elements
        public LogTrace SelectedTrace { get { return _selectedTrace; } set { _selectedTrace = value; OnPropertyChanged("IsTraceActive"); OnPropertyChanged("SelectedTraceString"); OnPropertyChanged("SelectedTraceId"); } }
        public string SelectedTraceId => SelectedTrace?.Id;
        public string SelectedTraceString => SelectedTrace?.ToString();
        public bool IsTraceAdditionAllowed { get { return _isTraceAdditionAllowed; } set { _isTraceAdditionAllowed = value; OnPropertyChanged(); } }
        public bool IsTraceActive => IsTraceAdditionAllowed && SelectedTrace != null && !SelectedTrace.IsFinished; // If trace addition not allowed, activeness doesn't matter
        public string CurrentGraphString => GraphToDisplay.ToString();
        public string TracesToGenerate { get { return _tracesToGenerate; } set { _tracesToGenerate = value; OnPropertyChanged(); } }
        public BitmapImage CurrentGraphImage { get { return _currentGraphImage; } set { _currentGraphImage = value; OnPropertyChanged(); } }
        public string QualityDimensions => QualityDimensionRetriever.Retrieve(GraphToDisplay, new Log {Traces = EntireLog.ToList()}).ToString();
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
                PerformPostProcessingIfNecessary();
            }
        }

        #region Private properties

        private DcrGraph GraphToDisplay { get { return _graphToDisplay; } set { _graphToDisplay = value; OnPropertyChanged("QualityDimensions"); UpdateGraphImage(); } }

        #endregion

        #region Commands
        
        private ICommand _newTraceCommand;
        private ICommand _finishTraceCommand;
        private ICommand _saveLogCommand;
        private ICommand _autoGenLogCommand;
        private ICommand _resetCommand;
        private ICommand _saveGraphCommand;
        
        public ICommand NewTraceCommand { get { return _newTraceCommand; } set { _newTraceCommand = value; OnPropertyChanged(); } }
        public ICommand FinishTraceCommand { get { return _finishTraceCommand; } set { _finishTraceCommand = value; OnPropertyChanged(); } }
        public ICommand SaveLogCommand { get { return _saveLogCommand; } set { _saveLogCommand = value; OnPropertyChanged(); } }
        public ICommand AutoGenLogCommand { get { return _autoGenLogCommand; } set { _autoGenLogCommand = value; OnPropertyChanged(); } }
        public ICommand ResetCommand { get { return _resetCommand; } set { _resetCommand = value; OnPropertyChanged(); } }
        public ICommand SaveGraphCommand { get { return _saveGraphCommand; } set { _saveGraphCommand = value; OnPropertyChanged(); } }

        #endregion

        #endregion

        public MainWindowViewModel()
        {
            SetUpCommands();
        }

        private void UpdateGraph()
        {
            _postProcessingResultJustDone = null;
            if (PerformPostProcessing)
            {
                PostProcessing();
                _postProcessingResultJustDone = GraphToDisplay;
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

            _redundancyRemover = new RedundancyRemover();

            ActivityButtons = new ObservableCollection<ActivityNameWrapper>();

            EntireLog = new TrulyObservableCollection<LogTrace>();

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
        }

        #region State initialization procedures

        private void SetUpWithLog(Log log)
        {
            // TODO: _exhaustiveApproach.AddLog(log); - then add to GUI list etc? - test effectiveness - probably same deal
            Activities = new ObservableCollection<Activity>(log.Alphabet.Select(x => new Activity(x.IdOfActivity, x.Name)));
            foreach (var activity in Activities)
            {
                ActivityButtons.Add(new ActivityNameWrapper(activity.Id));
            }
            _exhaustiveApproach = new ExhaustiveApproach(new HashSet<Activity>(Activities));

            foreach (var logTrace in log.Traces)
            {
                AddFinishedTraceFromLog(logTrace);
            }
            RefreshLogTraces();
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
            OnPropertyChanged("EntireLog");
            RefreshDataContainer?.Invoke(); // Inform View to update container of data
        }

        #region Command implementation methods

        /// <summary>
        /// Used when adding a full log of traces (All finished from the start)
        /// </summary>
        private void AddFinishedTraceFromLog(LogTrace logTrace) // TODO: Use at AddLog
        {
            if (logTrace.Events.Count == 0) return;
            var trace = logTrace.Copy();
            trace.IsFinished = true;
            if (string.IsNullOrEmpty(trace.Id))
            {
                trace.Id = Guid.NewGuid().ToString();
            }
            EntireLog.Add(trace);
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
            EntireLog.Add(trace);

            // Programmatically select trace (focus)
            SelectedTrace = EntireLog[EntireLog.Count - 1]; // last element
            RefreshLogTraces();
            SelectTraceByIndex?.Invoke(EntireLog.Count - 1);
        }

        /// <summary>
        /// Called by GUI code-behind when an event should be added to SelectedTrace
        /// </summary>
        /// <param name="buttonContentName"></param>
        public void ActivityButtonClicked(string buttonContentName)
        {
                var activity = Activities.ToList().Find(x => x.Id == buttonContentName);
                if (activity != null)
                {
                SelectedTrace.Add(new LogEvent(activity.Id, activity.Name));
                    OnPropertyChanged("SelectedTraceString");
                if (_exhaustiveApproach.AddEvent(activity.Id, SelectedTrace.Id)) // "activity" run on instance "SelectedTrace"
                {
                    // Graph was altered as a result of the added event
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
            // Reflect finished state
            RefreshLogTraces();
            // Unregister event
            SelectedTrace.EventAdded -= RefreshLogTraces;

            if (_exhaustiveApproach.Stop(SelectedTrace.Id))
            {
                // Graph was altered as a result of stopping the trace
            UpdateGraph();
        }
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
                            Traces = EntireLog.ToList()
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
                AppendLogAndUpdate(log);
            }
            else
            {
                MessageBox.Show("Please enter an integer value.", "Error");
            }
        }

        private void Reset()
        {
            Init();
        }

        private void SaveGraph()
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

        /// <summary>
        /// Fired when "Perform post-processing" checkbox is clicked
        /// </summary>
        private void PerformPostProcessingIfNecessary()
        {
            if (PerformPostProcessing)
            {
                if (_postProcessingResultJustDone == null) // If not just done, do it
                {
                    PostProcessing();
                     _postProcessingResultJustDone = GraphToDisplay;
                }
                else
                {
                    GraphToDisplay = _postProcessingResultJustDone;
                }
            }
            else
            {
                GraphToDisplay = _exhaustiveApproach.Graph;
            }
        }

        #endregion

        #region Helper methods

        private async void UpdateGraphImage()
        {
            var image = await GraphImageRetriever.Retrieve(GraphToDisplay);
            if (image != null)
            {
                CurrentGraphImage = image;
            }
        }

        private void PostProcessing()
        {
            var redundancyRemovedGraph = _redundancyRemover.RemoveRedundancy(_exhaustiveApproach.Graph);
            GraphToDisplay = ExhaustiveApproach.PostProcessingWithTraceFinder(redundancyRemovedGraph, _redundancyRemover.UniqueTraceFinder); // Reuse traces found in RedundancyRemover
        }

        private void DisableTraceBuilding()
        {
            IsTraceAdditionAllowed = false;
            SelectedTrace = new LogTrace();
        }

        /// <summary>
        /// Only to be used when AutoLogGenerating
        /// </summary>
        /// <param name="log"></param>
        private void AppendLogAndUpdate(List<LogTrace> log)
        {
            EntireLog = new TrulyObservableCollection<LogTrace>(EntireLog.ToList().Concat(log));
            UpdateCurrentLogIfNeeded();
        }

        private void UpdateCurrentLogIfNeeded()
        {
            OnPropertyChanged("EntireLog");
            //if (CurrentLog.Count < 100)
            //{
            //    OnPropertyChanged("CurrentLog"); // Display the new trace
            //}
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
