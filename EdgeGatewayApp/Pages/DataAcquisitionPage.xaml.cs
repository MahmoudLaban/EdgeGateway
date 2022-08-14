using EasyModbus;
using EdgeGatewayApp.Model;
using EdgeGatewayApp.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ToggleSwitch;

namespace EdgeGatewayApp.Pages
{
    /// <summary>
    /// Interaction logic for DataAcquisitionPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        private ModbusClient modbusClient;
        private List<LogData> liveData;
        private string modbusTag = "AI";
        private DispatcherTimer aTimer;
        private DispatcherTimer csvTimer;
        private int timerInterval = 1;
        private readonly int csvTimerInterval = 2; // minutes - Time duration included in CSV file (max 10 min due to InSight API size limit)
        private readonly int quantity = 50;
        private bool isLive = true;
        private bool isSaveDatabase = true;
        
        private readonly LocalDataLoggingService _logDataService = new LocalDataLoggingService();
        private DateTime startDate;
        private DateTime endDate;

        private MainWindow mainWindow;
        public MainPage()
        {
            InitializeComponent();
            mainWindow = (MainWindow)Application.Current.MainWindow;

            liveData = new List<LogData>();
            DGLogs.ItemsSource = liveData;

            timerInterval = Convert.ToInt32(TxtInterval.Text);
            aTimer = new DispatcherTimer();
            aTimer.Interval = TimeSpan.FromMilliseconds(Convert.ToInt32(timerInterval * 1000));
            aTimer.Tick += OnTimedEvent;

            csvTimer = new DispatcherTimer();
            csvTimer.Interval = TimeSpan.FromMinutes(csvTimerInterval);
            csvTimer.Tick += OnCsvFileNameEvent;

            FetchMinMaxDate();

            if (!File.Exists(mainWindow.appSettings.InsightLogFileName))
            {
                using (StreamWriter sw = File.AppendText(mainWindow.appSettings.InsightLogFileName))
                {
                    sw.WriteLine($"FileName, Status, DateTime");
                }
            }
        }
        private void OnConnect(object sender, RoutedEventArgs e)
        {
            try
            {
                modbusClient = new ModbusClient(TxtIP.Text, Convert.ToInt16(TxtPort.Text));
                modbusClient.Connect();
                BtnDisconnect.IsEnabled = true;
                BtnConnect.IsEnabled = false;
                LblLog.Text = "Modbus connected";
                aTimer.Start();
                csvTimer.Start();
                mainWindow.csvFileName = $"ModbusData_{DateTime.Now.ToString("yyyy'_'MM'_'dd'_'HH'_'mm")}.csv";

            }
            catch (Exception ex)
            {
                LblLog.Text = ex.Message;
            }
        }

        private void OnDisconnect(object sender, RoutedEventArgs e)
        {
            if (modbusClient != null)
            {
                aTimer.Stop();
                csvTimer.Stop();
                modbusClient.Disconnect();
                modbusClient = null;
                BtnDisconnect.IsEnabled = false;
                BtnConnect.IsEnabled = true;
                LblLog.Text = "Modbus disconnected";
            }
        }

        private void OnSetValue(object sender, RoutedEventArgs e)
        {
            if (modbusClient != null && modbusClient.Connected)
            {
                var value = Convert.ToInt32(TxtValue.Text);
                ComboBoxItem selAddress = (ComboBoxItem)AddressList.SelectedItem;

                if (selAddress.Tag == null)
                {
                    LblLog.Text = "Please select Adress";
                }
                else
                {
                    int address = Convert.ToInt32(selAddress.Tag);
                    if (modbusTag == "CO")
                    {
                        modbusClient.WriteSingleCoil(address, value > 0);
                    }
                    else if (modbusTag == "AO")
                    {
                        modbusClient.WriteSingleRegister(address, value);
                    }
                    LblLog.Text = "Set value successfully";
                }

            }
            else
            {
                MessageBox.Show("Please connect Modbus device", "Warning");
            }
        }

