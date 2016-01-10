using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.IO;
using System.ComponentModel.Composition.Primitives;
using System.Reflection;

namespace MEFedMVVM.ViewModelLocator
{
    /// <summary>
    /// Default composer for Design time. This will load all assemblies that have the DesignTimeCatalog attibute
    /// </summary>
    public class DefaultDesignTimeComposer : IComposer
    {
        private const string MefedmvvmWpf = "MEFedMVVM.WPF";
        private const string MefedmvvmWpfDll = "MEFedMVVM.WPF.dll";

        #region IComposer Members

        public ComposablePartCatalog InitializeContainer()
        {
            return GetCatalog();
        }

        #endregion

        private AggregateCatalog GetCatalog()
        {
            //CAS - Modified to work around Blend’s caching of different versions of an assembly at the same time.
            //This will ensure that only the newest assembly found is actually in the catalog.

            Dictionary<string, AssemblyCatalog> assemDict = new Dictionary<string, AssemblyCatalog>();

            
            IList<AssemblyCatalog> assembliesLoadedCatalogs =
                (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                 //only load assemblyies with this attribute
                 where assembly.GetReferencedAssemblies().Where(x => x.Name.Contains(MefedmvvmWpf)).Count() > 0 ||
                 assembly.ManifestModule.Name == MefedmvvmWpfDll
                 select new AssemblyCatalog(assembly)).ToList();

            if (assembliesLoadedCatalogs.Where(x => x.Assembly.ManifestModule.Name != MefedmvvmWpfDll).Count() == 0)
            {
                Debug.WriteLine("No assemblies found for Design time. Quick tip... ");
                return null;
            }

            var catalog = new AggregateCatalog();

            foreach (var item in assembliesLoadedCatalogs)
            {
                AssemblyCatalog ass;
                if (assemDict.TryGetValue(item.Assembly.FullName, out ass))
                {
                    DateTime oldAssDT = File.GetLastAccessTime(ass.Assembly.Location);
                    DateTime newAssDT = File.GetLastAccessTime(item.Assembly.Location);
                    if (newAssDT > oldAssDT)
                    {
                        assemDict[item.Assembly.FullName] = item;
                    }
                }
                else
                {
                    assemDict[item.Assembly.FullName] = item;
                }
            }

            foreach (var item in assemDict.Values)
                catalog.Catalogs.Add(item);
            return catalog;
        }
    }

    /// <summary>
    /// Implementation for a default runtime composer
    /// </summary>
    public class DefaultRuntimeComposer : IComposer
    {
        #region IComposer Members

        public ComposablePartCatalog InitializeContainer()
        {
            return GetCatalog();
        }

        #endregion

        private AggregateCatalog GetCatalog()
        {
            var catalog = new AggregateCatalog();
            //var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            
            Assembly assm = Assembly.GetEntryAssembly();
            string strDir = System.IO.Path.GetFullPath(System.IO.Path.GetDirectoryName(assm.Location));
            string filteredName = Path.GetFileNameWithoutExtension(assm.Location);
            if (!string.IsNullOrEmpty(filteredName))
            {
                catalog.Catalogs.Add(new AssemblyCatalog(assm));
                catalog.Catalogs.Add(new AssemblyCatalog(Assembly.GetExecutingAssembly()));
                catalog.Catalogs.Add(new DirectoryCatalog(strDir, string.Format("{0}.*Common.dll", filteredName)));
                catalog.Catalogs.Add(new DirectoryCatalog(strDir, string.Format("{0}.AddIns.*.dll", filteredName)));
                catalog.Catalogs.Add(new DirectoryCatalog(strDir, "Cinch.*.dll"));
            }
            /*
            var extensionPath = String.Format(@"{0}\Extensions\", baseDirectory);
            catalog.Catalogs.Add(new DirectoryCatalog(baseDirectory));
            catalog.Catalogs.Add(new DirectoryCatalog(baseDirectory, "*.exe"));
            if (Directory.Exists(extensionPath))
                catalog.Catalogs.Add(new DirectoryCatalog(extensionPath));
            */
            return catalog;

        }
    }

}
