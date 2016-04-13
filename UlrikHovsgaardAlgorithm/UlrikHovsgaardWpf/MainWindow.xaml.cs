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
            _viewModel.RefreshDataContainer += RefreshDataGrid;
            _viewModel.SelectTraceByIndex += SelectTraceByIndex;
            DataContext = _viewModel;

            _viewModel.Init();

            
        }

        private void DisplayOptionsWindow(StartOptionsWindowViewModel viewModel)
        {
            var optionsWindow = new StartOptionsWindow(viewModel);
            optionsWindow.Owner = Window.GetWindow(this);
            optionsWindow.ShowDialog();
        }

        private void RefreshDataGrid()
        {
            dataGridLogDisplay.Items.Refresh();
        }

        private void SelectTraceByIndex(int index)
        {
            //dataGridLogDisplay.SelectedIndex = index;

            //dataGridLogDisplay.ScrollIntoView(dataGridLogDisplay.SelectedItem); // TODO: Marks with transparent BG instead of blue BG

            //dataGridLogDisplay.ScrollIntoView(dataGridLogDisplay.SelectedItem, dataGridLogDisplay.Columns[0]);

            //var selectedRow = (DataGridRow)dataGridLogDisplay.ItemContainerGenerator.ContainerFromIndex(dataGridLogDisplay.SelectedIndex);
            //FocusManager.SetIsFocusScope(selectedRow, true);
            //FocusManager.SetFocusedElement(selectedRow, selectedRow);

            //DataGridRow row = (DataGridRow)dataGridLogDisplay.ItemContainerGenerator.ContainerFromIndex(index);
            //row.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }

        private void ActivityButtonClicked(object sender, RoutedEventArgs e)
        {
            var buttonContentName = (sender as Button).Content.ToString();
            _viewModel.ActivityButtonClicked(buttonContentName);
        }
    }
}
