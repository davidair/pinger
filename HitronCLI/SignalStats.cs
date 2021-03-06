﻿/*
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
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Implementation of various signal stats from http://192.168.0.1/webpages/index.html#status_docsis_wan/m/1/s/2
/// </summary>
namespace HitronCLI
{
    public abstract class HitronStat
    {
        public static Dictionary<string, Type> StatMapping = DefineStatMapping();
        private static Dictionary<Type, string> ReverseStatMapping = GetReverseStatMapping();

        private static Dictionary<string, Type> DefineStatMapping()
        {
            Dictionary<string, Type> mapping = new Dictionary<string, Type>();

            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.IsSubclassOf(typeof(HitronStat)))
                {
                    string endpoint = (string)type.GetField("Endpoint").GetValue(null);
                    mapping.Add(endpoint, type);
                }
            }

            return mapping;
        }

        private static Dictionary<Type, string> GetReverseStatMapping()
        {
            Dictionary<Type, string> mapping = new Dictionary<Type, string>();

            foreach (var entry in HitronStat.StatMapping)
            {
                mapping.Add(entry.Value, entry.Key);
            }

            return mapping;
        }

        private string _originalSerialization;

        public String GetEndpoint()
        {
            return HitronStat.ReverseStatMapping[this.GetType()];
        }

        public static HitronStat FromJObject(string endpoint, JObject entry)
        {
            Type type = HitronStat.StatMapping[endpoint];
            HitronStat stat = (HitronStat)type.GetMethod("FromJObject").Invoke(null, BindingFlags.Static, null, new object[] { entry }, null);
            stat._originalSerialization = entry.ToString();
            return stat;
        }

        public static void PrintList(List<HitronStat> list)
        {
            bool firstRow = true;
            foreach (var entry in list)
            {
                if (firstRow)
                {
                    entry.PrintHeader();
                    firstRow = false;
                }
                entry.PrintStat();
            }
        }

        public abstract void PrintStat();
        public abstract void PrintHeader();

        public static T SafeParse<T>(JObject entry, string key) where T : IConvertible
        {
            if (!entry.ContainsKey(key))
            {
                return default(T);
            }

            string value = entry[key].Value<String>();

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default(T);
            }
        }

        public static bool GetBoolValue(JObject entry, string key)
        {
            return entry[key].Value<String>().Equals("YES", StringComparison.InvariantCultureIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            HitronStat other = obj as HitronStat;
            if (other == null)
            {
                return false;
            }

            return this._originalSerialization == other._originalSerialization;
        }

        public override int GetHashCode()
        {
            return this._originalSerialization.GetHashCode();
        }
    }

    /// <summary>
    /// http://192.168.0.1/1/Device/CM/DsInfo
    /// </summary>
    public class DownstreamStats : HitronStat
    {
        public static string Endpoint = "DsInfo";

        /// <summary>
        /// Port ID
        /// </summary>
        public int PortId { get; set; }
        /// <summary>
        /// Frequency (MHz)
        /// </summary>
        public long Frequency { get; set; }
        /// <summary>
        /// Modulation
        /// </summary>
        public string Modulation { get; set; }
        /// <summary>
        /// Signal strength (dBmV)
        /// </summary>
        public double SignalStrength { get; set; }
        /// <summary>
        /// Signal noise ratio (dB)
        /// </summary>
        public double SignalNoiseRatio { get; set; }
        /// <summary>
        /// Channel ID
        /// </summary>
        public int ChannelId { get; set; }

        // The following fields are not displayed in the UI

        public long DownstreamOctets { get; set; }
        public int Correct { get; set; }
        public int Uncorrect { get; set; }

        public override void PrintHeader()
        {
            Console.WriteLine("Port ID,Frequency (MHz),Modulation,Signal strength (dBmV),Signal noise ratio (dB),Channel ID,Downstream Octets,Correct,Uncorrect");
        }
        
        public override void PrintStat()
        {
            Console.WriteLine($"{PortId},{Frequency},{Modulation},{SignalStrength},{SignalNoiseRatio},{ChannelId},{DownstreamOctets},{Correct},{Uncorrect}");
        }

        public static DownstreamStats FromJObject(JObject entry)
        {
            return new DownstreamStats()
            {
                PortId = SafeParse<int>(entry, "portId"),
                Frequency = SafeParse<long>(entry, "frequency"),
                Modulation = entry["modulation"].Value<string>(),
                SignalStrength = SafeParse<double>(entry, "signalStrength"),
                SignalNoiseRatio = SafeParse<double>(entry, "snr"),
                ChannelId = SafeParse<int>(entry, "channelId"),
                DownstreamOctets = SafeParse<long>(entry, "dsoctets"),
                Correct = SafeParse<int>(entry, "correcteds"),
                Uncorrect = SafeParse<int>(entry, "uncorrect"),
            };
        }
    }

    /// <summary>
    /// http://192.168.0.1/1/Device/CM/UsInfo
    /// </summary>
    public class UpstreamStats : HitronStat
    {
        public static string Endpoint = "UsInfo";

        /// <summary>
        /// Port ID
        /// </summary>
        public int PortId { get; set; }
        /// <summary>
        /// Frequency (MHz)
        /// </summary>
        public long Frequency { get; set; }
        /// <summary>
        /// Modulation
        /// </summary>
        public string ModulationType { get; set; }
        /// <summary>
        /// Signal strength (dBmV)
        /// </summary>
        public double SignalStrength { get; set; }
        /// <summary>
        /// Channel ID
        /// </summary>
        public int ChannelId { get; set; }
        /// <summary>
        /// Bandwidth
        /// </summary>
        public long Bandwidth { get; set; }

        public override void PrintHeader()
        {
            Console.WriteLine("Port ID,Frequency (MHz),Modulation,Signal strength (dBmV),Channel ID,Bandwidth");
        }

        public override void PrintStat()
        {
            Console.WriteLine($"{PortId},{Frequency},{ModulationType},{SignalStrength},{ChannelId},{Bandwidth}");
        }

        public static UpstreamStats FromJObject(JObject entry)
        {
            return new UpstreamStats()
            {
                PortId = SafeParse<int>(entry, "portId"),
                Frequency = SafeParse<long>(entry, "frequency"),
                ModulationType = entry["modulationType"].Value<string>(),
                SignalStrength = SafeParse<double>(entry, "signalStrength"),
                ChannelId = SafeParse<int>(entry, "channelId"),
                Bandwidth = SafeParse<long>(entry, "bandwidth"),
            };
        }
    }

    /// <summary>
    /// http://192.168.0.1/1/Device/CM/DsOfdm
    /// </summary>
    public class OFDMDownstreamStats : HitronStat
    {
        public static string Endpoint = "DsOfdm";

        /// <summary>
        /// Receiver
        /// </summary>
        public int Receive { get; set; }
        /// <summary>
        /// FFT type
        /// </summary>
        public string FftType { get; set; }
        /// <summary>
        /// Subcarr 0 Frequency (MHz)
        /// </summary>
        public long SubcarrierFrequency { get; set; }
        /// <summary>
        /// PLC locked
        /// </summary>
        public bool PlcLocked { get; set; }
        /// <summary>
        /// NCP locked
        /// </summary>
        public bool NcpLocked { get; set; }
        /// <summary>
        /// MDC1 locked
        /// </summary>
        public bool MdcLocked { get; set; }
        /// <summary>
        /// PLC power(dBmv)
        /// </summary>
        public double PlcPower { get; set; }

        public override void PrintHeader()
        {
            Console.WriteLine("Receiver,FFT type,Subcarr 0 Frequency (MHz),PLC locked,NCP locked,MDC1 locked,PLC power(dBmv)");
        }

        public override void PrintStat()
        {
            Console.WriteLine($"{Receive},{FftType},{SubcarrierFrequency},{PlcLocked},{NcpLocked},{MdcLocked},{PlcPower}");
        }

        public static OFDMDownstreamStats FromJObject(JObject entry)
        {
            return new OFDMDownstreamStats()
            {
                Receive = SafeParse<int>(entry, "receive"),
                FftType = entry["ffttype"].Value<string>(),
                SubcarrierFrequency = SafeParse<long>(entry, "Subcarr0freqFreq"),
                PlcLocked = GetBoolValue(entry, "plclock"),
                NcpLocked = GetBoolValue(entry, "ncplock"),
                MdcLocked = GetBoolValue(entry, "mdc1lock"),
                PlcPower = SafeParse<double>(entry, "plcpower")
            };
        }
    }

    /// <summary>
    /// http://192.168.0.1/1/Device/CM/UsOfdm
    /// </summary>
    public class OFDMUpstreamStats : HitronStat
    {
        public static string Endpoint = "UsOfdm";

        /// <summary>
        /// Channel Index
        /// </summary>
        public int ChannelIndex { get; set; }
        /// <summary>
        /// State
        /// </summary>
        public string State { get; set; }
        /// <summary>
        /// lin Digital Att
        /// </summary>
        public double LinDigitalAtt { get; set; }
        /// <summary>
        /// Digital Att
        /// </summary>
        public double DigitalAtt { get; set; }
        /// <summary>
        /// BW (sc's*fft)
        /// </summary>
        public double ChannelBw { get; set; }
        /// <summary>
        /// Report Power
        /// </summary>
        public double ReportPower { get; set; }
        /// <summary>
        /// Report Power1_6
        /// </summary>
        public double ReportPower1_6 { get; set; }
        /// <summary>
        /// FFT Size
        /// </summary>
        public string FftSize { get; set; }

        public override void PrintHeader()
        {
            Console.WriteLine("Channel Index,State,lin Digital Att,Digital Att,BW (sc's*fft),Report Power,Report Power1_6,FFT Size");
        }

        public override void PrintStat()
        {
            Console.WriteLine($"{ChannelIndex},{State},{LinDigitalAtt},{DigitalAtt},{ChannelBw},{ReportPower},{ReportPower1_6},{FftSize}");
        }

        public static OFDMUpstreamStats FromJObject(JObject entry)
        {
            return new OFDMUpstreamStats()
            {
                ChannelIndex = SafeParse<int>(entry, "uschindex"),
                State = entry["state"].Value<string>().Trim(),
                LinDigitalAtt = SafeParse<double>(entry, "digAtten"),
                DigitalAtt = SafeParse<double>(entry, "digAttenBo"),
                ChannelBw = SafeParse<double>(entry, "channelBw"),
                ReportPower = SafeParse<double>(entry, "repPower"),
                ReportPower1_6 = SafeParse<double>(entry, "repPower1_6"),
                FftSize = entry["fftVal"].Value<string>().Trim(),
            };
        }
    }
}
