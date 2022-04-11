using EasyModbus;
using HMIUserApp.Model;
using HMIUserApp.Service;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Threading;
using ToggleSwitch;


namespace HMIUserApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ModbusClient modbusClient;
        private List<LogData> liveData;
        private string modbusTag = "AI";
        private DispatcherTimer aTimer;
        private DispatcherTimer csvTimer;
        private int timerInterval = 1;
        private readonly int csvTimerInterval = 10; // minutes
        private readonly int quantity = 50;
        private bool isLive = true;
        private bool isSaveDatabase = true;
        private bool isSaveCsv = true;
        private readonly string folderName = "HistoryData";
        private string csvFileName;
        private readonly LogDataService _logDataService = new LogDataService();
        private DateTime startDate;
        private DateTime endDate;
        public MainWindow()
        {
            InitializeComponent();
            
            liveData = new List<LogData>();
            DGLogs.ItemsSource = liveData;
                
            timerInterval = Convert.ToInt32(TxtInterval.Text);
            aTimer = new DispatcherTimer();
            aTimer.Interval = TimeSpan.FromMilliseconds(Convert.ToInt32(timerInterval* 1000));
            aTimer.Tick += OnTimedEvent;

            csvTimer = new DispatcherTimer();
            csvTimer.Interval = TimeSpan.FromMinutes(csvTimerInterval);
            csvTimer.Tick += OnCsvFileNameEvent;

            FetchMinMaxDate();


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
                csvFileName = $"ModbusData_{DateTime.Now.ToString("yyyy'_'MM'_'dd'_'HH'_'mm")}.csv";

            }catch(Exception ex)
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

        private async void OnDelete(object sender, RoutedEventArgs e)
        {
            UserApp.IsEnabled = false;
            BtnDelete.Content = "Deleting in Progress";
            BtnDelete.Background = Brushes.OrangeRed;
            LblLog.Text = "Deleting in Progress";
            await Task.Run(() => _logDataService.DeleteRecord(startDate, endDate));
            UserApp.IsEnabled = true;
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
            } else if (toggleSwitch.Tag?.ToString() == "SaveDatabase")
            {
                isSaveDatabase = true;
            } else
            {
                isSaveCsv = true;
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
            else
            {
                isSaveCsv = false;
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
                    
                } else
                {
                    FetchHistoryData();
                }
                
            }
        }
        
        private void RefreshAddressList()
        {
            AddressList.Items.Clear();
            for (int i = 0; i < quantity; i++)
            {
                ComboBoxItem item = new ComboBoxItem();
                if (i ==0)
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
                    AddLogData(i+1, coilResult[i] ? 1 : 0, "CO", i);
                }
                
                var decretResult = modbusClient.ReadDiscreteInputs(0, quantity);
                for (int i = 0; i < decretResult.Length; i++)
                {
                    AddLogData(10001 + i, decretResult[i] ? 1 : 0, "CI", i);
                }
                if (isSaveDatabase) _logDataService.Save();
            } catch(Exception ex)
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
            csvFileName = $"ModbusData_{DateTime.Now.ToString("yyyy'_'MM'_'dd'_'HH'_'mm")}.csv";
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
            if (isSaveCsv)
            {
                Directory.CreateDirectory(folderName);
                string pathString = System.IO.Path.Combine(folderName, csvFileName);
                if (File.Exists(pathString))
                {
                    using (StreamWriter sw = File.AppendText(pathString))
                    {
                        sw.WriteLine($"{tag + String.Format("{0:D5}", i + 1)}, {DateTime.Now.ToString()}, {value.ToString()}, {String.Format("{0:D5}", address)}");
                    }
                }
                else
                {
                    using (StreamWriter sw = File.AppendText(pathString))
                    {
                        sw.WriteLine($"TagName, DateTime, Value, ModbusAddrss");
                        sw.WriteLine($"{tag + String.Format("{0:D5}", i + 1)}, {DateTime.Now.ToString()}, {value.ToString()}, {String.Format("{0:D5}", address)}");
                    }
                }
                
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
            } catch(Exception ex)
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
    }
}
