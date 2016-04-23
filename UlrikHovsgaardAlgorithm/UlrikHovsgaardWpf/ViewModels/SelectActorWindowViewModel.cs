using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace UlrikHovsgaardWpf.ViewModels
{
    public class SelectActorWindowViewModel : SuperViewModel
    {
        #region Properties

        private string _activityAmountUpperBound;

        public string ActivityAmountUpperBound { get { return _activityAmountUpperBound; } set { _activityAmountUpperBound = value; OnPropertyChanged(); } }

        #region Commands


        private ICommand _confirmUpperBoundSelectionCommand;

        public ICommand ConfirmUpperBoundSelectionCommand { get { return _confirmUpperBoundSelectionCommand; } set { _confirmUpperBoundSelectionCommand = value; OnPropertyChanged(); } }

        #endregion

        #endregion


    }
}
