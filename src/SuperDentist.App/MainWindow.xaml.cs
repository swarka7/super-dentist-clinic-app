using System.Windows;
using SuperDentist.App.ViewModels;

namespace SuperDentist.App
{
    public partial class MainWindow : Window
    {
        public MainWindow(ShellViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
