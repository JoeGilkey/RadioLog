using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Windows.Media.Animation;

namespace RadioLog
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    [SecurityPermission(SecurityAction.Demand, Unrestricted = true)]
    [PermissionSet(SecurityAction.Demand, Unrestricted = true)]
    public partial class App : Application
    {
        public static bool AppUpdateAvailable { get; private set; }
        public static bool AppUpdateInProgress { get; private set; }
        public static string AssemblyProductName { get; private set; }
        public static string AssemblyShortVersionInfo { get; private set; }
        public static string AssemblyLongVersionInfo { get; private set; }
        public static string AssemblyDisplayName { get; private set; }
        
        public static bool RunSecurityDemands()
        {
            FileIOPermission fPer = new FileIOPermission(PermissionState.None);
            fPer.AllLocalFiles = FileIOPermissionAccess.AllAccess;
            fPer.AllFiles = FileIOPermissionAccess.AllAccess;
            try
            {
                fPer.Demand();
            }
            catch (SecurityException s)
            {
                Common.DebugHelper.WriteLine("File IO Permission Error: {0}", s.Message);
                return false;
            }

            System.Security.Permissions.FileDialogPermission fdPer = new FileDialogPermission(FileDialogPermissionAccess.None);
            fdPer.Access = FileDialogPermissionAccess.OpenSave;
            try
            {
                fdPer.Demand();
            }
            catch (System.Security.SecurityException s)
            {
                Common.DebugHelper.WriteLine("File Dialog Persmission Error: {0}", s.Message);
                return false;
            }

            System.Security.Permissions.RegistryPermission rPer = new RegistryPermission(PermissionState.None);
            rPer.SetPathList(RegistryPermissionAccess.AllAccess, "HKEY_LOCAL_MACHINE");
            try
            {
                fPer.Demand();
            }
            catch (System.Security.SecurityException s)
            {
                Common.DebugHelper.WriteLine("Registry Access Permission Error: {0}", s.Message);
                return false;
            }

            return true;
        }

        public static void RunDefaultConnectionLimitConfig()
        {
            try
            {
                System.Net.ServicePointManager.DefaultConnectionLimit = 1000;
            }
            catch { }
        }

        public static void SaveSettings()
        {
            try
            {
                RadioLog.Common.AppSettings.Instance.SaveSettingsFile();
            }
            catch (Exception ex)
            {
                Common.DebugHelper.WriteExceptionToLog("App.SaveSettings", ex, true, "System Settings");
            }
            try
            {
                RadioLog.Common.RadioInfoLookupHelper.Instance.SaveInfo();
            }
            catch (Exception ex)
            {
                Common.DebugHelper.WriteExceptionToLog("App.SaveSettings", ex, true, "Radio Lookup Info");
            }
        }

        public static bool IsAlreadyRunning()
        {
            Process current = Process.GetCurrentProcess();
            foreach (Process process in Process.GetProcessesByName(current.ProcessName))
            {
                if (process.Id != current.Id)
                {
                    return true;
                }
            }

            return false;
        }

        private static void LoadAssemblyDisplayInfo()
        {
            Assembly ass = typeof(App).Assembly;
            //if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed)
            //{
            //    Version netVer = System.Deployment.Application.ApplicationDeployment.CurrentDeployment.CurrentVersion;
            //    AssemblyShortVersionInfo = string.Format("{0}.{1}", netVer.Major, netVer.Minor);
            //    AssemblyLongVersionInfo = string.Format("{0}.{1}.{2}.{3}", netVer.Major, netVer.Minor, netVer.Build, netVer.Revision);
            //}
            //else
            //{
            //    AssemblyName assName = ass.GetName();
            //    AssemblyShortVersionInfo = string.Format("{0}.{1}", assName.Version.Major, assName.Version.Minor);
            //    AssemblyLongVersionInfo = string.Format("{0}.{1}.{2}.{3}", assName.Version.Major, assName.Version.Minor, assName.Version.Build, assName.Version.Revision);
            //}

            AssemblyName assName = ass.GetName();
            AssemblyShortVersionInfo = string.Format("{0}.{1}", assName.Version.Major, assName.Version.Minor);
            AssemblyLongVersionInfo = string.Format("{0}.{1}.{2}.{3}", assName.Version.Major, assName.Version.Minor, assName.Version.Build, assName.Version.Revision);

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

        public static void PerformAppRestart()
        {
            Common.DebugHelper.WriteLine("PERFORMING RADIOLOG RESTART");
            try
            {
                Common.AppSettings.Instance.SaveSettingsFile();
            }
            catch { }

            try
            {
                Common.RadioInfoLookupHelper.Instance.SaveInfo();
            }
            catch { }

            try
            {
                var dispatcher = Application.Current.Dispatcher;
                System.Windows.Forms.Application.Restart();
                if (dispatcher != null)
                {
                    dispatcher.BeginInvokeShutdown(System.Windows.Threading.DispatcherPriority.Normal);
                }
            }
            catch (Exception ex)
            {
                Common.DebugHelper.WriteExceptionToLog("PerformAppRestart", ex, false);
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            this.DispatcherUnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);
            bool bConsoleDisplayed = false;
            if (e.Args.Contains("console"))
            {
                bConsoleDisplayed = true;
                Common.ConsoleManager.Show();
            }

            if (!RunSecurityDemands())
            {
                Application.Current.Shutdown();
                return;
            }

            LoadAssemblyDisplayInfo();

            RadioLog.Common.AppSettings.Instance.Startup();

            Common.DebugHelper.SetShouldOutputNonErrorsToEventLog(Common.AppSettings.Instance.DiagnosticMode);
            Common.DebugHelper.WriteLine("STARTING RADIOLOG");
            
            RadioLog.Common.RadioInfoLookupHelper.Instance.ClearChangesMade();

            if (!bConsoleDisplayed && RadioLog.Common.AppSettings.Instance.ShowDebugConsole)
            {
                Common.ConsoleManager.Show();
                bConsoleDisplayed = true;
            }

            Common.DebugHelper.SetShouldDoConsoleOutput(bConsoleDisplayed);

            RunDefaultConnectionLimitConfig();

            if (string.IsNullOrWhiteSpace(RadioLog.Common.AppSettings.Instance.CurrentTheme) || !ControlzEx.Theming.ThemeManager.Current.Themes.Any(t => t.Name == RadioLog.Common.AppSettings.Instance.CurrentTheme))
            {

                RadioLog.Common.AppSettings.Instance.CurrentTheme = ControlzEx.Theming.ThemeManager.Current.DetectTheme()?.Name;
            }
            else
            {
                ControlzEx.Theming.ThemeManager.Current.ChangeTheme(App.Current, RadioLog.Common.AppSettings.Instance.CurrentTheme);
            }

            if (!RadioLog.Common.AppSettings.Instance.InitialLayoutDone)
            {
                RadioLog.Common.AppSettings.Instance.InitialLayoutDone = true;
                RadioLog.Common.AppSettings.Instance.EnableClipboardStreamURLIntegration = false;

                if (RadioLog.Common.ScreenHelper.IsSmallScreenSize)
                {
                    RadioLog.Common.AppSettings.Instance.ViewSize = Common.ProcessorViewSize.Small;
                    RadioLog.Common.AppSettings.Instance.GridFontSize = 16;
                }
                else
                {
                    RadioLog.Common.AppSettings.Instance.ViewSize = Common.ProcessorViewSize.Normal;
                    RadioLog.Common.AppSettings.Instance.GridFontSize = 20;
                }
                RadioLog.Common.AppSettings.Instance.SaveSettingsFile();
            }

            RadioLog.WPFCommon.ClipboardHelper.Instance.Start();

            RadioLog.AudioProcessing.AudioProcessingGlobals.OutputDiagnostics();

            ShutdownMode = System.Windows.ShutdownMode.OnMainWindowClose;

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            SaveSettings();

            RadioLog.WPFCommon.ClipboardHelper.Instance.Stop();

            Common.DebugHelper.WriteLine("STOPPING RADIOLOG");

            base.OnExit(e);
        }

        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            if (e.Exception.GetType() == typeof(System.ArgumentException))
                return;
            try
            {
                Common.DumpLogHelper.WriteExceptionToDumpLog(e.Exception);
                RadioLog.Common.DebugHelper.WriteExceptionToLog("Global Error", e.Exception, false, null, true);
            }
            finally
            {
                //Application.Current.Shutdown();
            }
        }
    }
}
