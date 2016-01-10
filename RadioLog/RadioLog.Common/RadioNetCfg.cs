using System;
using System.Collections.Generic;
using System.Management;
using System.Runtime.InteropServices;

namespace RadioLog.Common
{
    /// <summary>
    /// This code is used with desktop radios that connect to a PC by exposing an RNDIS interface.
    /// </summary>
    public static class RadioNetCfg
    {
        [DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCache")]
        private static extern UInt32 DnsFlushResolverCache();

        private static string[] DriverAdapterNames = { "mototrbo", "motorola apx", "digital remote ndis device" };

        /// <summary>
        /// Updates the network adapter settings for MotoTRBO radios.
        /// </summary>
        public static void RunForMotoTRBO() { RunForNamedItem("mototrbo"); }

        /// <summary>
        /// Updates the network adapter settings for Motorola APX series radios.
        /// </summary>
        public static void RunForAPX() { RunForNamedItem("motorola apx"); }

        /// <summary>
        /// Updates the network adapter settings for Hytera DMR series radios.
        /// </summary>
        public static void RunForHYT() { RunForNamedItem("digital remote ndis device"); }

        /// <summary>
        /// This routine updates the network adapters that match the testName property so they do not pass normal network traffic thru them. Only traffic specifically bound to the adapter should be passed thru.
        /// </summary>
        /// <param name="testName">Portion of network adapter name to run against.</param>
        private static void RunForNamedItem(string testName)
        {
            if (string.IsNullOrEmpty(testName))
                return;
            RunForNamedItems(testName);
        }
        private static void RunForNamedItems(params string[] testnames)
        {
            if (testnames == null || testnames.Length <= 0)
                return;
            for (int i = 0; i < testnames.Length; i++)
                testnames[i] = testnames[i].ToLower();
            try
            {
                List<UInt32> _adapterIndexes = new List<uint>();

                ManagementClass managementClassNetworkAdapter = new ManagementClass("Win32_NetworkAdapter");
                ManagementObjectCollection managementClassNetAdapterCollection = managementClassNetworkAdapter.GetInstances();
                foreach (ManagementObject netManagementObject in managementClassNetAdapterCollection)
                {
                    for (int i = 0; i < testnames.Length; i++)
                    {
                        if (((string)netManagementObject["Caption"]).ToLower().Contains(testnames[i]))
                        {
                            _adapterIndexes.Add((UInt32)netManagementObject["Index"]);
                        }
                    }
                }

                UInt32 connectionMetric = 9999;
                ManagementClass managementClassNetAdapterConfig = new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection managementClassNetAdapterConfigCollection = managementClassNetAdapterConfig.GetInstances();
                foreach (ManagementObject netConfigManagementObject in managementClassNetAdapterConfigCollection)
                {
                    try
                    {
                        UInt32 index = (UInt32)netConfigManagementObject["Index"];
                        if (!_adapterIndexes.Contains(index))
                            continue;
                        string adapterName = (string)netConfigManagementObject["ServiceName"];
                        if (!(bool)netConfigManagementObject["IPEnabled"])
                        {
                            //Device is not currently enabled as an IP device, probably not connected or a driver issue.
                            continue;
                        }

                        ManagementBaseObject objNewIP = null;
                        ManagementBaseObject objSetIP = null;
                        objNewIP = netConfigManagementObject.GetMethodParameters("SetDynamicDNSRegistration");
                        objNewIP["FullDNSRegistrationEnabled"] = false;
                        objNewIP["DomainDNSRegistrationEnabled"] = false;
                        objSetIP = netConfigManagementObject.InvokeMethod("SetDynamicDNSRegistration", objNewIP, null);
                        UInt32 retVal1 = (UInt32)objSetIP.Properties["ReturnValue"].Value;
                        objNewIP = netConfigManagementObject.GetMethodParameters("SetIPConnectionMetric");
                        objNewIP["IPConnectionMetric"] = connectionMetric;
                        objSetIP = netConfigManagementObject.InvokeMethod("SetIPConnectionMetric", objNewIP, null);
                        UInt32 retVal2 = (UInt32)objSetIP.Properties["ReturnValue"].Value;

                        connectionMetric--;


                    }
                    catch (Exception ex)
                    {
                        ConsoleColor consoleColor = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("ERROR!");
                        Exception e = ex;
                        while (e != null)
                        {
                            Console.WriteLine("    " + e.Message);
                            e = e.InnerException;
                        }
                        Console.ForegroundColor = consoleColor;
                    }
                }
                DnsFlushResolverCache();
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            catch { }
        }

        public static void RunForAllSupportedRadios() { RunForNamedItems(DriverAdapterNames); }

        private static bool _inBackgroundRun = false;
        private static DateTime _lastRun = DateTime.MinValue;
        public static void RunForAllSupportedRadiosInBackground()
        {
            if (_inBackgroundRun || (DateTime.Now - _lastRun).TotalMinutes < 3)
                return;
            _inBackgroundRun = true;
            _lastRun = DateTime.Now;
            System.Threading.Tasks.Task tBackground = new System.Threading.Tasks.Task(() =>
            {
                try
                {
                    RunForAllSupportedRadios();
                }
                finally { _inBackgroundRun = false; }
            });
            tBackground.Start();
        }
    }
}
