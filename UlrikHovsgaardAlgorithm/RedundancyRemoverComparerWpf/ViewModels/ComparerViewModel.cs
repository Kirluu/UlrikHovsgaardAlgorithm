using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;
using RedundancyRemoverComparerWpf.DataClasses;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Datamodels;
using UlrikHovsgaardAlgorithm.Export;
using UlrikHovsgaardAlgorithm.Parsing;
using UlrikHovsgaardAlgorithm.RedundancyRemoval;
using UlrikHovsgaardWpf.Data;
using UlrikHovsgaardWpf.ViewModels;

namespace RedundancyRemoverComparerWpf.ViewModels
{
    public class ComparerViewModel : SuperViewModel
    {
        public enum GraphDisplayMode { Original, FullyRedundancyRemoved, ErrorContext }

        private DcrGraphSimple _preRedRemGraph;
        private DcrGraph _fullyRedRemGraph;
        private DcrGraphSimple _patternRedRemGraph;
        private Dictionary<string, HashSet<Result>> _allResults;
        private Dictionary<int, List<Relation>> _roundToRelationsRemovedDict;
        private DrawingImage _patternGraphImage;
        private DrawingImage _otherGraphImage;

        private bool _showPreRedundancyRemovalGraph;
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
            // Initialize any source-collections for availability in UI
            var initialOption = new TestableGraph("Select a test-case...", null);
            TestableGraphs = new List<TestableGraph>
            {
                initialOption,
                new TestableGraph("Mortgage application mined graph", XmlParser.ParseDcrGraph(Properties.Resources.mortgageGRAPH)),
                new TestableGraph("9 activities N-squared inclusion-relations", XmlParser.ParseDcrGraph(Properties.Resources.AllInclusion9ActivitiesGraph))
            };
            TestableGraphSelected = initialOption; // Prompt user to select an option
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

        public bool ShowPreRedundancyRemovalGraph
        {
            get => _showPreRedundancyRemovalGraph;
            set
            {
                _showPreRedundancyRemovalGraph = value; OnPropertyChanged(); OnPropertyChanged(nameof(GraphShownText));
                UpdateGraphImages(true);
            }
        }

        public GraphDisplayMode GraphToDisplay
        {
            get => _graphToDisplay;
            set
            {
                _graphToDisplay = value; OnPropertyChanged(); OnPropertyChanged(nameof(GraphToDisplay));
                UpdateGraphImages(true);
            }
        }

        public string GraphShownText => ShowPreRedundancyRemovalGraph
            ? "Showing graph prior to redundancy-removal"
            : "Showing graph with no redundancy";

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
                // TODO: Perform logic to run the comparer and fetch images from the results
                if (_testableGraphSelected.Graph != null)
                {
                    _comparer.PerformComparison(_testableGraphSelected.Graph); // TODO: Use BG-worker with GUI-events as well
                    _preRedRemGraph = _comparer.InitialGraph;
                    _fullyRedRemGraph = _comparer.FinalCompleteGraph;
                    _patternRedRemGraph = _comparer.FinalPatternGraph;

                    _allResults = _comparer.AllResults;

                    // Build history: round-number mapped to the relations removed in that round
                    var roundsSorted = Enumerable.Range(1, _comparer.RoundsSpent);
                    _roundToRelationsRemovedDict = roundsSorted.ToDictionary(x => x, round => _allResults.SelectMany(kv => kv.Value.Where(res => res.Round == round).SelectMany(res => res.Removed)).ToList());
                    
                    // Update view's display of various properties
                    OnPropertyChanged(nameof(ResultString));
                    OnPropertyChanged(nameof(ErrorHeadlineString));
                    OnPropertyChanged(nameof(MissingRedundantRelations));
                    OnPropertyChanged(nameof(ErroneouslyRemovedRelations));

                    // Get and update images
                    UpdateGraphImages();
                }
                else
                {
                    // TODO: Clear results and views etc.
                }
            }
        }

        public HashSet<Relation> MissingRedundantRelations => _comparer.MissingRedundantRelations;

        public HashSet<Relation> ErroneouslyRemovedRelations => _comparer.ErroneouslyRemovedRelations;

        public string ResultString =>
            $"{(_comparer.RedundantRelationsCountPatternApproach / (double) _comparer.RedundantRelationsCountActual):P2} ({_comparer.RedundantRelationsCountPatternApproach} / {_comparer.RedundantRelationsCountActual})";

        public string ErrorHeadlineString =>
            $"Erroneous removals: {_comparer.ErroneouslyRemovedRelations.Count}";

        #region Methods

        public void SwitchGraphToShowButtonClicked()
        {
            // Invert choice
            

            ShowPreRedundancyRemovalGraph = !ShowPreRedundancyRemovalGraph;
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
                    image = await GraphImageRetriever.Retrieve(DcrGraphExporter.ExportToXml(_fullyRedRemGraph));
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
