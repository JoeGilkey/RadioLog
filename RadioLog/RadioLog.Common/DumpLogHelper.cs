using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace RadioLog.Common
{
    public static class DumpLogHelper
    {
        private static string AssemblyLongVersionInfo = string.Empty;
        private static string AssemblyProductName = string.Empty;
        private static string AssemblyDisplayName = string.Empty;

        static DumpLogHelper()
        {
            Assembly ass = Assembly.GetEntryAssembly();
            if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed)
            {
                Version netVer = System.Deployment.Application.ApplicationDeployment.CurrentDeployment.CurrentVersion;
                AssemblyLongVersionInfo = string.Format("{0}.{1}.{2}.{3}", netVer.Major, netVer.Minor, netVer.Build, netVer.Revision);
            }
            else
            {
                AssemblyName assName = ass.GetName();
                AssemblyLongVersionInfo = string.Format("{0}.{1}.{2}.{3}", assName.Version.Major, assName.Version.Minor, assName.Version.Build, assName.Version.Revision);
            }

            object[] attribs = ass.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
            if (attribs != null && attribs.Length > 0)
            {
                AssemblyProductAttribute attribProd = attribs[0] as AssemblyProductAttribute;
                if (attribProd != null)
                {
                    AssemblyProductName = attribProd.Product;
                    //AssemblyDisplayName = string.Format("{0} ({1})", AssemblyProductName, AssemblyShortVersionInfo);
                    AssemblyDisplayName = string.Format("{0} ({1})", AssemblyProductName, AssemblyLongVersionInfo);
                }
            }
        }

        private static string GetDumpFileName() { return System.IO.Path.Combine(Common.AppSettings.Instance.AppDataDir, "RadioLogDump.txt"); }

        private static double BytesToGb(ulong val)
        {
            if (val <= 0)
                return 0;
            return (val / (1024 * 1024 * 1024));
        }

        private static string RectangleToInfo(string name, System.Drawing.Rectangle r)
        {
            return string.Format("{0}=[W:{1}, H:{2}, T:{3}, L:{4}]", name, r.Width, r.Height, r.Top, r.Left);
        }

        private static void AppendComputerInfo(StringBuilder sb)
        {
            try
            {
                if (sb == null)
                    return;
                Microsoft.VisualBasic.Devices.ComputerInfo info = new Microsoft.VisualBasic.Devices.ComputerInfo();
                sb.AppendLine("**** COMPUTER INFO ****");
                sb.AppendLine(string.Format("Physical Memory: {0:N} Avail, {1:N} Total", BytesToGb(info.AvailablePhysicalMemory), BytesToGb(info.TotalPhysicalMemory)));
                sb.AppendLine(string.Format("Virtual Memory: {0:N} Avail, {1:N} Total", BytesToGb(info.AvailableVirtualMemory), BytesToGb(info.TotalVirtualMemory)));
                sb.AppendLine(string.Format("OS Full Name: {0}", info.OSFullName));
                sb.AppendLine(string.Format("OS Platform: {0}", info.OSPlatform));
                sb.AppendLine(string.Format("OS Version: {0}", info.OSVersion));
                try
                {
                    System.Windows.Forms.Screen[] screens = System.Windows.Forms.Screen.AllScreens;
                    sb.AppendLine(string.Format("{0} SCREEN(s) DETECTED.", screens.Length));
                    foreach (System.Windows.Forms.Screen screen in screens)
                    {
                        sb.AppendLine(string.Format("  SCREEN [{0}], Primary={1}, BitsPerPixel={2}, {3}, {4}", screen.DeviceName, screen.Primary, screen.BitsPerPixel, RectangleToInfo("Bounds", screen.Bounds), RectangleToInfo("Working", screen.WorkingArea)));
                    }
                }
                catch(Exception exScreens)
                {
                    sb.AppendLine(string.Format("ERROR DETECTING SCREENS: {0}", DebugHelper.GetFullExceptionMessage(exScreens)));
                }
                try
                {
                    string[] comPorts = System.IO.Ports.SerialPort.GetPortNames();
                    if (comPorts != null)
                    {
                        sb.AppendLine(string.Format("{0} COM PORT(s) DETECTED.", comPorts.Length));
                        foreach (string com in comPorts)
                        {
                            sb.AppendLine(string.Format("  {0}", com));
                        }
                    }
                    else
                    {
                        sb.AppendLine("NO COM PORTS RETURNED!");
                    }
                }
                catch(Exception exCom)
                {
                    sb.AppendLine(string.Format("ERROR DETECTING COM PORTS: {0}", DebugHelper.GetFullExceptionMessage(exCom)));
                }
            }
            catch { }
        }

        public static string AppVersionInfo
        {
            get { return string.Format("{0} ver {1}", AssemblyProductName, AssemblyLongVersionInfo); }
        }
        public static void AppendDumpHeaderInfo(StringBuilder sbDumpHeader)
        {
            sbDumpHeader.AppendLine("**** VERSION INFO ****");
            sbDumpHeader.AppendLine(string.Format("Date={0}", DateTime.Now.ToLongDateString()));
            sbDumpHeader.AppendLine(string.Format("Time={0}", DateTime.Now.ToLongTimeString()));
            sbDumpHeader.AppendLine(string.Format("Product={0}", AssemblyProductName));
            sbDumpHeader.AppendLine(string.Format("Assembly={0}", AssemblyDisplayName));
            sbDumpHeader.AppendLine(string.Format("AssemblyVersion={0}", AssemblyLongVersionInfo));
            sbDumpHeader.AppendLine(string.Format("Environment={0}", Environment.OSVersion.ToString()));
            sbDumpHeader.AppendLine(string.Format("64BitOS={0}", Environment.Is64BitOperatingSystem));
            sbDumpHeader.AppendLine(string.Format("MachineName={0}", Environment.MachineName));
            sbDumpHeader.AppendLine(string.Format("UserDomainName={0}", Environment.UserDomainName));
            sbDumpHeader.AppendLine(string.Format("UserName={0}", Environment.UserName));
            sbDumpHeader.AppendLine(string.Format("SystemPageSize={0}", Environment.SystemPageSize));
            sbDumpHeader.AppendLine(string.Format("ProcessorCount={0}", Environment.ProcessorCount));
            AppendComputerInfo(sbDumpHeader);
        }

        public static void WriteExceptionToDumpLog(Exception ex, string codeSection = null, string detailInfo = null)
        {
            if (!AppSettings.Instance.EnableErrorDumps)
                return;
            try
            {
                string dumpFile = GetDumpFileName();
                string errorStr = DebugHelper.GetFullExceptionMessage(ex);
                StringBuilder sbDumpHeader = new StringBuilder();
                sbDumpHeader.AppendLine("**** START OF ERROR DUMP ****");
                AppendDumpHeaderInfo(sbDumpHeader);
                if (!string.IsNullOrWhiteSpace(codeSection))
                    sbDumpHeader.AppendLine(string.Format("CODE SECTION={0}", codeSection));
                if (!string.IsNullOrWhiteSpace(detailInfo))
                    sbDumpHeader.AppendLine(string.Format("CODE DETAILS={0}", detailInfo));
                sbDumpHeader.AppendLine("**** EXCEPTION INFO ****");
                sbDumpHeader.AppendLine(errorStr);
                sbDumpHeader.AppendLine("**** ENVIRONMENT STACK TRACE ****");
                sbDumpHeader.AppendLine(Environment.StackTrace);
                sbDumpHeader.AppendLine("**** END OF ERROR DUMP ****");
                System.IO.File.AppendAllText(dumpFile, sbDumpHeader.ToString());
            }
            catch { }
        }
    }
}
