using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;

namespace RadioLog.Common
{
    public class ImportExportHelper
    {
        private void PrepareXML(XmlDocument xDoc)
        {
            if (xDoc.DocumentElement == null)
            {
                XmlNode xn = xDoc.CreateElement("RadioLogConfig");
                xDoc.AppendChild(xn);
            }
        }

        public void ExportInfo(string strFileName)
        {
            try
            {
                XmlDocument xDoc = new XmlDocument();
                PrepareXML(xDoc);
                AppSettings.Instance.SaveGroupsToXml(xDoc);
                AppSettings.Instance.SaveSourcesToXml(xDoc);

                RadioInfoLookupHelper.Instance.SaveXml(xDoc);

                if (System.IO.File.Exists(strFileName))
                    System.IO.File.Delete(strFileName);
                using (Stream fs = System.IO.File.OpenWrite(strFileName))
                {
                    xDoc.Save(fs);
                    fs.Close();
                }
            }
            catch { }
        }
        public bool ImportInfo(string strFileName)
        {
            if (System.IO.File.Exists(strFileName))
            {
                XmlDocument xDoc = new XmlDocument();
                using (Stream fs = System.IO.File.OpenRead(strFileName))
                {
                    xDoc.Load(fs);
                    fs.Close();
                }
                PrepareXML(xDoc);

                AppSettings.Instance.ImportXML(xDoc);
                RadioInfoLookupHelper.Instance.LoadXml(xDoc);

                AppSettings.Instance.SaveSettingsFile();
                RadioInfoLookupHelper.Instance.SaveInfo();

                return true;
            }

            return false;
        }
    }
}
