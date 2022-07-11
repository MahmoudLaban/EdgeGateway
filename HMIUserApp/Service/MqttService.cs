using HMIUserApp.Helpers;
using HMIUserApp.Model;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace HMIUserApp.Service
{
    public class MqttService
    {
        private AppSettings _appSettings;
        private DeviceClient _deviceClient;
        
        public MqttService(AppSettings appSettings)
        {
            _appSettings = appSettings;
        }
        public async Task<bool> ConnectDevice(string iotHubHostName, string deviceId, string deviceKey)
        {
            try
            {
                var deviceAuthentication = new DeviceAuthenticationWithRegistrySymmetricKey(deviceId, deviceKey);
                _deviceClient = DeviceClient.Create(iotHubHostName, deviceAuthentication, TransportType.Mqtt);
                _deviceClient.SetConnectionStatusChangesHandler(ConnectionStatusChangesHandler);
                
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

        }
        public void DisconnectDevice()
        {
            if (_deviceClient != null)
            {
                _deviceClient.Dispose();
                _deviceClient = null;
            }
        }
        public async Task SendMessageAsync(MqttData data)
        {
            string messageString = JsonConvert.SerializeObject(data);
            Message message = new Message(Encoding.ASCII.GetBytes(messageString));

            await _deviceClient.SendEventAsync(message);
            Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);

        }
    }
}
