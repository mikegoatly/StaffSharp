using Avalonia.Controls;
using Avalonia.Interactivity;
using StaffSharp.SvgRenderDebug.ViewModels;

namespace StaffSharp.SvgRenderDebug
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }

        private void OnExampleSelected(object? sender, SelectionChangedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm &&
                sender is ComboBox comboBox &&
                comboBox.SelectedItem is AbcExample example)
            {
                vm.LoadExample(example);
                // Clear selection so the same example can be loaded again
                comboBox.SelectedItem = null;
            }
        }

        private void OnSvgViewerSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                // Subtract margins and borders
                var availableWidth = e.NewSize.Width - 20; // 10px margin on each side
                if (availableWidth > 100) // Minimum reasonable width
                {
                    vm.ControlWidth = availableWidth;
                }
            }
        }
    }
}
