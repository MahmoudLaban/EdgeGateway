using HMIUserApp.Model;
using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ToggleSwitch;

namespace HMIUserApp.Pages
{
    /// <summary>
    /// Interaction logic for InsightPage.xaml
    /// </summary>
    public partial class InsightPage : Page
    {
        private MainWindow mainWindow;
        private List<InsightLogData> liveData = new List<InsightLogData>();
        public InsightPage()
        {
            InitializeComponent();
            mainWindow = (MainWindow)Application.Current.MainWindow;
            if (!mainWindow.isSaveCsv)
            {
                tsUpload.IsChecked = false;
                tsUpload.IsEnabled = false;
            }
            liveData = new List<InsightLogData>();
            InsightLogs.ItemsSource = liveData;
            FetchHistoryData();
        }
        private void OnChecked(object sender, RoutedEventArgs e)
        {
            HorizontalToggleSwitch toggleSwitch = (HorizontalToggleSwitch)sender;
            if (toggleSwitch.Tag?.ToString() == "UploadCSVDATA")
            {
                mainWindow.isUploadCsv = true;
            }

        }
        private void OnUnChecked(object sender, RoutedEventArgs e)
        {
            HorizontalToggleSwitch toggleSwitch = (HorizontalToggleSwitch)sender;
            if (toggleSwitch.Tag?.ToString() == "UploadCSVDATA")
            {
                mainWindow.isUploadCsv = false;
            }
        }
        private async void OnUploadCsvAsync(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV Files (*.csv)|*.csv";
            if (openFileDialog.ShowDialog() == true)
            {
                BtnUpload.Content = "Uploading...";
                BtnUpload.Background = Brushes.OrangeRed;

                await mainWindow.insightService.UploadCSV(openFileDialog.FileName);
                BtnUpload.Content = "Upload";
                BrushConverter bc = new BrushConverter();
                BtnUpload.Background = (Brush)bc.ConvertFrom("#009788");
                FetchHistoryData();
            }
        }

        private void GotoMainPage(object sender, RoutedEventArgs e)
        {
            mainWindow.MainFrame.Content = mainWindow.MainPage;
        }
        
        public void FetchHistoryData()
        {
            liveData.Clear();
            InsightLogs.Items.Refresh();
            string[] lines = File.ReadAllLines(mainWindow.appSettings.InsightLogFileName);
            

            liveData = lines.Select(line =>
            {
                string[] data = line.Split(',');
                // We return a person with the data in order.
                return new InsightLogData
                {
                    FileName = data[0],
                    Status = data[1],
                    UploadedDateTime = data[2]
                };
            }).ToList();
            InsightLogs.ItemsSource = liveData;
            InsightLogs.Items.Refresh();
        }

        private void OnConnect(object sender, RoutedEventArgs e)
        {
            if (TxtIoTHubName.Text == "")
            {
                lblError.Text = "Please fill IoTHub name";
                return;
            }
            if (TxtDeviceId.Text == "")
            {
                lblError.Text = "Please fill Device ID";
                return;
            }
            if (TxtDeviceKey.Text == "")
            {
                lblError.Text = "Please fill Device key";
                return;
            }
            mainWindow.mqttService.ConnectDevice(TxtIoTHubName.Text, TxtDeviceId.Text, TxtDeviceKey.Text);
        }
        private async void OnDisConnect(object sender, RoutedEventArgs e)
        {
            await mainWindow.mqttService.DisconnectDeviceAsync();
        }
    }
}
