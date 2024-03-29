﻿using EdgeGatewayApp.Helpers;
using EdgeGatewayApp.Pages;
using RestSharp;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace EdgeGatewayApp.Service
{
    public class HttpPublishingService
    {
        private AppSettings appSettings;
        private MainWindow _mainWindow;
        public HttpPublishingService(AppSettings _appSettings, MainWindow mainWindow)
        {
            appSettings = _appSettings;
            _mainWindow = mainWindow;
        }

        // publish csv data to InSight
        public async Task<string> UploadCSV(string pathString)
        {
            if (File.Exists(pathString))
            {
                string content;
                using (var reader = new StreamReader(pathString))
                {
                    content = reader.ReadToEnd();
                }
                var client = new RestClient("https://online.wonderware.eu/apis/");
                var request = new RestRequest("upload/datasource", Method.Post);
                request.AddHeader("Authorization", appSettings.InsightApiKey);
                request.AddHeader("x-filename", Path.GetFileName(pathString));
                request.AddHeader("Content-Type", "text/plain");

                request.AddParameter("text/plain", content, ParameterType.RequestBody);

                var response = await client.ExecuteAsync(request);
                string fileName = Path.GetFileName(pathString);
                if (response.StatusCode == HttpStatusCode.Accepted)
                {
                    string archiveFilePath = Path.Combine(appSettings.ArchiveFolderName, fileName);
                    Directory.CreateDirectory(appSettings.ArchiveFolderName);
                    File.Move(pathString, archiveFilePath);
                    using (StreamWriter sw = File.AppendText(appSettings.InsightLogFileName))
                    {
                        sw.WriteLine($"{fileName}, {"UPLOAD SUCCESSFUL"}, {DateTime.Now.ToString()}");
                    }
                    ((InsightPage)_mainWindow.InsightPage).FetchHistoryData();
                    return $"Uploaded {fileName} successfully";
                }
                else
                {
                    using (StreamWriter sw = File.AppendText(appSettings.InsightLogFileName))
                    {
                        sw.WriteLine($"{fileName}, {"UPLOAD FAILED"}, {DateTime.Now.ToString()}");
                    }
                    ((InsightPage)_mainWindow.InsightPage).FetchHistoryData();
                    return $"Uploading {fileName} failed";
                }

            }
            
            return "File does not exist, please select correct file";
        }
    }
}
