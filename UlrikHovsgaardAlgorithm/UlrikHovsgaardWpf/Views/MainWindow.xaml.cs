using System;
using System.Collections.Generic;
using System.Globalization;
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
using UlrikHovsgaardAlgorithm.Datamodels;
using UlrikHovsgaardWpf.ViewModels;

namespace UlrikHovsgaardWpf.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel _viewModel;

        private bool _sliderValueChanged = false;

        public MainWindow()
        {
            InitializeComponent();

            thresholdSlider.PreviewMouseUp += thresholdSlider_MouseUp;

            Show();

            _viewModel = new MainWindowViewModel();
            // Register window-opening event
            _viewModel.OpenStartOptionsEvent += DisplayOptionsWindow;
            _viewModel.RefreshDataContainer += RefreshDataGrid;
            _viewModel.SelectTraceByIndex += SelectTraceByIndex;
            _viewModel.RefreshImageBorder += AttemptToRefreshImagePlacement;
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

        private void AttemptToRefreshImagePlacement()
        {
            border.child_PreviewMouseRightButtonDown(new object(), null);
        }

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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Environment.Exit(0);
        }

        private void textBox_KeyDown(object sender, KeyEventArgs e) // TODO: Also LostFocus event
        {
            if (e.Key == Key.Enter)
            {
                EvaluateThresholdValueInput();

                // Drop focus and re-focus - forces slider to consider bound value
                _ignoreLostFocus = true;
                thresholdSlider.Focus();
                txtThreshold.Focus();
                _ignoreLostFocus = false;

                AskToRecomputeGraphDueToThresholdChange();
            }
        }

        private bool _ignoreLostFocus = false;
        private void txtThreshold_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_ignoreLostFocus) return;

            EvaluateThresholdValueInput();
            AskToRecomputeGraphDueToThresholdChange();
        }

        private void thresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _sliderValueChanged = true;
        }

        private void thresholdSlider_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_sliderValueChanged == true)
            {
                AskToRecomputeGraphDueToThresholdChange();
                _sliderValueChanged = false;
                e.Handled = true;
            }
        }

        private void AskToRecomputeGraphDueToThresholdChange()
        {
            var threshold = thresholdSlider.Value; // decimal format (between 0 and 1)
            Threshold.Value = threshold;
            
            _viewModel.UpdateGraph(); // Hack: Made method public, because command-execution didn't work
        }

        private void EvaluateThresholdValueInput()
        {
            // Hotwire input-value
            var threshText = txtThreshold.Text;
            threshText = threshText.Split(' ')[0];
            if (threshText.Contains("%"))
                threshText = threshText.Substring(0, threshText.Length - 1); // Remove last char
            double threshValue;
            if ((threshText.Contains(",") && double.TryParse(threshText, out threshValue)) || double.TryParse(threshText, NumberStyles.Any, CultureInfo.InvariantCulture, out threshValue))
            {
                // Set value as decimal value
                txtThreshold.Text = (threshValue / 100).ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                MessageBox.Show("The given Constraint Violation Threshold value is not valid.");
                return;
            }
        }
    }
}
