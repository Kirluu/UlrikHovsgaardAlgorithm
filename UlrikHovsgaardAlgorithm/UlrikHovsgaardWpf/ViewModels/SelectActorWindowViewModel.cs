using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using UlrikHovsgaardWpf.Data;

namespace UlrikHovsgaardWpf.ViewModels
{
    public class SelectActorWindowViewModel : SuperViewModel
    {
        #region Fields

        // TODO: Have full log here to filter by

        #endregion


        #region Properties

        private string _activityAmountUpperBound;
        private ObservableCollection<ActorWithSubLog> _actorsWithSubLogs;

        public string ActivityAmountUpperBound { get { return _activityAmountUpperBound; } set { _activityAmountUpperBound = value; OnPropertyChanged(); } }
        public ObservableCollection<ActorWithSubLog> ActorsWithSubLogs { get { return _actorsWithSubLogs; } set { _actorsWithSubLogs = value; OnPropertyChanged(); } }

        #region Commands


        private ICommand _confirmUpperBoundSelectionCommand;
        private ICommand _cancelCommand;

        public ICommand ConfirmUpperBoundSelectionCommand { get { return _confirmUpperBoundSelectionCommand; } set { _confirmUpperBoundSelectionCommand = value; OnPropertyChanged(); } }
        public ICommand CancelCommand { get { return _cancelCommand; } set { _cancelCommand = value; OnPropertyChanged(); } }

        #endregion

        #endregion


    }
}
