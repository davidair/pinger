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
using System.Data.SQLite;
using System.IO;

namespace PingerCore
{
    /// <summary>
    /// Get drop rate:
    /// select 1.0 * F  / (F + count(*)) from Pings P cross join (select count(*) as F from Pings where response == -1) X where P.response != -1;
    /// </summary>
    public class Database
    {
        // %userprofile%\AppData\Roaming\Pinger\pings.sqlite
        private static string GetDatabasePath()
        {
            string dbDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Pinger");
            Directory.CreateDirectory(dbDirectory);
            return Path.Combine(dbDirectory, "pings.sqlite");
        }

        private static void EnsureTable(SQLiteConnection connection)
        {
            String commandText = @"CREATE TABLE IF NOT EXISTS 
                Pings(id INTEGER PRIMARY KEY AUTOINCREMENT,
                    date INTEGER,
                    response INTEGER);";

            using (var command = new SQLiteCommand(connection))
            {
                command.CommandText = commandText;
                command.ExecuteNonQuery();
            }
        }

        public static void WritePingStats(int milliseconds)
        {
            WritePingStats(milliseconds, GetDatabasePath());
        }

        public static void WritePingStats(int milliseconds, string databasePath)
        {
            string connectionString = String.Format("URI=file:{0}", databasePath);

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                EnsureTable(connection);
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = "INSERT INTO Pings(date, response) VALUES(@date, @response)";
                    command.Parameters.AddWithValue("@date", DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
                    command.Parameters.AddWithValue("@response", milliseconds);
                    command.Prepare();
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
