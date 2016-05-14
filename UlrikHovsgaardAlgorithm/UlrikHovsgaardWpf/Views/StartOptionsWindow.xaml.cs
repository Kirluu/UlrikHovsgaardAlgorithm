using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UlrikHovsgaardWpf.ViewModels;

namespace UlrikHovsgaardWpf.Views
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

            _viewModel.OpenSelectActorWindow += DisplaySelectActorWindow;

            DataContext = _viewModel;

            _viewModel.ClosingRequest += (sender, e) => this.Close();

            textBox.Focus();
        }

        private void DisplaySelectActorWindow(SelectActorWindowViewModel viewModel)
        {
            var selectActorWindow = new SelectActorWindow(viewModel);
            selectActorWindow.Owner = Window.GetWindow(this);
            selectActorWindow.ShowDialog();
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
