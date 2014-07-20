using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Mime;
using System.Reflection;
using System.Security.Policy;
using ImageGrabber.Properties;
using MessageBox = System.Windows.MessageBox;

namespace ImageGrabber {
  /// <summary>
  ///   Loads an assembly into a new AppDomain and obtains all the namespaces in the loaded Assembly, which are returned as a List. The new AppDomain is then Unloaded.
  ///   This class creates a new instance of a <c>AssemblyLoader</c> class which does the actual ReflectionOnly loading of the Assembly into the new AppDomain.
  /// </summary>
  public static class SeperateAppDomainAssemblyLoader {
    #region Public Methods

    /// <summary>
    ///   Loads an assembly into a new AppDomain and obtains all the namespaces in the loaded Assembly, which are returned as a List.
    ///   The new AppDomain is then Unloaded The Assembly file location
    /// </summary>
    /// <param name="assemblyLocations"></param>
    /// <returns>A list of found namespaces</returns>
    public static Dictionary<string, Assembly> LoadAssemblies(List<FileInfo> assemblyLocations) {
      string pathToDll = Assembly.GetExecutingAssembly().CodeBase;
      AppDomainSetup domainSetup = new AppDomainSetup { PrivateBinPath = pathToDll };
     // AppDomainSetup setup = AppDomain.CurrentDomain.SetupInformation;

      var newDomain = AppDomain.CreateDomain(Settings.Default.Domain_Name, null, domainSetup);
      try {
        AssemblyLoader loader = (AssemblyLoader) (newDomain.CreateInstanceFromAndUnwrap(pathToDll, typeof(AssemblyLoader).FullName));
        return loader.LoadAssemblies(assemblyLocations);
      } catch (Exception ex) {
        MessageBox.Show(ex.Message);
        return null;
      } finally {
        AppDomain.Unload(newDomain);
      }
    }

    #endregion

    /// <summary>
    ///   Remotable AssemblyLoader, this class inherits from <c>MarshalByRefObject</c> to allow the CLR to marshall this object by reference across AppDomain boundaries
    /// ProxyClass
    /// </summary>
    private sealed class AssemblyLoader : MarshalByRefObject {
      #region Private/Internal Methods

      /// <summary>
      ///   ReflectionOnlyLoad of single Assembly based on the assemblyPath parameter.
      ///   Generate Proxy class stub.
      /// </summary>
      /// <param name="assemblyLocations">The path to the Assembly</param>
      [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      internal Dictionary<string, Assembly> LoadAssemblies(IEnumerable<FileInfo> assemblyLocations) {
        var namespaces = new Dictionary<string, Assembly>();
        AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
        try {
          foreach (FileInfo assemblyLocation in assemblyLocations)
            Assembly.ReflectionOnlyLoadFrom(assemblyLocation.FullName);

          foreach (Assembly reflectionOnlyAssembly in AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies()) {
            try {
              foreach (Type type in reflectionOnlyAssembly.GetTypes()) {
                try {
                  if (type.Namespace != null && !namespaces.ContainsKey(type.Namespace))
                    namespaces.Add(type.Namespace, reflectionOnlyAssembly);
                } catch {} // Already added in the list.
              }
            } catch (TypeLoadException ex) {
              MessageBox.Show(ex.Message);
            }
          }
          return namespaces;
        } catch (FileNotFoundException) {
          // Continue loading assemblies even if an assembly can not be loaded in the new AppDomain.
          return namespaces;
        }
      }

      #endregion
    }

    private static Assembly AssemblyResolve(object sender, ResolveEventArgs args) {
      var path = sender as string;
      return path != null ? Assembly.ReflectionOnlyLoadFrom(path) : null;
    }
  }
}