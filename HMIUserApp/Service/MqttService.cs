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
        public void ConnectDevice(string iotHubHostName, string deviceId, string deviceKey)
        {
            var deviceAuthentication = new DeviceAuthenticationWithRegistrySymmetricKey(deviceId, deviceKey);
            _deviceClient = DeviceClient.Create(iotHubHostName, deviceAuthentication, TransportType.Mqtt);
        }
        public async Task SendMessage(ModbusData data)
        {
            string messageString = JsonConvert.SerializeObject(data);
            Message message = new Message(Encoding.ASCII.GetBytes(messageString));
            
            await _deviceClient.SendEventAsync(message);

        }
    }
}
