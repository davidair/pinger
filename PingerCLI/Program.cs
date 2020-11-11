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
using PingerCore;
using System;

namespace PingerCLI
{
    class Program
    {
        static void Main(string[] args)
        {
            int failures = 0;
            int successes = 0;
            int totalPings = 0;

            while (true)
            {
                PingSender.SendPing("google.com", 1000, (milliseconds) => {
                    totalPings++;
                    String message;
                    if (milliseconds >= 0)
                    {
                        successes++;
                        message = String.Format("Pinged in {0} ms", milliseconds);
                        Database.WritePingStats(milliseconds);
                    }
                    else
                    {
                        failures++;
                        message = "Ping timed out";
                        Database.WritePingStats(-1);
                    }
                    Console.WriteLine(String.Format("{1}; average percentage drop: {0:0.00}%", 100.0 * failures / totalPings, message));
                });
            }
        }
    }
}
