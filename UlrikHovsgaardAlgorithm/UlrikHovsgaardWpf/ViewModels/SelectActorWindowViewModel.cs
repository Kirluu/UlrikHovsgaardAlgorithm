using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Forms;
using UlrikHovsgaardWpf.Data;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardWpf.Utils;

namespace UlrikHovsgaardWpf.ViewModels
{
    public class SelectActorWindowViewModel : SuperViewModel
    {
        public event Action<Log> SubLogSelected;

        #region Fields

        private readonly Log _entireLog;

        #endregion
        
        #region Properties

        private string _activityAmountUpperBound;
        private ObservableCollection<ActorWithSubLog> _actorsWithSubLogs;

        public string ActivityAmountUpperBound { get { return _activityAmountUpperBound; } set { _activityAmountUpperBound = value; OnPropertyChanged(); } }
        public ObservableCollection<ActorWithSubLog> ActorsWithSubLogs { get { return _actorsWithSubLogs; } set { _actorsWithSubLogs = value; OnPropertyChanged(); } }
        public ActorWithSubLog SelectedActorWithSubLog { get; set; }

        #region Commands


        private ICommand _confirmUpperBoundSelectionCommand;
        private ICommand _confirmActorLogSelectionCommand;
        private ICommand _cancelCommand;

        public ICommand ConfirmUpperBoundSelectionCommand { get { return _confirmUpperBoundSelectionCommand; } set { _confirmUpperBoundSelectionCommand = value; OnPropertyChanged(); } }
        public ICommand ConfirmActorLogSelectionCommand { get { return _confirmActorLogSelectionCommand; } set { _confirmActorLogSelectionCommand = value; OnPropertyChanged(); } }
        public ICommand CancelCommand { get { return _cancelCommand; } set { _cancelCommand = value; OnPropertyChanged(); } }

        #endregion

        #endregion
        
        public SelectActorWindowViewModel(Log log)
        {
            _entireLog = log;
            ActorsWithSubLogs = new ObservableCollection<ActorWithSubLog>();
            SetUpCommands();
        }

        private void SetUpCommands()
        {
            ConfirmUpperBoundSelectionCommand = new ButtonActionCommand(UpperBoundSelected);
            ConfirmActorLogSelectionCommand = new ButtonActionCommand(SubLogChosen);
            CancelCommand = new ButtonActionCommand(Cancel);
        }

        private void UpperBoundSelected()
        {
            if (string.IsNullOrEmpty(ActivityAmountUpperBound))
                MessageBox.Show("Please enter an integer value.");
            int amount;
            if (int.TryParse(ActivityAmountUpperBound, out amount))
            {
                // Limit to traces with max 'amount' unique activities
                var subLog = _entireLog.FilterByNoOfActivities(amount);

                ActorsWithSubLogs.Clear();

                foreach (var actor in new HashSet<string>(subLog.Traces.SelectMany(trace => trace.Events.Select(a => a.ActorName))))
                {
                    ActorsWithSubLogs.Add(new ActorWithSubLog(actor, subLog.FilterByActor(actor)));
                }
            }
            else
            {
                MessageBox.Show("Value is not an integer.", "Error");
            }
        }
        private void SubLogChosen()
        {
            if (SelectedActorWithSubLog == null) return;

            SubLogSelected?.Invoke(SelectedActorWithSubLog.Log);
            OnClosingRequest();
        }

        private void Cancel()
        {
            OnClosingRequest();
        }
    }
}
