using System.ComponentModel;
using System.Windows;
using TeamScreenClientPortableWPF.ViewModels;

namespace TeamScreenClientPortableWPF.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // Set the DataContext to the ViewModel
            var viewModel = new MainViewModel();
            this.DataContext = viewModel;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (this.DataContext is MainViewModel viewModel)
            {
                viewModel.OnWindowClosing();
            }
        }
    }
}