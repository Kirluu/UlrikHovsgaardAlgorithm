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

namespace UlrikHovsgaardWpf
{
    /// <summary>
    /// Interaction logic for StartOptionsWindow.xaml
    /// </summary>
    public partial class StartOptionsWindow : Window
    {
        private StartOptionsWindowViewModel _viewModel;

        public StartOptionsWindow(StartOptionsWindowViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;

            DataContext = _viewModel;

            _viewModel.ClosingRequest += (sender, e) => this.Close();
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseUp(e);
            if (Mouse.Captured is Calendar || Mouse.Captured is System.Windows.Controls.Primitives.CalendarItem)
            {
                Mouse.Capture(null);
            }
        }
    }
}
