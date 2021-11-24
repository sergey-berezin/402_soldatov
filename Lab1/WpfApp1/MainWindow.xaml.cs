using System;
using System.Windows;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {

        private void RecognitionStop_Click(object sender, RoutedEventArgs e)
        {
            if (RecognitionViewModel != null && RecognitionViewModel.RecognitionStatus == true)
            {
                RecognitionViewModel.Stop();
            }
        }

        private void FolderOpeningButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecognitionViewModel != null && RecognitionViewModel.RecognitionStatus == false && RecognitionViewModel.DatabaseCleaningStatus == false)
            {
                var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();

                if (dialog.ShowDialog(this).GetValueOrDefault())
                {
                    DirectoryName.Text = dialog.SelectedPath;
                    RecognitionViewModel.ChosenDirectoryPath = dialog.SelectedPath;
                }

                RecognitionViewModel.RecognitionStatus = false;
                RecognitionViewModel.NewOpeningAndRecognition();
            }
        }

        private void AllClassLabelsListBoxChanged(object sender, EventArgs e)
        {
            RecognitionViewModel.SingleClassLabelCollection.Clear();
            if (AllClassLabelsListBox.SelectedItem is AllClassLabels ClassLabelElement)
            {
                RecognitionViewModel.CollectionFilter(ClassLabelElement);
            }
        }

        private void DataBaseCleaningButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecognitionViewModel != null && RecognitionViewModel.RecognitionStatus == false && RecognitionViewModel.DatabaseCleaningStatus == false)
            {
                RecognitionViewModel.DatabaseCleaning();
            }
        }

        public RecognitionViewModel RecognitionViewModel;

        public MainWindow()
        {
            InitializeComponent();
            RecognitionViewModel = new RecognitionViewModel();
            this.DataContext = RecognitionViewModel;
            RecognitionViewModel.RecognitionStatus = false;
            RecognitionViewModel.DatabaseCleaningStatus = false;
        }
    }
}
