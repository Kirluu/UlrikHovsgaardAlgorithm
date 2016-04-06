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
using UlrikHovsgaardWpf.ViewModels;

namespace UlrikHovsgaardWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            Show();

            _viewModel = new MainWindowViewModel();
            // Register window-opening event
            _viewModel.OpenStartOptionsEvent += DisplayOptionsWindow;
            _viewModel.Init();

            DataContext = _viewModel;
        }

        private void DisplayOptionsWindow(StartOptionsWindowViewModel viewModel)
        {
            var optionsWindow = new StartOptionsWindow(viewModel);
            optionsWindow.Owner = Window.GetWindow(this);
            optionsWindow.ShowDialog();
        }

        private void ActivityButtonClicked(object sender, RoutedEventArgs e)
        {
            var buttonContentName = (sender as Button).Content.ToString();
            _viewModel.ActivityButtonClicked(buttonContentName);
        }
    }
}
