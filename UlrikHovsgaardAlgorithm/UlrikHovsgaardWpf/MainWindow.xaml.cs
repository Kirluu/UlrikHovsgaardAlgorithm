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
            dataGridLogDisplay.SelectedIndex = index;

            dataGridLogDisplay.ScrollIntoView(dataGridLogDisplay.SelectedItem); // TODO: Marks with transparent BG instead of blue BG
        }

        private void ActivityButtonClicked(object sender, RoutedEventArgs e)
        {
            var buttonContentName = (sender as Button).Content.ToString();
            _viewModel.ActivityButtonClicked(buttonContentName);
        }

        // TODO: Save initial "Point" to be able to go back to that Point after PostProcessing??? - Rather: Encourage to go to whatever Point fits the given image
        // TODO: Maybe can re-set Stretch to Uniform (maybe after setting to None or null first)

        #region http://stackoverflow.com/questions/741956/pan-zoom-image

        private void image_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var st = (ScaleTransform)((TransformGroup)image.RenderTransform)
                .Children.First(tr => tr is ScaleTransform);
            double zoom = e.Delta > 0 ? .2 : -.2;
            st.ScaleX += zoom;
            st.ScaleY += zoom;
        }

        Point _start;
        Point _origin;
        private void image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var tt = (TranslateTransform)((TransformGroup)image.RenderTransform)
                .Children.First(tr => tr is TranslateTransform);
            _start = e.GetPosition(border);
            _origin = new Point(tt.X, tt.Y);
            image.CaptureMouse();
        }

        private void image_MouseMove(object sender, MouseEventArgs e)
        {
            if (image.IsMouseCaptured)
            {
                var tt = (TranslateTransform)((TransformGroup)image.RenderTransform)
                    .Children.First(tr => tr is TranslateTransform);
                Vector v = _start - e.GetPosition(border);
                tt.X = _origin.X - v.X;
                tt.Y = _origin.Y - v.Y;
            }
        }

        private void image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            image.ReleaseMouseCapture();
        }

        #endregion
    }
}
