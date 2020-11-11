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
using System.ComponentModel;
using System.Windows;

namespace Pinger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly BackgroundWorker _pingWorker = new BackgroundWorker();
        private int _failures;
        private int _successes;
        private int _totalPings;

        public MainWindow()
        {
            _pingWorker.DoWork += _pingWorker_DoWork;
            InitializeComponent();
            _pingWorker.RunWorkerAsync();
        }

        private void _pingWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                PingSender.SendPing("google.com", 1000, (milliseconds) => {
                    _totalPings++;
                    String message;
                    if (milliseconds >= 0)
                    {
                        _successes++;
                        message = String.Format("Pinged in {0} ms", milliseconds);
                        Database.WritePingStats(milliseconds);
                    }
                    else
                    {
                        _failures++;
                        message = "Ping timed out";
                        Database.WritePingStats(-1);
                    }
                    Application.Current.Dispatcher.Invoke(new Action(() => {
                        _text.AppendText(message + Environment.NewLine);
                        _averageDrop.Text = String.Format("Average percentage drop: {0:0.00}%", 100.0 * _failures / _totalPings);
                    }));
                });
            }
        }

        private void _text_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            _text.ScrollToEnd();
        }
    }
}
