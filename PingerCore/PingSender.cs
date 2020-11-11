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
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;

namespace PingerCore
{
    public class PingSender
    {
        public static void SendPing(string site, int waitMillis, Action<int> onPing)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            long start = stopwatch.ElapsedMilliseconds;

            using (var process = new Process())
            {
                process.StartInfo.FileName = "ping.exe";
                process.StartInfo.Arguments = "-n 1 " + site;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.OutputDataReceived += (sender, e) =>
                {
                    string data = e.Data as string;
                    if (String.IsNullOrWhiteSpace(data))
                    {
                        return;
                    }

                    Match match = Regex.Match(data, @"Reply from .* time=(\d+)ms");
                    if (match.Success)
                    {
                        onPing(Int32.Parse(match.Groups[1].Value));
                    }
                    else if (data == "Request timed out.")
                    {
                        onPing(-1);
                    }
                };
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.BeginOutputReadLine();
                process.WaitForExit();
            }

            long elapsed = stopwatch.ElapsedMilliseconds - start;
            long waitTime = waitMillis - elapsed;
            if (waitTime > 0)
            {
                Thread.Sleep((int)waitTime);
            }
        }
    }
}
