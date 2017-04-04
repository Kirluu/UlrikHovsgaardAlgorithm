using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardWpf.Utils;

namespace UlrikHovsgaardWpf.ViewModels
{
    public class StatisticsWindowViewModel : SuperViewModel
    {
        // FIELDS
        private DcrGraph _dcrGraph;
        private static bool _refreshingPaused;

        // PROPERTIES
        private string _relationFilter;
        public string RelationFilter
        {
            get { return _relationFilter; }
            set
            {
                _relationFilter = value;
                OnPropertyChanged();
                RefreshStatisticsTextBox();
            }
        }

        // DATA SOURCES

        private TrulyObservableCollection<ConstraintStatistics> _constraintStats;
        public TrulyObservableCollection<ConstraintStatistics> ConstraintStats { get { return _constraintStats; } set { _constraintStats = value; OnPropertyChanged(); } }

        public List<string> RelationFilters => new List<string> { DcrGraph.AllRelationsStr, DcrGraph.InclusionsExclusionsStr, DcrGraph.ResponsesStr, DcrGraph.ConditionsStr };

        private TrulyObservableCollection<ActivitySelection> _activitySelections;
        public TrulyObservableCollection<ActivitySelection> ActivitySelections { get { return _activitySelections; } set { _activitySelections = value; OnPropertyChanged(); } }

        // COMMANDS
        private ICommand _selectAllCommand;
        private ICommand _deselectAllCommand;

        public ICommand SelectAllCommand { get { return _selectAllCommand; } set { _selectAllCommand = value; OnPropertyChanged(); } }
        public ICommand DeselectAllCommand { get { return _deselectAllCommand; } set { _deselectAllCommand = value; OnPropertyChanged(); } }
        


        public StatisticsWindowViewModel(DcrGraph dcrGraph)
        {
            _dcrGraph = dcrGraph;

            // Init datasource for activity filtering
            ActivitySelections = new TrulyObservableCollection<ActivitySelection>(_dcrGraph.Activities.Select(a => new ActivitySelection(a)));

            // Set initial RelationFilter value
            RelationFilter = DcrGraph.AllRelationsStr;

            // Default values now set --> Now listen for further selection changes
            foreach (var activitySelection in ActivitySelections)
            {
                activitySelection.SelectionChanged += RefreshStatisticsTextBox;
            }

            SetUpCommands();
        }


        // METHODS

        public void Refresh(DcrGraph newGraph)
        {
            _dcrGraph = newGraph;
            RefreshStatisticsTextBox();
        }

        private void SetUpCommands()
        {
            SelectAllCommand = new ButtonActionCommand(SelectAllClicked);
            DeselectAllCommand = new ButtonActionCommand(DeselectAllClicked);
        }

        private void SelectAllClicked()
        {
            _refreshingPaused = true;
            foreach (var activitySelection in ActivitySelections)
            {
                activitySelection.IsSelected = true;
            }
            _refreshingPaused = false;
            RefreshStatisticsTextBox();
        }

        private void DeselectAllClicked()
        {
            _refreshingPaused = true;
            foreach (var activitySelection in ActivitySelections)
            {
                activitySelection.IsSelected = false;
            }
            _refreshingPaused = false;
            RefreshStatisticsTextBox();
        }

        public void RefreshStatisticsTextBox()
        {
            if (_refreshingPaused) return;

            // Read from grid selections + combobox to determine what to write
            var selectedActivities = new HashSet<Activity>(ActivitySelections.Where(a => a.IsSelected).Select(a => a.Activity));
            var stats = _dcrGraph.FilteredConstraintStringsWithConfidence(selectedActivities, RelationFilter, true);
            var newStats = stats.Select(x => new ConstraintStatistics(x.Item1, x.Item2));
            ConstraintStats = new TrulyObservableCollection<ConstraintStatistics>(newStats);
        }

        public class ConstraintStatistics : SuperViewModel
        {
            private string _constraintName;
            private string _violationsOverInvocations;
            private string _confidenceInRemovalPercentString;
            private bool _isContradicted;

            public string ConstraintName { get { return _constraintName; } set { _constraintName = value; OnPropertyChanged(); } }
            public string ViolationsOverInvocations { get { return _violationsOverInvocations; } set { _violationsOverInvocations = value; OnPropertyChanged(); } }
            public string ConfidenceInRemovalPercentString { get { return _confidenceInRemovalPercentString; } set { _confidenceInRemovalPercentString = value; OnPropertyChanged(); } }
            public bool IsContradicted { get { return _isContradicted; } set { _isContradicted = value; OnPropertyChanged(); } }

            public ConstraintStatistics(string whatIsIt, Confidence confidence)
            {
                ConstraintName = whatIsIt;
                ViolationsOverInvocations = string.Format("{0} / {1}", confidence.Violations, confidence.Invocations);
                ConfidenceInRemovalPercentString = string.Format("{0:N2} %", confidence.Get * 100.0);
                IsContradicted = confidence.IsContradicted();
            }
        }

        public class ActivitySelection : SuperViewModel
        {
            public event Action SelectionChanged;

            private Activity _activity;
            private bool _isSelected;

            public Activity Activity { get { return _activity; } set { _activity = value; OnPropertyChanged(); } }
            public bool IsSelected { get { return _isSelected; } set { _isSelected = value; OnPropertyChanged(); SelectionChanged?.Invoke(); } }

            public ActivitySelection(Activity activity)
            {
                Activity = activity;
                IsSelected = true;
            }
        }
    }
}
