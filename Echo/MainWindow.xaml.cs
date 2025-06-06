using Microsoft.Win32;
using System.Windows;
using System.IO;
using System.Windows.Shapes;
using Echo.Models;
using Echo.Util;

namespace Echo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainWindowViewModel _viewModel;


        public MainWindow(MainWindowViewModel vm)
        {
            InitializeComponent();

            _viewModel = vm;
            DataContext = _viewModel;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Start();
        }

        private void Record(object sender, RoutedEventArgs e)
        {
            _viewModel.Record();
        }

        private void Save(object sender, RoutedEventArgs e)
        {
            _viewModel.Save();
        }

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select one or more files",
                Filter = "Json Files (*.json)|*.json|All Files (*.*)|*.*",
                Multiselect = false,
                InitialDirectory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Echo", "Ocrams")
            };

            if (dlg.ShowDialog() ?? false)
            {
                _viewModel.CurrentOrcam = await SerializationHelper.DeserializeFromFileAsync<Orcam>(dlg.FileName);
            }
        }

        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Stop();

        }
    }
}