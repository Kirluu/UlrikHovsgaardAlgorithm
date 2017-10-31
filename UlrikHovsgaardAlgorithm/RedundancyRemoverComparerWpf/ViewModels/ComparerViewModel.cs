using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using RedundancyRemoverComparerWpf.DataClasses;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Datamodels;
using UlrikHovsgaardAlgorithm.Export;
using UlrikHovsgaardAlgorithm.Parsing;
using UlrikHovsgaardAlgorithm.RedundancyRemoval;
using UlrikHovsgaardAlgorithm.Utils;
using UlrikHovsgaardWpf.Data;
using UlrikHovsgaardWpf.ViewModels;

namespace RedundancyRemoverComparerWpf.ViewModels
{
    public class ComparerViewModel : SuperViewModel
    {
        public enum GraphDisplayMode { Original, FullyRedundancyRemoved, OvershootContext, CriticalErrorContext, PatternResultFullyRedundancyRemoved }

        private bool _settingUp;

        private DcrGraphSimple _preRedRemGraph;
        private DcrGraph _fullyRedRemGraph;
        private DcrGraphSimple _patternRedRemGraph;
        private List<RedundancyEvent> _allResults;
        private Dictionary<int, List<RedundantRelationEvent>> _roundToRelationsRemovedDict;
        private DrawingImage _patternGraphImage;
        private DrawingImage _otherGraphImage;
        private DcrGraphSimple _otherGraphImageGraph;

        private RedundantRelationEvent _selectedErrorRelation;
        private GraphDisplayMode _graphToDisplay;
        private List<TestableGraph> _testableGraphs;
        private TestableGraph _testableGraphSelected;

        private HashSet<Relation> _missingRedundantRelations;

        private RedundancyRemoverComparer _comparer = new RedundancyRemoverComparer();

        public Dispatcher Dispatcher { get; private set; }

        public ComparerViewModel()
        {
            Dispatcher = Dispatcher.CurrentDispatcher;
        }

        public void SetUpInitialSettings()
        {
            _settingUp = true;

            // Initialize any source-collections for availability in UI
            var initialOption = new TestableGraph("Select a test-case...", null);
            TestableGraphs = new List<TestableGraph>
            {
                initialOption,
                new TestableGraph("Mortgage application mined graph", XmlParser.ParseDcrGraph(Properties.Resources.mortgageGRAPH)),
                new TestableGraph("9 activities N-squared inclusion-relations", XmlParser.ParseDcrGraph(Properties.Resources.AllInclusion9ActivitiesGraph)),
                new TestableGraph("'Repair example' log mined by DCR-miner", XmlParser.ParseDcrGraph(Properties.Resources.repairExample_Mined)), // http://www.promtools.org/prom6/downloads/ + "example-logs.zip"
                new TestableGraph("'Sepsis Case' log mined by DCR-miner (Pre-cooked RR-graph)", XmlParser.ParseDcrGraph(Properties.Resources.Sepsis_Graph), XmlParser.ParseDcrGraph(Properties.Resources.Sepsis_Graph_RR)), // https://data.4tu.nl/repository/uuid:915d2bfb-7e84-49ad-a286-dc35f063a460 + "Sepsis Cases - Event Log.xes.gz" (link on page)
            };
            TestableGraphSelected = initialOption; // Prompt user to select an option

            GraphToDisplay = GraphDisplayMode.FullyRedundancyRemoved;

            _settingUp = false;
        }

        public DrawingImage PatternGraphImage { get { _patternGraphImage?.Freeze(); return _patternGraphImage; } set { _patternGraphImage = value; OnPropertyChanged(); } }

        public DrawingImage OtherGraphImage
        {
            get
            {
                _otherGraphImage?.Freeze(); return _otherGraphImage;
            }
            set
            {
                _otherGraphImage = value; OnPropertyChanged();
            }
        }

        public GraphDisplayMode GraphToDisplay
        {
            get => _graphToDisplay;
            set
            {
                _graphToDisplay = value; OnPropertyChanged(nameof(GraphToDisplay));
                RefreshButtonColors();
                if (!_settingUp)
                    UpdateGraphImages(true);
            }
        }
        public Brush FullRedRemButtonBackColor => GraphToDisplay == GraphDisplayMode.FullyRedundancyRemoved ? new SolidColorBrush(Colors.LimeGreen) : new SolidColorBrush(Colors.LightGray);
        public Brush OriginalButtonBackColor => GraphToDisplay == GraphDisplayMode.Original ? new SolidColorBrush(Colors.LimeGreen) : new SolidColorBrush(Colors.LightGray);
        public Brush OvershootContextButtonBackColor => GraphToDisplay == GraphDisplayMode.OvershootContext ? new SolidColorBrush(Colors.LimeGreen) : new SolidColorBrush(Colors.LightGray);
        public Brush CriticalErrorContextButtonBackColor => GraphToDisplay == GraphDisplayMode.CriticalErrorContext ? new SolidColorBrush(Colors.LimeGreen) : new SolidColorBrush(Colors.LightGray);
        public Brush PatternResultFullyRedRemButtonBackColor => GraphToDisplay == GraphDisplayMode.PatternResultFullyRedundancyRemoved ? new SolidColorBrush(Colors.LimeGreen) : new SolidColorBrush(Colors.LightGray);


