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

        private string _stats;
        public string Stats { get { return _stats; } set { _stats = value; OnPropertyChanged(); } }

        // DATA SOURCES
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

            Init();

            SetUpCommands();
        }

        public void Init()
        {
            // Init datasource for activity filtering
            ActivitySelections = new TrulyObservableCollection<ActivitySelection>(_dcrGraph.Activities.Select(a => new ActivitySelection(a)));

            // Set initial RelationFilter value
            RelationFilter = DcrGraph.AllRelationsStr;

            // Default values now set --> Now listen for further selection changes
            foreach (var activitySelection in ActivitySelections)
            {
                activitySelection.SelectionChanged += RefreshStatisticsTextBox;
            }
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

        private void RefreshStatisticsTextBox()
        {
            if (_refreshingPaused) return;

            // Read from grid selections + combobox to determine what to write
            var selectedActivities = new HashSet<Activity>(ActivitySelections.Where(a => a.IsSelected).Select(a => a.Activity));
            var stats = _dcrGraph.ToDcrFormatString(selectedActivities, RelationFilter, true);
            Stats = stats;
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
