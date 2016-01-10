using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Reflection;

namespace Cinch
{
    public static class ViewResolver
    {
        private static readonly Dictionary<string, Type> _registeredViews = new Dictionary<string, Type>();


        public static void ResolveViewLookups(IEnumerable<Assembly> assembliesToExamine)
        {
            try
            {
                foreach (Assembly ass in assembliesToExamine)
                {
                    Console.WriteLine(string.Format("Resolving views for assembly {0}", ass.FullName));
                    try
                    {
                        foreach (Type type in ass.GetTypes())
                        {
                            foreach (var attrib in type.GetCustomAttributes(typeof(ViewnameToViewLookupKeyMetadataAttribute), true))
                            {
                                ViewnameToViewLookupKeyMetadataAttribute viewMetadataAtt = (ViewnameToViewLookupKeyMetadataAttribute)attrib;
                                lock (_registeredViews)
                                {
                                    _registeredViews.Add(viewMetadataAtt.ViewName, viewMetadataAtt.ViewLookupKey);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(string.Format("Unable to get types from assembly {0}", ass.FullName));
                        Console.WriteLine(string.Format("  Exception: {0}", ex.Message));
                        StringBuilder sbExceptions = new StringBuilder(string.Format("ViewResolver is unable to ResolveViewLookups, Assembly.GetTypes(), {0}", ass.FullName));
                        ReflectionTypeLoadException rtle = ex as ReflectionTypeLoadException;
                        if (rtle != null && rtle.LoaderExceptions != null)
                        {
                            foreach (Exception e in rtle.LoaderExceptions)
                            {
                                Console.WriteLine(string.Format("  Loader Exception: {0}", e.Message));
                                sbExceptions.AppendLine(e.Message);
                            }
                        }
                        throw new InvalidOperationException(sbExceptions.ToString(), ex);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("ViewResolver is unable to ResolveViewLookups based on current parameters", ex);
            }
        }



        public static DependencyObject CreateView(string viewName)
        {
            if (string.IsNullOrEmpty(viewName))
                throw new ArgumentNullException("viewName");

            Type viewLookupKey;
            lock (_registeredViews)
            {

                if (!_registeredViews.ContainsKey(viewName))
                    throw new InvalidOperationException(
                        String.Format("ViewResolver could not CreateView using Key{0}", viewName));

                if (!_registeredViews.TryGetValue(viewName, out viewLookupKey))
                    return null;
            }

            return (DependencyObject)Activator.CreateInstance(viewLookupKey);
        }


        public static void Register(Dictionary<string, Type> startupData)
        {
            foreach (var entry in startupData)
                Register(entry.Key, entry.Value);
        }


        public static void Register(string viewName, Type viewLookupKey)
        {
            if (string.IsNullOrEmpty(viewName))
                throw new ArgumentNullException("viewName");

            if (viewLookupKey == null)
                throw new ArgumentNullException("viewLookupKey");

            if (!typeof(UserControl).IsAssignableFrom(viewLookupKey))
                throw new ArgumentException("viewLookupKey must be of UserControl");

            lock (_registeredViews)
            {
                _registeredViews.Add(viewName, viewLookupKey);
            }
        }


        public static bool Unregister(string viewName)
        {
            if (string.IsNullOrEmpty(viewName))
                throw new ArgumentNullException("viewName");

            lock (_registeredViews)
            {
                return _registeredViews.Remove(viewName);
            }
        }
    }
}
