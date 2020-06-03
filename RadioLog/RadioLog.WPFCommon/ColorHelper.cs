using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Reflection;

namespace RadioLog.WPFCommon
{
    public static class ColorHelper
    {
        private static Dictionary<string, Brush> _accentColorBrushes = null;
        private static void LoadAccentColorBrushes()
        {
            try
            {
                if (_accentColorBrushes != null)
                    return;
                _accentColorBrushes = new Dictionary<string, Brush>();
                foreach(var theme in ControlzEx.Theming.ThemeManager.Current.Themes)
                {
                    _accentColorBrushes[theme.Name] = theme.ShowcaseBrush;
                }
            }
            catch (Exception ex)
            {
                Common.DebugHelper.WriteExceptionToLog("ColorHelper.LoadAccentColorBrushes", ex, true);
            }
        }
        
        static ColorHelper()
        {
            LoadAccentColorBrushes();
        }

        public static string GetKnownBrushName(Brush brush)
        {
            if (brush == null)
                return null;
            foreach (KeyValuePair<string, Brush> kvp in _accentColorBrushes)
            {
                if (kvp.Value == brush)
                    return kvp.Key;
            }
            return string.Empty;
        }
        public static Brush GetBrushByKnownName(string strBrushName)
        {
            if (_accentColorBrushes.ContainsKey(strBrushName))
                return _accentColorBrushes[strBrushName];
            else
                return null;
        }
        public static string GetSaveBrushText(Brush brush)
        {
            if (brush == null)
                return string.Empty;
            string knownName = GetKnownBrushName(brush);
            if (!string.IsNullOrEmpty(knownName))
                return knownName;
            else
                return brush.ToString();
        }
        public static Brush GetBrushFromSaveText(string strColor)
        {
            if (string.IsNullOrEmpty(strColor))
                return null;
            return GetBrushByKnownName(strColor);
            
        }

        public static BrushSelectionHolder[] GetBrushSelectionItems()
        {
            List<BrushSelectionHolder> rslt = new List<BrushSelectionHolder>();
            foreach (KeyValuePair<string, Brush> kvp in _accentColorBrushes)
            {
                rslt.Add(new BrushSelectionHolder(kvp.Key, kvp.Value));
            }
            return rslt.ToArray();
        }
    }

    public class BrushSelectionHolder
    {
        public string BrushName { get; private set; }
        public string BrushKey { get; private set; }
        public Brush BrushValue { get; private set; }

        private string FixBrushName(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;
            string rslt = string.Empty;
            for (int i = 0; i < key.Length; i++)
            {
                if (i > 0 && char.IsUpper(key, i))
                    rslt += " ";
                rslt += key[i];
            }
            return rslt;
        }

        public BrushSelectionHolder(string key, Brush val)
        {
            this.BrushName = FixBrushName(key);
            this.BrushKey = key;
            this.BrushValue = val;
        }
    }
}