        public void SetValueFromAzureIoTHub(ModbusData data)
        {
            if (modbusClient != null && modbusClient.Connected)
            {
                var iValue = Convert.ToInt32(data.Value);
                int iAddress = Convert.ToInt32(data.ModbusAddress);
                if (data.TagName == "CO")
                {
                    modbusClient.WriteSingleCoil(iAddress, iValue > 0);
                }
                else if (data.TagName == "AO")
                {
                    modbusClient.WriteSingleRegister(iAddress, iValue);
                }
                //LblLog.Text = "Set value successfully";
            }
        }
        private async void OnDelete(object sender, RoutedEventArgs e)
        {
            MainPageUI.IsEnabled = false;
            BtnDelete.Content = "Deleting in Progress";
            BtnDelete.Background = Brushes.OrangeRed;
            LblLog.Text = "Deleting in Progress";
            await Task.Run(() => _logDataService.DeleteRecord(startDate, endDate));
            MainPageUI.IsEnabled = true;
            BtnDelete.Content = "Delete";
            LblLog.Text = "Deleted successfully";
            BrushConverter bc = new BrushConverter();
            BtnDelete.Background = (Brush)bc.ConvertFrom("#009788");
            FetchMinMaxDate();
            FetchHistoryData();
        }
        private void OnHistoryRefresh(object sender, RoutedEventArgs e)
        {
            FetchHistoryData();
        }
        private void OnChecked(object sender, RoutedEventArgs e)
        {
            HorizontalToggleSwitch toggleSwitch = (HorizontalToggleSwitch)sender;
            if (toggleSwitch.Tag?.ToString() == "LiveHistory")
            {
                Console.WriteLine("Live");
                isLive = true;
                if (modbusClient != null && modbusClient.Connected) aTimer.Start();
                BorderCalendar.Visibility = Visibility.Hidden;
                BorderSetValue.Visibility = Visibility.Hidden;
            }
            else if (toggleSwitch.Tag?.ToString() == "SaveDatabase")
            {
                isSaveDatabase = true;
            }
            else if (toggleSwitch.Tag?.ToString() == "SaveCsv")
            {
                mainWindow.isSaveCsv = true;
            }

        }
        private void OnUnChecked(object sender, RoutedEventArgs e)
        {
            HorizontalToggleSwitch toggleSwitch = (HorizontalToggleSwitch)sender;
            if (toggleSwitch.Tag?.ToString() == "LiveHistory")
            {
                isLive = false;
                aTimer.Stop();
                BorderCalendar.Visibility = Visibility.Visible;
                BorderSetValue.Visibility = Visibility.Hidden;
                FetchHistoryData();
            }
            else if (toggleSwitch.Tag?.ToString() == "SaveDatabase")
            {
                isSaveDatabase = false;
            }
            else if (toggleSwitch.Tag?.ToString() == "SaveCsv")
            {
                mainWindow.isSaveCsv = false;
                mainWindow.isUploadCsv = false;
            }
        }
        private void TagsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            ComboBoxItem selectedItem = (ComboBoxItem)comboBox.SelectedItem;

