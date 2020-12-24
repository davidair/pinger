using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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

        public String GetEndpoint()
        {
            return HitronStat.ReverseStatMapping[this.GetType()];
        }

        public static HitronStat FromJObject(string endpoint, JObject entry)
        {
            Type type = HitronStat.StatMapping[endpoint];
            return (HitronStat)type.GetMethod("FromJObject").Invoke(null, BindingFlags.Static, null, new object[] { entry }, null);
        }
    }

    class Helper
    {
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
        public double SignalNoiseRation { get; set; }
        /// <summary>
        /// Channel ID
        /// </summary>
        public int ChannelId { get; set; }

        // The following fields are not displayed in the UI

        public long DownstreamOctets { get; set; }
        public int Correct { get; set; }
        public int Uncorrect { get; set; }

        public static DownstreamStats FromJObject(JObject entry)
        {
            return new DownstreamStats()
            {
                PortId = entry["portId"].Value<int>(),
                Frequency = entry["frequency"].Value<long>(),
                Modulation = entry["modulation"].Value<string>(),
                SignalStrength = entry["signalStrength"].Value<double>(),
                SignalNoiseRation = entry["snr"].Value<double>(),
                ChannelId = entry["channelId"].Value<int>(),
                DownstreamOctets = entry["dsoctets"].Value<long>(),
                Correct = entry["correcteds"].Value<int>(),
                Uncorrect = entry["uncorrect"].Value<int>(),
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

        public static UpstreamStats FromJObject(JObject entry)
        {
            return new UpstreamStats()
            {
                PortId = entry["portId"].Value<int>(),
                Frequency = entry["frequency"].Value<long>(),
                ModulationType = entry["modulationType"].Value<string>(),
                SignalStrength = Helper.SafeParse<double>(entry, "signalStrength"),
                ChannelId = entry["channelId"].Value<int>(),
                Bandwidth = entry["bandwidth"].Value<long>(),
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
        /// Subcarr 0 Frequency(MHz)
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

        public static OFDMDownstreamStats FromJObject(JObject entry)
        {
            return new OFDMDownstreamStats()
            {
                Receive = entry["receive"].Value<int>(),
                FftType = entry["ffttype"].Value<string>(),
                SubcarrierFrequency = Helper.SafeParse<long>(entry, "Subcarr0freqFreq"),
                PlcLocked = Helper.GetBoolValue(entry, "plclock"),
                NcpLocked = Helper.GetBoolValue(entry, "ncplock"),
                MdcLocked = Helper.GetBoolValue(entry, "mdc1lock"),
                PlcPower = Helper.SafeParse<double>(entry, "plcpower")
            };
        }
    }

    /// <summary>
    /// http://192.168.0.1/1/Device/CM/UsOfdm
    /// </summary>
    public class OFDMUpstreamStats : HitronStat
    {
        public static string Endpoint = "UsOfdm";

        public int ChannelIndex { get; set; }
        public string State { get; set; }
        public double LinDigitalAtt { get; set; }
        public double DigitalAtt { get; set; }
        public double ChannelBw { get; set; }
        public double ReportPower { get; set; }
        public double ReportPower1_6 { get; set; }
        public string FftSize { get; set; }

        public static OFDMUpstreamStats FromJObject(JObject entry)
        {
            return new OFDMUpstreamStats()
            {
                ChannelIndex = entry["uschindex"].Value<int>(),
                State = entry["state"].Value<string>().Trim(),
                LinDigitalAtt = entry["digAtten"].Value<double>(),
                DigitalAtt = entry["digAttenBo"].Value<double>(),
                ChannelBw = entry["channelBw"].Value<double>(),
                ReportPower = entry["repPower"].Value<double>(),
                ReportPower1_6 = entry["repPower1_6"].Value<double>(),
                FftSize = entry["fftVal"].Value<string>().Trim(),
            };
        }
    }
}
