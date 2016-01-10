using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;

namespace RadioLog.Common
{
    public class NetworkUtils
    {
        public static readonly Guid MOTOTRBO_RADIO_TYPE_ID = Guid.Parse("2C49C4C1-419C-460E-9170-7E3B39D9F2E0");
        public static readonly Guid HYTERA_RADIO_TYPE_ID = Guid.Parse("69223964-AEC0-491B-8E8A-CB00BF28F044");
        public static readonly Guid CIMARRON_C25_RADIO_TYPE_ID = Guid.Parse("86D70F38-4EDE-4406-B18D-D8DCBDA60481");

        public static NetworkInterface[] GetAvailableInterfaces()
        {
            NetworkInterface[] intfList = NetworkInterface.GetAllNetworkInterfaces();
            List<NetworkInterface> rslt = new List<NetworkInterface>();
            foreach (NetworkInterface ni in intfList)
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                    continue;
                IPInterfaceProperties ipProps = ni.GetIPProperties();
                if (ipProps == null)
                    continue;
                if (ipProps.UnicastAddresses.Count == 0)
                    continue;
                rslt.Add(ni);
            }
            return rslt.ToArray();
        }
        public static IPAddress GetGatewayAddressFromNetInterface(NetworkInterface intf)
        {
            IPInterfaceProperties ipProps = intf.GetIPProperties();
            if (ipProps == null)
                return IPAddress.Any;
            if (ipProps.GatewayAddresses == null || ipProps.GatewayAddresses.Count == 0)
                return IPAddress.Any;
            return ipProps.GatewayAddresses[0].Address;
        }
        private static DetectedRadio[] GetPossibleAddresses(string descriptionTestVal, Guid radioTypeId, NetworkInterface[] intfList)
        {
            if (string.IsNullOrEmpty(descriptionTestVal) || intfList == null || intfList.Length <= 0)
                return null;
            string testVal = descriptionTestVal.ToLower();
            List<DetectedRadio> rslt = new List<DetectedRadio>();
            foreach (NetworkInterface intf in intfList)
            {
                if (string.IsNullOrEmpty(intf.Description))
                    continue;
                if (intf.Description.ToLower().Contains(testVal))
                {
                    IPAddress addr = GetGatewayAddressFromNetInterface(intf);
                    if (addr != IPAddress.Any)
                        rslt.Add(new DetectedRadio() { RadioIp = addr, RadioTypeId = radioTypeId });
                }
            }
            return rslt.ToArray();
        }
        public static DetectedRadio[] GetPossibleAddresses(string descriptionTestVal, Guid radioTypeId) { return GetPossibleAddresses(descriptionTestVal, radioTypeId, GetAvailableInterfaces()); }
        public static DetectedRadio[] GetPossibleRadioAddresses()
        {
            NetworkInterface[] intfList = GetAvailableInterfaces();
            if (intfList == null)
                return null;
            List<DetectedRadio> rslt = new List<DetectedRadio>();

            DetectedRadio[] tmp = GetPossibleAddresses("mototrbo", MOTOTRBO_RADIO_TYPE_ID, intfList);
            if (tmp != null)
                rslt.AddRange(tmp);
            tmp = GetPossibleAddresses("digital remote ndis device", HYTERA_RADIO_TYPE_ID, intfList);
            if (tmp != null)
                rslt.AddRange(tmp);
            return rslt.ToArray();
        }
        public static DetectedRadio[] GetPossibleMOTOTRBOAddresses() { return GetPossibleAddresses("mototrbo", MOTOTRBO_RADIO_TYPE_ID); }
        public static DetectedRadio[] GetPossibleHyteraAddresses() { return GetPossibleAddresses("digital remote ndis device", HYTERA_RADIO_TYPE_ID); }
        public static IPAddress GetLocalAddressFromRadioAddress(IPAddress radioAddress, int iIgnoreBytes)
        {
            NetworkInterface[] intfList = GetAvailableInterfaces();
            byte[] ipBytes = radioAddress.GetAddressBytes();
            if (ipBytes == null)
                return null;
            if (intfList == null || intfList.Length <= 0)
                return null;
            foreach (NetworkInterface ni in intfList)
            {
                IPInterfaceProperties ipProps = ni.GetIPProperties();
                foreach (UnicastIPAddressInformation uip in ipProps.UnicastAddresses)
                {
                    byte[] uipByes = uip.Address.GetAddressBytes();
                    if (uipByes == null)
                        continue;
                    if (uipByes.Length == ipBytes.Length)
                    {
                        bool b = true;
                        for (int i = 0; i < uipByes.Length - iIgnoreBytes; i++)
                        {
                            b &= (uipByes[i] == ipBytes[i]);
                        }
                        if (b)
                        {
                            return uip.Address;
                        }
                    }
                }
            }
            return null;
        }
        public static IPAddress GetLocalAddressFromRadioAddress(IPAddress radioAddress, out int iIgnoreBytesUsed)
        {
            iIgnoreBytesUsed = 0;
            IPAddress rslt = GetLocalAddressFromRadioAddress(radioAddress, 1);
            if (rslt == null)
            {
                iIgnoreBytesUsed = 2;
                rslt = GetLocalAddressFromRadioAddress(radioAddress, 2);
            }
            else
            {
                iIgnoreBytesUsed = 1;
            }
            return rslt;
        }
        public static IPAddress GetLocalAddressFromRadioAddress(string radioAddress)
        {
            IPAddress _addr = null;
            if (IPAddress.TryParse(radioAddress, out _addr))
            {
                return GetLocalAddressFromRadioAddress(_addr);
            }
            else
            {
                return null;
            }
        }
        public static IPAddress GetLocalAddressFromRadioAddress(IPAddress radioAddress)
        {
            int iIgnoreBytesUsed = 0;
            IPAddress rslt = GetLocalAddressFromRadioAddress(radioAddress, out iIgnoreBytesUsed);
            if (iIgnoreBytesUsed < 2)
                return rslt;
            else
                return null;
        }
        public static IPAddress GetDMRRadioIPAddress(byte radioSubnet, uint radioId)
        {
            uint iVal1 = (radioId / (256 * 256));
            radioId -= (iVal1 * 256 * 256);
            uint iVal2 = (radioId / 256);
            radioId -= (iVal2 * 256);

            string connIp = string.Format("{0}.{1}.{2}.{3}", radioSubnet, iVal1, iVal2, radioId);
            return IPAddress.Parse(connIp);
        }
    }

    public class DetectedRadio
    {
        public Guid RadioTypeId { get; set; }
        public IPAddress RadioIp { get; set; }
    }
}
