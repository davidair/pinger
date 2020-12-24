using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace HitronCLI
{
    class Program
    {
        static byte[] AdditionalEntropy = { 121, 231, 14, 137, 146, 122 };

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
            using (WebClient webClient = new WebClient())
            {
                System.Net.ServicePointManager.Expect100Continue = false;
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                foreach (var endpoint in new[] {"SysInfo", "DsInfo", "UsInfo", "DsOfdm", "UsOfdm" })
                {
                    JObject stats = GetStats(webClient, endpoint, timestamp);
                    Console.WriteLine(stats.ToString());
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
