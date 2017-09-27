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
using System.Windows.Navigation;
using System.Windows.Shapes;
using RedundancyRemoverComparerWpf.ViewModels;

namespace RedundancyRemoverComparerWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ComparerViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();
            _vm = new ComparerViewModel();
            this.DataContext = _vm;
            _vm.SetUpInitialSettings();
        }

        private void btnShowOriginal_Click(object sender, RoutedEventArgs e)
        {
            _vm.GraphToDisplay = ComparerViewModel.GraphDisplayMode.Original;
        }

        private void btnShowFullyRedundancyRemoved_Click(object sender, RoutedEventArgs e)
        {
            _vm.GraphToDisplay = ComparerViewModel.GraphDisplayMode.FullyRedundancyRemoved;
        }

        private void btnShowContextOfErroneouslyRemovedRelation_Click(object sender, RoutedEventArgs e)
        {
            _vm.AttemptToSwitchToErrorContextView();
        }
    }
}
