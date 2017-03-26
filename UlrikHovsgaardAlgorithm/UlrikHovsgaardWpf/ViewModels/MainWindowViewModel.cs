using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using UlrikHovsgaardAlgorithm;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Mining;
using UlrikHovsgaardAlgorithm.QualityMeasures;
using UlrikHovsgaardAlgorithm.RedundancyRemoval;
using UlrikHovsgaardWpf.Data;
using UlrikHovsgaardWpf.Utils;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows;
using SharpVectors.Converters;
using UlrikHovsgaardWpf.Views;
using MessageBox = System.Windows.Forms.MessageBox;

namespace UlrikHovsgaardWpf.ViewModels
{
    public delegate void OpenStartOptions(StartOptionsWindowViewModel viewModel);

    public class MainWindowViewModel : SuperViewModel
    {
        public event OpenStartOptions OpenStartOptionsEvent;

        public event Action RefreshDataContainer;
        public event Action<int> SelectTraceByIndex;
        public event Action RefreshImageBorder;

        #region Fields

        private DcrGraph _graphToDisplay;
        private ContradictionApproach _contradictionApproach;
        private RedundancyRemover _redundancyRemover;
        private DcrGraph _postProcessingResultJustDone;
        private BackgroundWorker _bgWorker;

        private StatisticsWindowViewModel _statisticsViewModel;

        #endregion

        #region Properties

        private ObservableCollection<Activity> _activities;
        private ObservableCollection<ActivityNameWrapper> _activityButtons;
        private TrulyObservableCollection<LogTrace> _entireLog; 
        private bool _isTraceAdditionAllowed;
        private LogTrace _selectedTrace;
        private string _tracesToGenerate;
        private bool _performPostProcessing;
        private DrawingImage _currentGraphImage;
        private bool _isImageLargerThanBorder;
        private bool _isWaiting;
        private string _waitingProgressMessage;
        private int _maxProgressSteps;
        private int _progessStepAmount;

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
        public DrawingImage CurrentGraphImage { get { _currentGraphImage?.Freeze(); return _currentGraphImage; } set { _currentGraphImage = value; OnPropertyChanged(); } }
        public bool IsImageLargerThanBorder { get { return _isImageLargerThanBorder; } set { _isImageLargerThanBorder = value; OnPropertyChanged(); } }

        public string QualityDimensions
        {
            get
            {
                IsWaiting = true;
                ProcessUITasks();
                var res = QualityDimensionRetriever.Retrieve(GraphToDisplay, new Log {Traces = EntireLog.ToList()}).ToString();
                IsWaiting = false;
                return res;
            }
        }

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
        public bool IsWaiting { get { return _isWaiting; } set { _isWaiting = value; OnPropertyChanged(); WaitingProgressMessage = "Processing, please wait..."; } }
        public string WaitingProgressMessage { get { return _waitingProgressMessage; } set { _waitingProgressMessage = value; OnPropertyChanged(); ProcessUITasks(); } }
        public int MaxProgressSteps { get { return _maxProgressSteps; } set { _maxProgressSteps = value; OnPropertyChanged(); } }
        public int ProgressStepAmount { get { return _progessStepAmount; } set { _progessStepAmount = value; OnPropertyChanged(); } }


        #region Private properties

        private DcrGraph GraphToDisplay { get { return _graphToDisplay; } set { _graphToDisplay = value; UpdateGraphImage(); } }

        private Dispatcher Dispatcher { get; set; }

        #endregion

        #region Commands
        
        private ICommand _newTraceCommand;
        private ICommand _finishTraceCommand;
        private ICommand _saveLogCommand;
        private ICommand _autoGenLogCommand;
        private ICommand _resetCommand;
        private ICommand _saveGraphCommand;
        private ICommand _updateQualityDimensionsCommand;
        private ICommand _cancelProcessingCommand;
        private ICommand _thresholdChangedCommand;
        private ICommand _showStatsCommand;
        
        public ICommand NewTraceCommand { get { return _newTraceCommand; } set { _newTraceCommand = value; OnPropertyChanged(); } }
        public ICommand FinishTraceCommand { get { return _finishTraceCommand; } set { _finishTraceCommand = value; OnPropertyChanged(); } }
        public ICommand SaveLogCommand { get { return _saveLogCommand; } set { _saveLogCommand = value; OnPropertyChanged(); } }
        public ICommand AutoGenLogCommand { get { return _autoGenLogCommand; } set { _autoGenLogCommand = value; OnPropertyChanged(); } }
        public ICommand ResetCommand { get { return _resetCommand; } set { _resetCommand = value; OnPropertyChanged(); } }
        public ICommand SaveGraphCommand { get { return _saveGraphCommand; } set { _saveGraphCommand = value; OnPropertyChanged(); } }
        public ICommand UpdateQualityDimensionsCommand { get { return _updateQualityDimensionsCommand; } set { _updateQualityDimensionsCommand = value; OnPropertyChanged(); } }
        public ICommand CancelProcessingCommand { get { return _cancelProcessingCommand; } set { _cancelProcessingCommand = value; OnPropertyChanged(); } }
        public ICommand ThresholdChangedCommand { get { return _thresholdChangedCommand; } set { _thresholdChangedCommand = value; OnPropertyChanged(); } }
        public ICommand ShowStatsCommand { get { return _showStatsCommand; } set { _showStatsCommand = value; OnPropertyChanged(); } }

