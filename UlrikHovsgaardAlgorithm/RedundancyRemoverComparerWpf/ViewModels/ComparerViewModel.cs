using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using RedundancyRemoverComparerWpf.DataClasses;
using UlrikHovsgaardAlgorithm.Export;
using UlrikHovsgaardAlgorithm.Parsing;
using UlrikHovsgaardAlgorithm.RedundancyRemoval;
using UlrikHovsgaardWpf.ViewModels;

namespace RedundancyRemoverComparerWpf.ViewModels
{
    public class ComparerViewModel : SuperViewModel
    {
        private DrawingImage _patternGraphImage;
        private DrawingImage _fullyRedRemGraphImage;
        private DrawingImage _preRedRemGraphImage;
        private bool _showPreRedundancyRemovalGraph;
        private List<TestableGraph> _testableGraphs;
        private TestableGraph _testableGraphSelected;

        private RedundancyRemoverComparer _comparer = new RedundancyRemoverComparer();

        public ComparerViewModel()
        {
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
                if (ShowPreRedundancyRemovalGraph)
                {
                    _preRedRemGraphImage?.Freeze(); return _preRedRemGraphImage;
                }
                _fullyRedRemGraphImage?.Freeze(); return _fullyRedRemGraphImage;
            }
            set
            {
                if (ShowPreRedundancyRemovalGraph)
                {
                    _preRedRemGraphImage = value;
                    OnPropertyChanged();
                }
                else
                {
                    _fullyRedRemGraphImage = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowPreRedundancyRemovalGraph { get => _showPreRedundancyRemovalGraph; set { _showPreRedundancyRemovalGraph = value; OnPropertyChanged(); OnPropertyChanged(nameof(GraphShownText)); } }

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
    }
}