        public void RefreshButtonColors()
        {
            OnPropertyChanged(nameof(FullRedRemButtonBackColor));
            OnPropertyChanged(nameof(OriginalButtonBackColor));
            OnPropertyChanged(nameof(OvershootContextButtonBackColor));
            OnPropertyChanged(nameof(CriticalErrorContextButtonBackColor));
            OnPropertyChanged(nameof(PatternResultFullyRedRemButtonBackColor));
        }

        public RedundantRelationEvent SelectedErrorRelation { get => _selectedErrorRelation; set { _selectedErrorRelation = value; OnPropertyChanged(); } }

        public List<TestableGraph> TestableGraphs
        {
            get => _testableGraphs; set { _testableGraphs = value; OnPropertyChanged(); }
        }

        public TestableGraph TestableGraphSelected
        {
            get => _testableGraphSelected;
            set
            {
                _testableGraphSelected = value; OnPropertyChanged();
                
                if (_testableGraphSelected.Graph != null)
                {
                    using (new WaitCursor())
                    {
                        _comparer = new RedundancyRemoverComparer(); // Reset
                        _comparer.PerformComparison(_testableGraphSelected.Graph, _testableGraphSelected.RedundancyRemovedGraph); // TODO: Use BG-worker with GUI-events as well
                        _preRedRemGraph = _comparer.InitialGraph;
                        _fullyRedRemGraph = _comparer.FinalCompleteGraph;
                        _patternRedRemGraph = _comparer.FinalPatternGraph;

                        _allResults = _comparer.AllResults;

                        // Build history: round-number mapped to the relations removed in that round
                        var roundsSorted = Enumerable.Range(1, _comparer.RoundsSpent);
                        _roundToRelationsRemovedDict = roundsSorted.ToDictionary(x => x,
                            round => _allResults.Where(y => y is RedundantRelationEvent).Cast<RedundantRelationEvent>()
                                .Where(y => y.Round == round).ToList());

                        TimeSpentCompleteApproach = _comparer.TimeSpentCompleteRedundancyRemover?.ToString();
                        TimeSpentPatternApproach = _comparer.MethodRunningTimes.Values.Aggregate((a,b) => a.Add(b)).ToString(); // Combined execution-times of all patterns
                        PatternStatistics = _comparer.MethodRunningTimes.Select(kv => "[0]" + kv.Key + ": " + kv.Value.ToString()).ToList();
                            // TODO: ^ Also access amount of relations removed by this relation

                        OnPropertyChanged(nameof(TimeSpentCompleteApproach));
                        OnPropertyChanged(nameof(TimeSpentPatternApproach));
                        OnPropertyChanged(nameof(PatternStatistics));

                        // Update view's display of various properties
                        OnPropertyChanged(nameof(ResultString));
                        OnPropertyChanged(nameof(ErrorHeadlineString));
                        OnPropertyChanged(nameof(MissingRedundantRelations));
                        OnPropertyChanged(nameof(OvershotRelations));
                        OnPropertyChanged(nameof(DidCriticalErrorOccur));
                        OnPropertyChanged(nameof(CriticalErrorRedundancyEvent));
                        OnPropertyChanged(nameof(CriticalErrorGraphContext));
                        OnPropertyChanged(nameof(CriticalErrorRedundancyEventString));
                        // Statistics:


                        // Get and update images
                        UpdateGraphImages();
                    }
                }
                else
                {
                    // TODO: Clear results and views etc.
                }
            }
        }

        /// <summary>
        /// Relations that we removed, which the full redundancy-remover did not (Not necessarily errors)
        /// </summary>
        public HashSet<RedundantRelationEvent> OvershotRelations => _comparer.RelationsRemovedButNotByCompleteApproach;

        public List<string> PatternStatistics { get; set; }

        public string TimeSpentPatternApproach { get; set; }
        public string TimeSpentCompleteApproach { get; set; }

        #region Pattern-approach result further computed to find any redundancies not detected by it

        #endregion

        #region Critical Error reporting

        public bool DidCriticalErrorOccur => _comparer.CriticalErrorEventWithContext != null;

        public RedundancyEvent CriticalErrorRedundancyEvent => _comparer.CriticalErrorEventWithContext?.Item1;

        public string CriticalErrorRedundancyEventString => _comparer.CriticalErrorEventWithContext?.Item1.ToString();

        public DcrGraphSimple CriticalErrorGraphContext => _comparer.CriticalErrorEventWithContext?.Item2;

        #endregion

        #region Redundancy-removal on our Pattern-approach result (Redundancies uncaptured by patterns)

        public DcrGraph PatternResultFullyRedundancyRemovedGraph => _comparer.PatternResultFullyRedundancyRemoved;
        
