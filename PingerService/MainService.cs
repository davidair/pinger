/*
Copyright 2020 Google Inc.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

   https://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using PingerCore;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace PingerService
{
    public partial class MainService : ServiceBase
    {
        private static string EventSource = "DavidAir Pinger";
        private static string FirebaseProjectName = String.Empty;

        const string DatabasePath = @"D:\Pinger\pings.sqlite";
        public MainService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", @"d:\Pinger\pinger-service-account-key.json");
            FirebaseProjectName = File.ReadAllText(@"d:\Pinger\firebase-project-name.txt").Trim();

            FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.GetApplicationDefault(),
            });

            if (!EventLog.SourceExists(EventSource))
            {
                EventLog.CreateEventSource(EventSource, "Application");
            }

            Thread thread = new Thread(DoWork);
            thread.IsBackground = true;
            thread.Start();
        }

        private void GetWeatherAndSaveToFirebase(string accessToken, string weatherAppId)
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    // Get weather for Kitchener
                    var data = client.DownloadString("https://api.openweathermap.org/data/2.5/weather?id=5992996&appid=" + weatherAppId);
                    string bucket = DateTime.Now.ToString("yyyy-MM-dd");
                    client.UploadString(string.Format("https://{2}.firebaseio.com/weather/{0}.json?access_token={1}", bucket, accessToken, FirebaseProjectName), data);
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(EventSource, "Failed to fetch or save weather: " + ex.Message, EventLogEntryType.Error);
            }
        }

        private void SavePingToFirebase(int milliseconds, string accessToken)
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    string bucket = DateTime.Now.ToString("yyyy-MM-dd");
                    var pingData = new { ping = milliseconds, time = DateTime.Now };
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(pingData);
                    client.UploadString(string.Format("https://{2}.firebaseio.com/raw_pings/{0}.json?access_token={1}", bucket, accessToken, FirebaseProjectName), json);
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(EventSource, "Failed to save ping data to Firebase: " + ex.Message, EventLogEntryType.Error);
            }
        }

        private void DoWork()
        {
            DateTime lastWeatherQuery = DateTime.MinValue;
            DateTime lastAccessTokenFetch = DateTime.MinValue;
            string accessToken = null;

            string weatherAppId = File.ReadAllText(@"D:\Pinger\weather_app_id.txt").Trim();

            while (true)
            {
                try
                {
                    if (!File.Exists(DatabasePath))
                    {
                        EventLog.WriteEntry(EventSource, "Ping database does not exist and will be created: " + DatabasePath, EventLogEntryType.Information);
                    }
                    PingSender.SendPing("google.com", 1000, (milliseconds) => {
                        // Save to local SQLite database
                        Database.WritePingStats(milliseconds, DatabasePath);

                        // Save to Firebase. Only fetch access token once an hour.
                        if (accessToken == null || DateTime.Now.Subtract(lastAccessTokenFetch).TotalMinutes > 60)
                        {
                            Task<string> task = FirebaseApp.DefaultInstance.Options.Credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
                            task.ContinueWith(x => {
                                accessToken = x.Result;
                                lastAccessTokenFetch = DateTime.Now;
                                lastWeatherQuery = DateTime.Now;
                                GetWeatherAndSaveToFirebase(accessToken, weatherAppId);
                                SavePingToFirebase(milliseconds, x.Result);
                            });
                        }
                        else
                        {
                            SavePingToFirebase(milliseconds, accessToken);
                            if (DateTime.Now.Subtract(lastWeatherQuery).TotalMinutes > 1)
                            {
                                GetWeatherAndSaveToFirebase(accessToken, weatherAppId);
                                lastWeatherQuery = DateTime.Now;
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    EventLog.WriteEntry(EventSource, "Failed to process ping: " + ex.Message, EventLogEntryType.Error);
                }
            }
        }
    }
}