        #endregion

        #endregion

        public MainWindowViewModel()
        {
            Dispatcher = Dispatcher.CurrentDispatcher;
            SetUpCommands();
        }

        public void UpdateGraph()
        {
            _postProcessingResultJustDone = null;
            if (PerformPostProcessing)
            {
                PostProcessing();
                _postProcessingResultJustDone = GraphToDisplay;
            }
            else
            {
                GraphToDisplay = _contradictionApproach.Graph;
            }

            if (_statisticsViewModel == null) _statisticsViewModel = new StatisticsWindowViewModel(GraphToDisplay);
            _statisticsViewModel.RefreshStatisticsTextBox();
        }

        public void Init()
        {
            Activities = new ObservableCollection<Activity>();

            _contradictionApproach = new ContradictionApproach(new HashSet<Activity>(Activities));
            ContradictionApproach.PostProcessingResultEvent += UpdateGraphWithPostProcessingResult;

            _redundancyRemover = new RedundancyRemover();
            _redundancyRemover.ReportProgress += ProgressMadeInRedundancyRemover;
            
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
            UpdateQualityDimensionsCommand = new ButtonActionCommand(UpdateQualityDimensions);
            CancelProcessingCommand = new ButtonActionCommand(CancelBackgroundWorker);
            ThresholdChangedCommand = new ButtonActionCommand(UpdateGraph);
            ShowStatsCommand = new ButtonActionCommand(ShowStats);
        }

        #region State initialization procedures

        private void SetUpWithLog(Log log)
        {
            IsWaiting = true;
            // TODO: _contradictionApproach.AddLog(log); - then add to GUI list etc? - test effectiveness - probably same deal
            Activities = new ObservableCollection<Activity>(log.Alphabet.Select(x => new Activity(x.IdOfActivity, x.Name){ Roles = x.ActorName }));
            foreach (var activity in Activities)
            {
                ActivityButtons.Add(new ActivityNameWrapper(activity.Id));
            }
            _contradictionApproach = new ContradictionApproach(new HashSet<Activity>(Activities));
            ContradictionApproach.PostProcessingResultEvent += UpdateGraphWithPostProcessingResult;

            foreach (var logTrace in log.Traces)
            {
                AddFinishedTraceFromLog(logTrace);
            }
            RefreshLogTraces();
            UpdateGraph();
            IsWaiting = false;
        }

        private void SetUpWithAlphabet(int sizeOfAlphabet)
        {
            IsWaiting = true;
            var a = 'A';
            for (int i = 0; i < sizeOfAlphabet; i++)
            {
                var currId = "" + Convert.ToChar(a + i);
                Activities.Add(new Activity(currId, string.Format("Activity {0}", currId)));
            }
            foreach (var activity in Activities)
            {
                ActivityButtons.Add(new ActivityNameWrapper(activity.Id));
            }
            _contradictionApproach = new ContradictionApproach(new HashSet<Activity>(Activities));
            ContradictionApproach.PostProcessingResultEvent += UpdateGraphWithPostProcessingResult;
            UpdateGraph();
            IsWaiting = false;
        }

        private void SetUpWithGraph(DcrGraph graph)
        {
            IsWaiting = true;
            _contradictionApproach.Graph = graph;
            Activities = new ObservableCollection<Activity>(_contradictionApproach.Graph.Activities);
            UpdateGraph();
            // Lock GUI... Given graph, can Autogen Log. Cannot make own! Can save stuff. Can restart. Can post-process
            DisableTraceBuilding();
            IsWaiting = false;
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
            _contradictionApproach.AddTrace(trace); // Actually adds finished trace (stops it immediately)
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
                if (_contradictionApproach.AddEvent(activity.Id, SelectedTrace.Id)) // "activity" run on instance "SelectedTrace"
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

            if (_contradictionApproach.Stop(SelectedTrace.Id))
            {
                // Graph was altered as a result of stopping the trace
                UpdateGraph();
            }
        }

        private void Reset()
        {
            Init();
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
                var logGen = new LogGenerator9001(20, _contradictionApproach.Graph);
                var log = logGen.GenerateLog(amount);
                AppendLogAndUpdate(log);
            }
            else
            {
                MessageBox.Show("Please enter an integer value.", "Error");
            }
        }

