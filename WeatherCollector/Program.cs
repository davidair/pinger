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
using System;
using System.IO;
using System.Net;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace WeatherCollector
{
    class Program
    {
        private static string GetWeatherPath()
        {
            string dbDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Pinger");
            Directory.CreateDirectory(dbDirectory);
            return Path.Combine(dbDirectory, "weather.txt");
        }

        private static string GetWeather(string weatherAppId)
        {
            try
            {
                using (WebClient wc = new WebClient())
                {
                    // Get weather for Kitchener
                    var data = wc.DownloadString("https://api.openweathermap.org/data/2.5/weather?id=5992996&appid=" + weatherAppId);
                    File.AppendAllText(GetWeatherPath(), data + Environment.NewLine + Environment.NewLine);
                    return data;
                }
            }
            catch
            {
                return null;
            }
        }

        static void Main(string[] args)
        {
            while (true)
            {
                string weatherAppId = File.ReadAllText(@"D:\Pinger\weather_app_id.txt").Trim();
                string weatherData = GetWeather(weatherAppId);
                try
                {
                    dynamic weather = JObject.Parse(weatherData);
                    double temperature = weather.main.temp.Value - 273.15;
                    Console.WriteLine("Temperature: " + temperature);
                }
                catch
                {
                    Console.WriteLine("Unable to parse weather response");
                }
                Thread.Sleep(1000 * 60);
            }
        }
    }
}
