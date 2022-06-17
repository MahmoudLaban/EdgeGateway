using Newtonsoft.Json;
using System.IO;

namespace HMIUserApp.Helpers
{
    public class ConfigurationHelper
    {
        public static AppSettings LoadJson()
        {
            using (StreamReader r = new StreamReader("appsettings.json"))
            {
                string json = r.ReadToEnd();
                var appSettings = JsonConvert.DeserializeObject<AppSettings>(json);
                return appSettings;
            }
        }

    }
    public class AppSettings
    {
        public string HistoryFolderName { get; set; }
        public string ArchiveFolderName { get; set; }
        public string InsightLogFileName { get; set; }
        public string InsightApiKey { get; set; }
    }
}
