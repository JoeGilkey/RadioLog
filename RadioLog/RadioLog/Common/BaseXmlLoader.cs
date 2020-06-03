using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;

namespace RadioLog.Common
{
    public abstract class BaseXmlLoader
    {
        public abstract string RootElementName { get; }

        protected string GetAttributeText(XmlAttribute attrib)
        {
            if (attrib == null)
                return string.Empty;
            else
                return attrib.Value;
        }
        protected Guid GetGuidAttribute(XmlAttribute attrib)
        {
            string strGuid = GetAttributeText(attrib);
            if (string.IsNullOrWhiteSpace(strGuid))
                return Guid.Empty;
            Guid g;
            if (Guid.TryParse(strGuid, out g))
                return g;
            else
                return Guid.Empty;
        }
        protected int GetIntAttribute(XmlAttribute attrib) { return GetNullableIntAttribute(attrib) ?? 0; }
        protected int? GetNullableIntAttribute(XmlAttribute attrib)
        {
            string strInt = GetAttributeText(attrib);
            if (string.IsNullOrWhiteSpace(strInt))
                return null;
            int i;
            if (int.TryParse(strInt, out i))
                return i;
            else
                return null;
        }
        protected bool GetBoolAttribute(XmlAttribute attrib)
        {
            string strBool = GetAttributeText(attrib);
            if (string.IsNullOrWhiteSpace(strBool))
                return false;
            bool b;
            if (bool.TryParse(strBool, out b))
                return b;
            else
                return false;
        }
        protected T GetEnumAttribute<T>(XmlAttribute attrib, T defaultValue) where T : struct,IConvertible
        {
            string strValue = GetAttributeText(attrib);
            if (string.IsNullOrWhiteSpace(strValue))
                return defaultValue;
            try
            {
                T rslt = defaultValue;
                if (Enum.TryParse<T>(strValue, true, out rslt))
                    return rslt;
                else
                    return defaultValue;
            }
            catch { return defaultValue; }
        }

        protected void SetTextAttribute(XmlNode xNode, string attribName, string attribValue)
        {
            if (xNode == null)
                return;
            XmlAttribute xCurAttrib = xNode.Attributes[attribName];
            if (xCurAttrib != null)
                xCurAttrib.Value = attribValue;
            else
            {
                XmlAttribute xNewAttrib = xNode.OwnerDocument.CreateAttribute(attribName);
                xNewAttrib.Value = attribValue;
                xNode.Attributes.Append(xNewAttrib);
            }
        }
        protected void SetNonEmptyGuidAttribute(XmlNode xNode, string attribName, Guid attribValue)
        {
            if (attribValue == Guid.Empty)
                return;
            SetGuidAttribute(xNode, attribName, attribValue);
        }
        protected void SetGuidAttribute(XmlNode xNode, string attribName, Guid? attribValue)
        {
            if (attribValue != null && attribValue.HasValue)
                SetTextAttribute(xNode, attribName, attribValue.Value.ToString());
            else
                SetTextAttribute(xNode, attribName, string.Empty);
        }
        protected void SetIntAttribute(XmlNode xNode, string attribName, int attribValue) { SetTextAttribute(xNode, attribName, attribValue.ToString()); }
        protected void SetBoolAttribute(XmlNode xNode, string attribName, bool attribValue) { SetTextAttribute(xNode, attribName, attribValue.ToString()); }
        protected void SetEnumAttribute<T>(XmlNode xNode, string attribName, T attribValue) where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum) return;
            SetTextAttribute(xNode, attribName, attribValue.ToString());
        }

        protected void PrepareXML(XmlDocument xDoc)
        {
            if (xDoc.DocumentElement == null)
            {
                XmlNode xn = xDoc.CreateElement(RootElementName);
                xDoc.AppendChild(xn);
            }
        }

        protected void LoadFromXMLFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return;
            bool bChangesMade = false;
            if (File.Exists(fileName))
            {
                XmlDocument xDoc = new XmlDocument();
                using (FileStream xFile = File.OpenRead(fileName))
                {
                    xDoc.Load(xFile);
                    xFile.Close();
                }
                PrepareXML(xDoc);
                bChangesMade = LoadXml(xDoc);
            }
            else
            {
                bChangesMade = true;
            }
            if (bChangesMade)
            {
                SaveToXMLFile(fileName);
            }
        }
        protected void SaveToXMLFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return;
            if (File.Exists(fileName))
                File.Delete(fileName);
            XmlDocument xDoc = new XmlDocument();
            PrepareXML(xDoc);
            SaveXml(xDoc);
            using (FileStream xFile = File.OpenWrite(fileName))
            {
                xDoc.Save(xFile);
                xFile.Close();
            }
        }

        public abstract bool LoadXml(XmlDocument xDoc);
        public abstract void SaveXml(XmlDocument xDoc);
    }
}
