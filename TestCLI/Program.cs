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
using System;
using System.Net;
using Nito.AsyncEx;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TestCLI
{
    class Program
    {
        static string FirebaseDatabaseUrl = String.Empty;
        enum Operation
        {
            Read,
            Delete,
            Update
        }
        static void Main(string[] args)
        {
            AsyncContext.Run(() => MainAsync(args));
        }

        private static Uri MakeDatabaseUri(string path, string accessToken)
        {
            return new Uri(new Uri(FirebaseDatabaseUrl), path + ".json?access_token=" + accessToken);
        }

        // https://firebase.google.com/docs/reference/rest/database#section-api-usage
        private static string RunFirebaseOperationInternal(WebClient client, Operation operation, string path, string payload, string accessToken)
        {
            switch (operation)
            {
                case Operation.Read:
                    return client.DownloadString(MakeDatabaseUri(path, accessToken));

                case Operation.Delete:
                    client.UploadValues(MakeDatabaseUri(path, accessToken), "DELETE", new NameValueCollection());
                    return null;

                case Operation.Update:
                    client.UploadString(MakeDatabaseUri(path, accessToken), payload);
                    return null;

                default:
                    throw new ApplicationException("Invalid operation specified: " + operation);
            }            
        }

        private async static Task<string> RunFirebaseOperation(Operation operation, string path, string payload)
        {
            Task<string> task = FirebaseApp.DefaultInstance.Options.Credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
            string accessToken = await task;
            using (WebClient client = new WebClient())
            {
                return RunFirebaseOperationInternal(client, operation, path, payload, accessToken);
            }
        }

        async static void DeleteDummyData(string path)
        {
            string data = File.ReadAllText(path);
            JObject contents = (JObject)JsonConvert.DeserializeObject(data);
            foreach (var entry in contents)
            {
                Console.WriteLine(entry.Key);
                await RunFirebaseOperation(Operation.Delete, "/raw_pings/2999-01-01/" + entry.Key, null);                
            }
        }

        async static void TestInsertReadDelete()
        {
            string data = await RunFirebaseOperation(Operation.Read, "/raw_pings/2999-01-01/-MKNkqMNkCV4ENXk7E6_", null);
            Console.WriteLine(data);
            await RunFirebaseOperation(Operation.Delete, "/raw_pings/2999-01-01/-MKNkqMNkCV4ENXk7E6_", null);
            data = await RunFirebaseOperation(Operation.Read, "/raw_pings/2999-01-01/-MKNkqMNkCV4ENXk7E6_", null);
            Console.WriteLine(data);
            var payload = JsonConvert.SerializeObject(new { ping = 23, time = DateTime.Now });
            await RunFirebaseOperation(Operation.Update, "/raw_pings/2999-01-01/-MKNkqMNkCV4ENXk7E6_", payload);
            data = await RunFirebaseOperation(Operation.Read, "/raw_pings/2999-01-01/-MKNkqMNkCV4ENXk7E6_", null);
            Console.WriteLine(data);
            await RunFirebaseOperation(Operation.Delete, "/raw_pings/2999-01-01/-MKNkqMNkCV4ENXk7E6_", null);
        }

        static void MainAsync(string[] args)
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", @"d:\Pinger\pinger-service-account-key.json");
            FirebaseDatabaseUrl = String.Format("https://{0}.firebaseio.com",
                File.ReadAllText(@"d:\Pinger\firebase-project-name.txt").Trim());
            FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.GetApplicationDefault(),
            });

            TestInsertReadDelete();
        }
    }
}
