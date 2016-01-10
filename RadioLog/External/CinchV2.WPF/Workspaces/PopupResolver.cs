using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Reflection;
using MEFedMVVM.ViewModelLocator;

namespace Cinch
{
    public static class PopupResolver
    {


        public static void ResolvePopupLookups(IEnumerable<Assembly> assembliesToExamine)
        {
            try
            {
                IUIVisualizerService uiVisualizerService  = 
                    ViewModelRepository.Instance.Resolver.Container.GetExport<IUIVisualizerService>().Value;

                foreach (Assembly ass in assembliesToExamine)
                {
                    Console.WriteLine(string.Format("Resolving popup lookups for assembly {0}", ass.FullName));
                    try
                    {
                        foreach (Type type in ass.GetTypes())
                        {
                            foreach (var attrib in type.GetCustomAttributes(typeof(PopupNameToViewLookupKeyMetadataAttribute), true))
                            {
                                PopupNameToViewLookupKeyMetadataAttribute viewMetadataAtt = (PopupNameToViewLookupKeyMetadataAttribute)attrib;
                                uiVisualizerService.Register(viewMetadataAtt.PopupName, viewMetadataAtt.ViewLookupKey);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(string.Format("Unable to get types from assembly {0}", ass.FullName));
                        Console.WriteLine(string.Format("  Exception: {0}", ex.Message));
                        StringBuilder sbExceptions = new StringBuilder(string.Format("PopupResolver is unable to ResolvePopupLookups, Assembly.GetTypes(), {0}", ass.FullName));
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
                throw new InvalidOperationException("PopupResolver is unable to ResolvePopupLookups based on current parameters", ex);
            }
        }
       
    }
}