        public HashSet<Relation> MissingRedundantRelations => _comparer.PatternResultFullyRedundancyRemovedMissingRelations;

        #endregion

        public string ResultString =>
            $"{(_comparer.RedundantRelationsCountPatternApproach / (double) _comparer.RedundantRelationsCountActual):P2} ({_comparer.RedundantRelationsCountPatternApproach} / {_comparer.RedundantRelationsCountActual})";

        public string ErrorHeadlineString =>
            $"Overshot removals: {_comparer.RelationsRemovedButNotByCompleteApproach.Count}";

        #region Methods

        public void CopyRighthandSideGraphXmlToClipboard()
        {
            Clipboard.SetText(DcrGraphExporter.ExportToXml(_otherGraphImageGraph));
        }

        public void AttemptToSwitchToErrorContextView()
        {
            if (SelectedErrorRelation == null)
            {
                MessageBox.Show("Please select an error in order to display its context.");
                return;
            }
            GraphToDisplay = GraphDisplayMode.OvershootContext;
        }

        private async void UpdateGraphImages(bool onlyRighthandSide = false)
        {
            try
            {
                Task patternResultImgTask = null;
                if (!onlyRighthandSide)
                {
                    patternResultImgTask = UpdatePatternResultImage();
                }

                // Other graph image (righthandside)
                Task otherImgTask = UpdateRighthandSideImage();

                // Now, after potentially starting both image-retrieval tasks, await them both
                if (patternResultImgTask != null)
                    await patternResultImgTask;

                if (otherImgTask != null)
                    await otherImgTask;
            }
            catch (WebException e)
            {
                //TODO: display error message.

            }
        }

        private async Task UpdatePatternResultImage()
        {
            var patternResultImage = await GraphImageRetriever.Retrieve(DcrGraphExporter.ExportToXml(_patternRedRemGraph));
            if (patternResultImage != null)
            {
                //IsImageLargerThanBorder = image.Height > 508 || image.Width > 1034;
                Dispatcher.Invoke(() => {
                    PatternGraphImage = patternResultImage;
                });
            }
        }

        private async Task UpdateRighthandSideImage()
        {
            // TODO: Caching of images

            DrawingImage image;
            switch (GraphToDisplay)
            {
                case GraphDisplayMode.Original:
                    image = await GraphImageRetriever.Retrieve(DcrGraphExporter.ExportToXml(_preRedRemGraph));
                    _otherGraphImageGraph = _preRedRemGraph;
                    break;
                case GraphDisplayMode.FullyRedundancyRemoved:
                    image = await GraphImageRetriever.Retrieve(DcrGraphExporter.ExportToXml(_fullyRedRemGraph));
                    _otherGraphImageGraph = DcrGraphExporter.ExportToSimpleDcrGraph(_fullyRedRemGraph);
                    break;
                case GraphDisplayMode.OvershootContext:
                    // Grab selected error-relation if any and build the graph
                    var overshootContextGraph = _comparer.GetContextBeforeEvent(SelectedErrorRelation);
                    image = await GraphImageRetriever.Retrieve(DcrGraphExporter.ExportToXml(overshootContextGraph));
                    _otherGraphImageGraph = overshootContextGraph;
                    break;
                case GraphDisplayMode.CriticalErrorContext:
                    var errorContextGraph = _comparer.CriticalErrorEventWithContext?.Item2;
                    image = await GraphImageRetriever.Retrieve(DcrGraphExporter.ExportToXml(errorContextGraph));
                    _otherGraphImageGraph = errorContextGraph;
                    break;
                case GraphDisplayMode.PatternResultFullyRedundancyRemoved:
                    var patternResultFullyRedRemGraph = _comparer.PatternResultFullyRedundancyRemoved;
                    image = await GraphImageRetriever.Retrieve(DcrGraphExporter.ExportToXml(patternResultFullyRedRemGraph));
                    _otherGraphImageGraph = DcrGraphExporter.ExportToSimpleDcrGraph(patternResultFullyRedRemGraph);
                    break;
                default:
                    throw new ArgumentException("Unexpected GraphDisplayMode value.");
            }

            // TODO: Build error-context graph if that is the selected graph to display on righthand-side

            if (image != null)
            {
                //IsImageLargerThanBorder = image.Height > 508 || image.Width > 1034;
                Dispatcher.Invoke(() => {
                    OtherGraphImage = image;
                });
            }
        }

        public async void ExportPatternResultToXml()
        {
            var sfd = new SaveFileDialog();
            sfd.Filter = "XML files (*.xml)|*.xml";

            if (sfd.ShowDialog() == true) // User pressed OK
            {
                try
                {
                    using (var sw = new StreamWriter(sfd.FileName))
                    {
                        await sw.WriteLineAsync(DcrGraphExporter.ExportToXml(_patternRedRemGraph));
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("File is open (presumably). Please make sure the file is closed and try again.");
                }
            }
        }

        #endregion
    }
}