            if (selectedItem.Tag != null)
            {
                modbusTag = selectedItem.Tag.ToString();
                if (isLive)
                {
                    liveData.Clear();
                    DGLogs.Items.Refresh();
                    if (modbusTag == "AO" || modbusTag == "CO")
                    {
                        BorderSetValue.Visibility = Visibility.Visible;
                        RefreshAddressList();
                    }
                    else
                    {
                        BorderSetValue.Visibility = Visibility.Hidden;
                    }
                }
                else
                {
                    FetchHistoryData();
                }

            }
        }
        private void DeviceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }

        private void RefreshAddressList()
        {
            AddressList.Items.Clear();
            for (int i = 0; i < quantity; i++)
            {
                ComboBoxItem item = new ComboBoxItem();
                if (i == 0)
                {
                    item.IsSelected = true;
                }
                item.Tag = i.ToString();
                item.Content = modbusTag + String.Format("{0:D5}", i + 1);
                AddressList.Items.Add(item);
            }

        }
        private void OnTimedEvent(Object source, EventArgs e)
        {
            Console.WriteLine("KKKKKKKKKKKKKKKK");
            try
            {
                liveData.Clear();
                DGLogs.Items.Refresh();

                var inputResult = modbusClient.ReadInputRegisters(0, quantity);
                for (int i = 0; i < inputResult.Length; i++)
                {
                    AddLogData(30001 + i, inputResult[i], "AI", i);
                }

                var holdingResult = modbusClient.ReadHoldingRegisters(0, quantity);
                for (int i = 0; i < holdingResult.Length; i++)
                {
                    AddLogData(40001 + i, holdingResult[i], "AO", i);
                }

                var coilResult = modbusClient.ReadCoils(0, quantity);
                for (int i = 0; i < coilResult.Length; i++)
                {
                    AddLogData(i + 1, coilResult[i] ? 1 : 0, "CO", i);
                }

                var decretResult = modbusClient.ReadDiscreteInputs(0, quantity);
                for (int i = 0; i < decretResult.Length; i++)
                {
                    AddLogData(10001 + i, decretResult[i] ? 1 : 0, "CI", i);
                }
                if (isSaveDatabase) _logDataService.Save();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                if (modbusClient != null)
                {
                    aTimer.Stop();
                    csvTimer.Stop();
                    modbusClient.Disconnect();
                    modbusClient = null;
                    BtnDisconnect.IsEnabled = false;
                    BtnConnect.IsEnabled = true;
                    LblLog.Text = "Modbus disconnected";
                }
            }

        }

        private void OnCsvFileNameEvent(Object source, EventArgs e)
        {
            if (mainWindow.isUploadCsv)
            {
                string pathString = System.IO.Path.Combine(mainWindow.appSettings.HistoryFolderName, mainWindow.csvFileName);
                _ = mainWindow.insightService.UploadCSV(pathString);
            }

            mainWindow.csvFileName = $"ModbusData_{DateTime.Now.ToString("yyyy'_'MM'_'dd'_'HH'_'mm")}.csv";
            
        }

        private void AddLogData(int address, int value, string tag, int i)
        {
            if (modbusTag == tag)
            {
                liveData.Add(new LogData
                {
                    DateTime = DateTime.Now,
                    Value = value.ToString(),
                    TagName = tag + String.Format("{0:D5}", i + 1),
                    ModbusAddress = String.Format("{0:D5}", address)
                });
                DGLogs.Items.Refresh();
            }
            if (isSaveDatabase)
            {
                _logDataService.AddRecord(new ModbusData
                {
                    DateTime = DateTime.Now,
                    Value = value.ToString(),
                    TagName = tag + String.Format("{0:D5}", i + 1),
                    ModbusAddress = String.Format("{0:D5}", address)
                });
            }
            if (mainWindow.isSaveCsv)
            {
                Directory.CreateDirectory(mainWindow.appSettings.HistoryFolderName);
                string pathString = System.IO.Path.Combine(mainWindow.appSettings.HistoryFolderName, mainWindow.csvFileName);
                if (File.Exists(pathString))
                {
                    using (StreamWriter sw = File.AppendText(pathString))
                    {
                        sw.WriteLine($"{tag + String.Format("{0:D5}", i + 1)}, {DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'")}, {value.ToString()}");
                    }
                }
                else
                {
                    using (StreamWriter sw = File.AppendText(pathString))
                    {
                        sw.WriteLine($"TagName, DateTime, Value");
                        sw.WriteLine($"{tag + String.Format("{0:D5}", i + 1)}, {DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'")}, {value.ToString()}");
                    }
                }

            }

            if (mainWindow.isUploadMqtt)
            {
                _ = mainWindow.mqttService.SendMessageAsync(new MqttData
                {
                    DateTime = DateTime.Now,
                    Value = value,
                    TagName = tag + String.Format("{0:D5}", i + 1),
                    ModbusAddress = String.Format("{0:D5}", address)
                });
            }
            if (mainWindow.isPublishHiveMqtt)
            {
                _ = mainWindow.hiveMqttService.PublishMessage(new MqttData
                {
                    DateTime = DateTime.Now,
                    Value = value,
                    TagName = tag + String.Format("{0:D5}", i + 1),
                    ModbusAddress = String.Format("{0:D5}", address)
                });
            }
        }

        private void EndDate_Changed(object sender, SelectionChangedEventArgs e)
        {
            endDate = EndDate.SelectedDate ?? DateTime.Now;
            if (!isLive) FetchHistoryData();
        }
        private void StartDate_Changed(object sender, SelectionChangedEventArgs e)
        {
            startDate = StartDate.SelectedDate ?? DateTime.Now;
            if (!isLive) FetchHistoryData();
        }

        private void FetchHistoryData()
        {
            liveData.Clear();
            DGLogs.Items.Refresh();
            var data = _logDataService.GetModbusDatas(modbusTag, startDate, endDate);
            data.ForEach(x =>
            {
                liveData.Add(new LogData
                {
                    DateTime = x.DateTime,
                    Value = x.Value,
                    TagName = x.TagName,
                    ModbusAddress = x.ModbusAddress
                });
                DGLogs.Items.Refresh();
            });
        }

        private void FetchMinMaxDate()
        {
            var minMaxDate = _logDataService.GetMinMaxDate();
            startDate = minMaxDate.Item1;
            endDate = minMaxDate.Item2;
            StartDate.DisplayDateStart = startDate;
            StartDate.DisplayDateEnd = endDate;
            StartDate.SelectedDate = startDate;
            EndDate.DisplayDateStart = startDate;
            EndDate.DisplayDateEnd = endDate;
            EndDate.SelectedDate = endDate;
        }

        private void TxtInterval_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                timerInterval = Convert.ToInt32(TxtInterval.Text);
                if (timerInterval < 1)
                {
                    LblLog.Text = "Storage interval should be filled with integer greater than 1";
                }
                if (LblLog != null)
                {
                    LblLog.Text = "";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                LblLog.Text = "Storage interval should be filled with integer greater than 1";
                timerInterval = 1;
            }

            if (aTimer != null && aTimer.IsEnabled)
            {
                aTimer.Stop();
                aTimer.Interval = TimeSpan.FromMilliseconds(Convert.ToInt32(timerInterval * 1000));
                aTimer.Start();
            }

        }

        private void GotoInsightPage(object sender, RoutedEventArgs e)
        {
            mainWindow.MainFrame.Content = mainWindow.InsightPage;
           ((InsightPage) mainWindow.InsightPage).FetchHistoryData();
        }
    }
}
