using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using UlrikHovsgaardWpf.ViewModels;

namespace UlrikHovsgaardWpf.Views
{
    /// <summary>
    /// Interaction logic for StatisticsWindow.xaml
    /// </summary>
    public partial class StatisticsUserControl : UserControl
    {
        private StatisticsWindowViewModel _viewModel;

        public StatisticsUserControl(StatisticsWindowViewModel viewModel)
        {
            _viewModel = viewModel;

            InitializeComponent();

            //_viewModel.Init();

            DataContext = _viewModel;
        }
    }
}
