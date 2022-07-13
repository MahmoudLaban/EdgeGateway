using HMIUserApp.Helpers;
using HMIUserApp.Model;
using HMIUserApp.Pages;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HMIUserApp.Service
{
    public class MqttPublishSubscribeServices
    {
        private AppSettings _appSettings;
        private DeviceClient _deviceClient;
        private MainWindow _mainWindow;
        public bool isConnected { get; set; }
        
        public MqttPublishSubscribeServices(AppSettings appSettings, MainWindow mainWindow)
        {
            _appSettings = appSettings;
            _mainWindow = mainWindow;
            isConnected = false;
        }
        public async Task<bool> ConnectDevice(string iotHubHostName, string deviceId, string deviceKey)
        {
            try
            {
                var deviceAuthentication = new DeviceAuthenticationWithRegistrySymmetricKey(deviceId, deviceKey);
                _deviceClient = DeviceClient.Create(iotHubHostName, deviceAuthentication, TransportType.Mqtt);
                _deviceClient.SetConnectionStatusChangesHandler(ConnectionStatusChangesHandler);
                await _deviceClient.SetReceiveMessageHandlerAsync(OnC2dMessageReceivedAsync, _deviceClient);

                var task = _deviceClient.OpenAsync();
                if (await Task.WhenAny(task, Task.Delay(10000)) == task)
                {
                    // task completed within timeout
                    return true;
                }
                else
                {
                    // timeout logic
                    return false;
                }

            }
            catch (Exception) {
                Console.WriteLine("Error");
                return false;
            }
        }
        
        private void ConnectionStatusChangesHandler(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
            isConnected = status == ConnectionStatus.Connected;
        }

        // publish modbus data to Azure IoT Hub
        public async Task SendMessageAsync(MqttData data)
        {
            string messageString = JsonConvert.SerializeObject(data);
            Message message = new Message(Encoding.ASCII.GetBytes(messageString));

            await _deviceClient.SendEventAsync(message);
            Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);

        }

        // subcribe message from Azure IoT hub
        private async Task OnC2dMessageReceivedAsync(Message receivedMessage, object _)
        {
            var jsonMsg = Encoding.ASCII.GetString(receivedMessage.GetBytes());
            await _deviceClient.CompleteAsync(receivedMessage);
            receivedMessage.Dispose();

            ModbusData data = JsonConvert.DeserializeObject<ModbusData>(jsonMsg);

            // set value on modbus simulator
            ((MainPage)_mainWindow.MainPage).SetValueFromAzureIoTHub(data);

        }

        public void DisconnectDevice()
        {
            if (_deviceClient != null)
            {
                _deviceClient = null;
            }
        }
        
    }
}
