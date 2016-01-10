using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace RadioLog.Common
{
    public class DirectoryHelper
    {
        public static bool SetupDirectory(string strPath)
        {
            if (!Directory.Exists(strPath))
            {
                DirectoryInfo directoryInfo = Directory.CreateDirectory(strPath);
                DirectorySecurity directorySecurity = directoryInfo.GetAccessControl();
                SecurityIdentifier securityIdentifier = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
                AccessRule rule = new FileSystemAccessRule(securityIdentifier, FileSystemRights.Write | FileSystemRights.ReadAndExecute | FileSystemRights.Modify, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.InheritOnly, AccessControlType.Allow);
                bool modified;
                directorySecurity.ModifyAccessRule(AccessControlModification.Add, rule, out modified);
                directoryInfo.SetAccessControl(directorySecurity);
            }
            return Directory.Exists(strPath);
        }
    }
}
