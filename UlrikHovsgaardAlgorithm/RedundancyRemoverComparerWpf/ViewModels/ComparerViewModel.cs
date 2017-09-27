using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
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
        public enum GraphDisplayMode { Original, FullyRedundancyRemoved, ErrorContext }

        private bool _settingUp;

        private DcrGraphSimple _preRedRemGraph;
        private DcrGraph _fullyRedRemGraph;
        private DcrGraphSimple _patternRedRemGraph;
        private List<RedundancyEvent> _allResults;
        private Dictionary<int, List<RedundantRelationEvent>> _roundToRelationsRemovedDict;
        private DrawingImage _patternGraphImage;
        private DrawingImage _otherGraphImage;

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
                new TestableGraph("9 activities N-squared inclusion-relations", XmlParser.ParseDcrGraph(Properties.Resources.AllInclusion9ActivitiesGraph))
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
        public Brush ErrorContextButtonBackColor => GraphToDisplay == GraphDisplayMode.ErrorContext ? new SolidColorBrush(Colors.LimeGreen) : new SolidColorBrush(Colors.LightGray);

        public void RefreshButtonColors()
        {
            OnPropertyChanged(nameof(FullRedRemButtonBackColor));
            OnPropertyChanged(nameof(OriginalButtonBackColor));
            OnPropertyChanged(nameof(ErrorContextButtonBackColor));
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
                        _comparer.PerformComparison(_testableGraphSelected.Graph); // TODO: Use BG-worker with GUI-events as well
                        _preRedRemGraph = _comparer.InitialGraph;
                        _fullyRedRemGraph = _comparer.FinalCompleteGraph;
                        _patternRedRemGraph = _comparer.FinalPatternGraph;

                        _allResults = _comparer.AllResults;

                        // Build history: round-number mapped to the relations removed in that round
                        var roundsSorted = Enumerable.Range(1, _comparer.RoundsSpent);
                        _roundToRelationsRemovedDict = roundsSorted.ToDictionary(x => x,
                            round => _allResults.Where(y => y is RedundantRelationEvent).Cast<RedundantRelationEvent>()
                                .Where(y => y.Round == round).ToList());

                        // Update view's display of various properties
                        OnPropertyChanged(nameof(ResultString));
                        OnPropertyChanged(nameof(ErrorHeadlineString));
                        OnPropertyChanged(nameof(MissingRedundantRelations));
                        OnPropertyChanged(nameof(ErroneouslyRemovedRelations));

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

        public HashSet<Relation> MissingRedundantRelations => _comparer.MissingRedundantRelations;

        public HashSet<RedundantRelationEvent> ErroneouslyRemovedRelations => _comparer.ErroneouslyRemovedRelations;

        public string ResultString =>
            $"{(_comparer.RedundantRelationsCountPatternApproach / (double) _comparer.RedundantRelationsCountActual):P2} ({_comparer.RedundantRelationsCountPatternApproach} / {_comparer.RedundantRelationsCountActual})";

        public string ErrorHeadlineString =>
            $"Erroneous removals: {_comparer.ErroneouslyRemovedRelations.Count}";

        #region Methods

        public void AttemptToSwitchToErrorContextView()
        {
            if (SelectedErrorRelation == null)
            {
                MessageBox.Show("Please select an error in order to display its context.");
                return;
            }
            GraphToDisplay = GraphDisplayMode.ErrorContext;
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
                    break;
                case GraphDisplayMode.FullyRedundancyRemoved:
                    image = await GraphImageRetriever.Retrieve(DcrGraphExporter.ExportToXml(_fullyRedRemGraph));
                    break;
                case GraphDisplayMode.ErrorContext:
                    // Grab selected error-relation if any and build the graph
                    image = await GraphImageRetriever.Retrieve(DcrGraphExporter.ExportToXml(_comparer.GetContextBeforeEvent(SelectedErrorRelation)));
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

        #endregion
    }
}
