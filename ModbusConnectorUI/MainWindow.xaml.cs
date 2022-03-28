using EasyModbus;
using System;
using System.Collections.Generic;
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
using System.Timers;
using System.Windows.Threading;

namespace ModbusConnectorUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ModbusClient modbusClient;
        private DispatcherTimer aTimer;
        public MainWindow()
        {
            InitializeComponent();
            TxtIP.Text = "127.0.0.1";
            TxtPort.Text = "502";
            BtnDisconnect.IsEnabled = false;
            BtnReadStart.IsEnabled = false;
            BtnReadStop.IsEnabled = false;
            TxtStartAddress.Text = "30000";
            TxtQuantity.Text = "10";
            TxtInterval.Text = "1";
        }

        private void OnConnect(object sender, RoutedEventArgs e)
        {
            modbusClient = new ModbusClient(TxtIP.Text, Convert.ToInt16(TxtPort.Text));
            modbusClient.Connect();
            TxtLogs.Text = "Modbus Connected";
            BtnDisconnect.IsEnabled = true;
            BtnConnect.IsEnabled = false;
            BtnReadStart.IsEnabled = true;
        }

        private void OnDisconnect(object sender, RoutedEventArgs e)
        {
            if (aTimer != null)
            {
                aTimer.Stop();
                aTimer = null;
            }
            if (modbusClient != null)
            {
                modbusClient.Disconnect();
                modbusClient = null;
                BtnDisconnect.IsEnabled = false;
                BtnConnect.IsEnabled = true;
                BtnReadStart.IsEnabled = false;
                BtnReadStop.IsEnabled = false;
            }
            TxtLogs.Text = "Modbus Disconnected" + Environment.NewLine + TxtLogs.Text;
        }
        private void OnReadStart(object sender, RoutedEventArgs e)
        {
             
            aTimer = new DispatcherTimer();
            aTimer.Interval = TimeSpan.FromMilliseconds(Convert.ToInt32(Convert.ToDecimal(TxtInterval.Text) * 1000));
            aTimer.Tick += OnTimedEvent;
            aTimer.Start();

            BtnReadStart.IsEnabled = false;
            BtnReadStop.IsEnabled = true;
        }
        private void OnReadStop(object sender, RoutedEventArgs e)
        {
            if(aTimer != null) {
                aTimer.Stop();
                aTimer = null;
            }
            
            BtnReadStart.IsEnabled = true;
            BtnReadStop.IsEnabled = false;
        }
        private void OnTimedEvent(Object source, EventArgs e)
        {
            int startAddress = 0; 
            int quantity = 1;
            try
            {
                startAddress = Convert.ToInt32(TxtStartAddress.Text);
                quantity = Convert.ToInt32(TxtQuantity.Text);
            } catch { 
            }
            
            if (startAddress < 10000)
            {
                // read coils
                var result = modbusClient.ReadCoils(startAddress, quantity);
                TxtLogs.Text = String.Join(", ", result) + Environment.NewLine + TxtLogs.Text;
            } else if (startAddress >= 10000 && startAddress < 30000)
            {
                // read digital inputs
                var result = modbusClient.ReadDiscreteInputs(startAddress-10000, quantity);
                TxtLogs.Text = String.Join(", ", result) + Environment.NewLine + TxtLogs.Text;
            } else if (startAddress >= 30000 && startAddress < 40000)
            {
                // read analog inputs
                var result = modbusClient.ReadInputRegisters(startAddress - 30000, quantity);
                TxtLogs.Text = String.Join(", ", result) + Environment.NewLine + TxtLogs.Text;
            } else if (startAddress >= 40000 && startAddress < 60000)
            {
                // read holding registers
                var result = modbusClient.ReadHoldingRegisters(startAddress - 40000, quantity);
                TxtLogs.Text = String.Join(", ", result) + Environment.NewLine + TxtLogs.Text;
            }
            
        }
    }
}
