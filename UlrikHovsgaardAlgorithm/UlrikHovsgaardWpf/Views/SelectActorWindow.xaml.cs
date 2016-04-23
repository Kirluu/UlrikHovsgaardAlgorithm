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
using UlrikHovsgaardWpf.Data;
using UlrikHovsgaardWpf.ViewModels;

namespace UlrikHovsgaardWpf.Views
{
    /// <summary>
    /// Interaction logic for SelectActorWindow.xaml
    /// </summary>
    public partial class SelectActorWindow : Window
    {
        private SelectActorWindowViewModel _viewModel;

        public SelectActorWindow(SelectActorWindowViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;

            DataContext = _viewModel;

            _viewModel.ClosingRequest += (sender, e) => this.Close();
        }
    }
}
