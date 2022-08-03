using EdgeGatewayApp.Helpers;
using EdgeGatewayApp.Model;
using EdgeGatewayApp.Pages;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using Newtonsoft.Json;
using System;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EdgeGatewayApp.Service
{
    public class HiveMQTTPubSubServices
    {
        private AppSettings _appSettings;
        private IMqttClient _hiveMqttClient;
        private MqttFactory _mqttFactory;
        private MainWindow _mainWindow;
        private string _pubTopic;
        private string _subTopic;
        
        public HiveMQTTPubSubServices(AppSettings appSettings, MainWindow mainWindow)
        {
            _appSettings = appSettings;
            _mainWindow = mainWindow;
            _mqttFactory = new MqttFactory();
        }
        public async Task<bool> ConnectHiveMqttServer(string pubTopic, string subTopic)
        {
            try
            {
                _pubTopic = pubTopic;
                _subTopic = subTopic;
                
                _hiveMqttClient = _mqttFactory.CreateMqttClient();

                var mqttClientOptions = new MqttClientOptionsBuilder()
                    .WithTcpServer("broker.hivemq.com")
                    .WithProtocolVersion(MqttProtocolVersion.V500) // use 5.0 protocol
                    .Build();

                _hiveMqttClient.ApplicationMessageReceivedAsync += e =>
                {
                    var output = System.Text.Json.JsonSerializer.Serialize(e, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    string payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                    
                    SubscribeMessage(payload);
                    return Task.CompletedTask;
                };

                // This will throw an exception if the server is not available.
                // The result from this message returns additional data which was sent 
                // from the server. Please refer to the MQTT protocol specification for details.
                var response = await _hiveMqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

                var mqttSubscribeOptions = _mqttFactory.CreateSubscribeOptionsBuilder()
                    .WithTopicFilter(f => { f.WithTopic(_subTopic); })
                    .Build();

                await _hiveMqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);

                
                return true;
            }
            catch (Exception) {
                Console.WriteLine("Error");
                return false;
            }
        }
       
        public async Task PublishMessage(MqttData data)
        {
            string msgString = JsonConvert.SerializeObject(data);

            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic(_pubTopic)
                .WithPayload(msgString)
                .Build();
            await _hiveMqttClient.PublishAsync(applicationMessage, CancellationToken.None);

        }

        public void SubscribeMessage(string payload)
        {
            ModbusData data = JsonConvert.DeserializeObject<ModbusData>(payload);
            // set value on modbus simulator
            ((MainPage)_mainWindow.MainPage).SetValueFromAzureIoTHub(data);
        }
        public async Task DisconnectHiveMqttServerAsync()
        {
            if (_hiveMqttClient != null)
            {
                var mqttClientDisconnectOptions = _mqttFactory.CreateClientDisconnectOptionsBuilder().Build();

                await _hiveMqttClient.DisconnectAsync(mqttClientDisconnectOptions, CancellationToken.None);

                _hiveMqttClient = null;
            }
        }
        
    }
}
