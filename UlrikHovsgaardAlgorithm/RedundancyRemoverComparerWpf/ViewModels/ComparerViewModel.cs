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
        private DcrGraphSimple _preRedRemGraph;
        private DcrGraph _fullyRedRemGraph;
        private DcrGraphSimple _patternRedRemGraph;
        private DrawingImage _patternGraphImage;
        private DrawingImage _otherGraphImage;

        private bool _showPreRedundancyRemovalGraph;
        private List<TestableGraph> _testableGraphs;
        private TestableGraph _testableGraphSelected;

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

                    // Get and update images
                    UpdateGraphImages();
                }
                else
                {
                    // TODO: Clear results and views etc.
                }
            }
        }

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
            var image = ShowPreRedundancyRemovalGraph
                ? await GraphImageRetriever.Retrieve(DcrGraphExporter.ExportToXml(_preRedRemGraph))
                : await GraphImageRetriever.Retrieve(DcrGraphExporter.ExportToXml(_fullyRedRemGraph));
            if (image != null)
            {
                //IsImageLargerThanBorder = image.Height > 508 || image.Width > 1034;
                Dispatcher.Invoke(() => {
                    OtherGraphImage = image;
                });
            }
        }
    }
}