        public async void SaveLog()
        {
            var dialog = new SaveFileDialog();
            dialog.Title = "Save log";
            dialog.FileName = "UlrikHøvsgaard log" + DateTime.Now.Date.ToString("dd-MM-yyyy");
            dialog.InitialDirectory = Environment.SpecialFolder.MyDocuments.ToString();
            dialog.Filter = "XML files (*.xml)|*.xml";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var logXml = Log.ExportToXml(new Log
                {
                    Id = Path.GetFileNameWithoutExtension(dialog.FileName),
                    Traces = EntireLog.ToList()
                });

                using (var sw = new StreamWriter(dialog.FileName))
                {
                    await sw.WriteLineAsync(logXml);
                }
            }
        }

        public void SaveGraph()
        {
            var dialog = new SaveFileDialog();
            dialog.Title = "Save graph";
            dialog.FileName = "UlrikHøvsgaard graph " + DateTime.Now.Date.ToString("dd-MM-yyyy");
            dialog.InitialDirectory = Environment.SpecialFolder.MyDocuments.ToString();
            dialog.Filter = "XML files (*.xml)|*.xml";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                using (StreamWriter sw = new StreamWriter(dialog.FileName))
                {
                    sw.WriteLine(GraphToDisplay.ExportToXml());
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
                }
                else
                {
                    GraphToDisplay = _postProcessingResultJustDone;
                }
            }
            else
            {
                GraphToDisplay = _contradictionApproach.Graph;
            }
            System.Threading.Thread.Sleep(50);
            RefreshImageBorder?.Invoke();
        }

        private void UpdateQualityDimensions()
        {
            OnPropertyChanged("QualityDimensions");
        }

        private void CancelBackgroundWorker()
        {
            _bgWorker.CancelAsync();
        }
        private void ShowStats()
        {
            // TODO: Open window incl. SYNC || Show as UserControl in same window as overlay
            var wdw = new StatisticsWindow(_statisticsViewModel);
            wdw.Show();
        }

        #endregion

        #region Helper methods

        private async void UpdateGraphImage()
        {
            try
            {

                var image = await GraphImageRetriever.Retrieve(GraphToDisplay);
                if (image != null)
                {
                    IsImageLargerThanBorder = image.Height > 508 || image.Width > 1034;
                    Dispatcher.Invoke(() => {
                        CurrentGraphImage = image;
                    });
                }

            }
            catch (WebException e)
            {
                //TODO: display error message.

            }
        }

        private void PostProcessing()
        {
            IsWaiting = true;

            _bgWorker = new BackgroundWorker();
            _bgWorker.WorkerSupportsCancellation = true;
            _bgWorker.WorkerReportsProgress = true;

            _bgWorker.DoWork += bw_DoWork;
            _bgWorker.ProgressChanged += bw_ProgressChanged;
            _bgWorker.RunWorkerCompleted += bw_RunWorkerCompleted;

            WaitingProgressMessage = "Finding unique traces for original graph...";
            MaxProgressSteps = _contradictionApproach.Graph.GetRelationCount;
            ProgressStepAmount = 0;
            ProcessUITasks();

            _bgWorker.RunWorkerAsync();

            //WaitingProgressMessage = "Processing, please wait...";
        }

        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (IsTraceAdditionAllowed)
                {
                    // Make a copy of Condradiction Approach where all traces are finished, to avoid potential Response cycles
                    var contrCopy = new ContradictionApproach(_contradictionApproach.Graph.GetActivities());
                    contrCopy.AddLog(new Log {Traces = _entireLog.ToList()});
                    var redundancyRemovedGraph = _redundancyRemover.RemoveRedundancy(contrCopy.Graph);
                    ContradictionApproach.PostProcessing(redundancyRemovedGraph);
                }
                else // Signifies that the program was initiated with a loaded graph
                {
                    var redundancyRemovedGraph = _redundancyRemover.RemoveRedundancy(GraphToDisplay);
                    ContradictionApproach.PostProcessing(redundancyRemovedGraph);
                }
            });
        }

        private void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // Gets a percentage string TODO: Update progressbar instead?
            //this.tbProgress.Text = (e.ProgressPercentage.ToString() + "%");
        }

        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // TODO: Progress simply stopped regardless?
            _postProcessingResultJustDone = GraphToDisplay;
            IsWaiting = false;
            //if ((e.Cancelled == true))
            //{
            //    this.tbProgress.Text = "Canceled!";
            //}

            //else if (!(e.Error == null))
            //{
            //    this.tbProgress.Text = ("Error: " + e.Error.Message);
            //}

            //else
            //{
            //    this.tbProgress.Text = "Done!";
            //}
        }

        private void WorkPerformed()
        {
            _bgWorker.ReportProgress((ProgressStepAmount / MaxProgressSteps));
        }

        // Event listener
        private void ProgressMadeInRedundancyRemover(string progressMessage)
        {
            _bgWorker.ReportProgress((ProgressStepAmount / MaxProgressSteps));
            ProgressStepAmount++;
            WaitingProgressMessage = progressMessage;
            ProcessUITasks();
        }

        private void UpdateGraphWithPostProcessingResult(DcrGraph postProcessedGraph)
        {
            Dispatcher.Invoke(() =>
            {
                GraphToDisplay = postProcessedGraph.Copy();
            });
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
    }
}
