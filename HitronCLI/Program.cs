using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace HitronCLI
{
    class Program
    {
        static byte[] AdditionalEntropy = { 121, 231, 14, 137, 146, 122 };
        static int RefreshIntervalSeconds = 10;

        // %userprofile%\AppData\Roaming\HitronCLI\encrypted_credentials.dat
        private static string GetPasswordPath()
        {
            string programDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "HitronCLI");
            Directory.CreateDirectory(programDirectory);
            return Path.Combine(programDirectory, "encrypted_credentials.dat");
        }



        static String ReadPasswordFromConsole()
        {
            // Note: it might be better to use SecureString but it's a huge hassle. Further reading:
            // Storing SecureString values in ProtectedData: https://stackoverflow.com/a/54032680/497403
            // Comparing SecureString values: https://stackoverflow.com/q/4502676/497403
            StringBuilder password = new StringBuilder();
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    break;
                }
                password.Append(key.KeyChar);
            }

            return password.ToString();
        }
        static void SetPassword()
        {
            Console.WriteLine("Please enter your modem password:");
            String password1 = ReadPasswordFromConsole();
            Console.WriteLine("Please re-enter your modem password:");
            String password2 = ReadPasswordFromConsole();
            if (password1 != password2)
            {
                Console.WriteLine("Error: passwords do not match");
                return;
            }
            byte[] encrypted = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(password1),
                AdditionalEntropy,
                DataProtectionScope.CurrentUser);

            File.WriteAllBytes(GetPasswordPath(), encrypted);
            Console.WriteLine("Password saved!");
        }

        static string SignIn(WebClient webClient)
        {
            if (!File.Exists(GetPasswordPath()))
            {
                Console.WriteLine("No password was saved - please run the tool with the 'set_password' command first");
                return null;
            }

            byte[] encrypted = File.ReadAllBytes(GetPasswordPath());
            byte[] decrypted = ProtectedData.Unprotect(encrypted, AdditionalEntropy, DataProtectionScope.CurrentUser);

            webClient.Headers.Add("content-type", "application/x-www-form-urlencoded; charset=UTF-8");
            string data = String.Format("model=%7B%22username%22%3A%22cusadmin%22%2C%22password%22%3A%22{0}%22%7D", Encoding.UTF8.GetString(decrypted));
            string result = webClient.UploadString("http://192.168.0.1/1/Device/Users/Login", data);
            JObject resultObject = (JObject)JsonConvert.DeserializeObject(result);
            if (!resultObject.ContainsKey("result") || resultObject["result"].ToString() != "success")
            {
                Console.WriteLine("Failed to login into the modem: " + result);
                return null;
            }

            return webClient.ResponseHeaders[HttpResponseHeader.SetCookie];
        }

        static JObject GetStats(WebClient webClient, string endpoint, long timestamp)
        {
            for (int retry = 0; retry < 2; retry++)
            {
                string url = String.Format("http://192.168.0.1/1/Device/CM/{0}?_={1}", endpoint, timestamp);
                string rawResult = webClient.DownloadString(url);
                JObject result = null;
                if (!rawResult.StartsWith("<!DOCTYPE html>"))
                {
                    try
                    {
                        result = (JObject)JsonConvert.DeserializeObject(rawResult);
                    }
                    catch
                    {

                    }
                }
                if (result != null)
                {
                    return result;
                }
                string cookie = SignIn(webClient);
                if (cookie == null)
                {
                    return null;
                }

                webClient.Headers.Add(HttpRequestHeader.Cookie, cookie);
            }
            return null;
        }

        static void CollectStats()
        {
            Dictionary<string, List<HitronStat>> allStats = new Dictionary<string, List<HitronStat>>();

            using (WebClient webClient = new WebClient())
            {
                System.Net.ServicePointManager.Expect100Continue = false;


                while (true)
                {
                    long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                    foreach (var endpoint in HitronStat.StatMapping.Keys)
                    {
                        List<HitronStat> currentStatList = new List<HitronStat>();
                        JObject stats = GetStats(webClient, endpoint, timestamp);
                        JArray list = (JArray)stats["Freq_List"];
                        foreach (JObject entry in list)
                        {
                            currentStatList.Add(HitronStat.FromJObject(endpoint, entry));
                        }

                        if (!allStats.ContainsKey(endpoint))
                        {
                            allStats[endpoint] = currentStatList;
                            HitronStat.PrintList(currentStatList);
                            continue;
                        }

                        bool hasChanges = false;
                        List<HitronStat> previousStatList = allStats[endpoint];
                        if (previousStatList.Count != currentStatList.Count)
                        {
                            Console.WriteLine("Warning: stat length changed");
                            HitronStat.PrintList(currentStatList);
                            continue;
                        }
                        for (int i = 0; i < previousStatList.Count; i++)
                        {
                            HitronStat stat1 = previousStatList[i];
                            HitronStat stat2 = currentStatList[i];
                            if (!stat1.Equals(stat2))
                            {
                                hasChanges = true;
                            }
                        }
                        if (hasChanges)
                        {
                            Console.WriteLine("Changes detected for " + endpoint);
                            HitronStat.PrintList(currentStatList);
                            allStats[endpoint] = currentStatList;
                        }
                    }

                    Thread.Sleep(1000 * RefreshIntervalSeconds);
                }
            }
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage: HitronCLI [command]");
            Console.WriteLine("       command is one of:");
            Console.WriteLine("       - stats");
            Console.WriteLine("       - set_password");
        }

        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                PrintUsage();
                return;
            }

            switch (args[0].ToLowerInvariant())
            {
                case "set_password":
                    SetPassword();
                    break;

                case "stats":
                    CollectStats();
                    break;

                default:
                    Console.WriteLine("Invalid command: " + args[0]);
                    PrintUsage();
                    break;
            }
        }
    }
}
