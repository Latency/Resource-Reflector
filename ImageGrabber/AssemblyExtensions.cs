using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace ImageGrabber {
  /// <summary>
  ///   Extension methods for System.Reflection.Assembly.
  /// </summary>
  public static class AssemblyExtensions {
    public static string GetCompany(this Assembly assembly) {
      foreach (var cad in assembly.GetCustomAttributesData().Where(a => a.AttributeType == typeof (AssemblyTitleAttribute)))
        return cad.ConstructorArguments.FirstOrDefault().Value as String;
      return String.Empty;
    }

    public static string GetCopyright(this Assembly assembly) {
      foreach (var cad in assembly.GetCustomAttributesData().Where(a => a.AttributeType == typeof(AssemblyCopyrightAttribute)))
        return cad.ConstructorArguments.FirstOrDefault().Value as String;
      return String.Empty;
    }

    public static string GetDescription(this Assembly assembly) {
      foreach (var cad in assembly.GetCustomAttributesData().Where(a => a.AttributeType == typeof(AssemblyDescriptionAttribute)))
        return cad.ConstructorArguments.FirstOrDefault().Value as String;
      return String.Empty;
    }

    public static IEnumerable<string> GetNamespaces(this Assembly assembly) {
      Type[] types = assembly.GetTypes();
      var result = new List<string>();
      foreach (Type type in types.Where(type => !result.Contains(type.Namespace)))
        result.Add(type.Namespace);
      return result.OrderBy(ns => ns).ToList();
    }

    public static IEnumerable<Type> GetNamespaceTypes(this Assembly assembly, string @namespace) {
      return (from type in assembly.GetTypes() where type.Namespace == @namespace select type).ToList();
    }

    public static string GetProduct(this Assembly assembly) {
      foreach (var cad in assembly.GetCustomAttributesData().Where(a => a.AttributeType == typeof(AssemblyProductAttribute)))
        return cad.ConstructorArguments.FirstOrDefault().Value as String;
      return String.Empty;
    }

    public static string GetTitle(this Assembly assembly) {
      foreach (var cad in assembly.GetCustomAttributesData().Where(a => a.AttributeType == typeof(AssemblyTitleAttribute)))
        return cad.ConstructorArguments.FirstOrDefault().Value as String;
      return String.Empty;
    }

    public static Version GetVersion(this Assembly assembly) {
      return assembly.GetName().Version;
    }
  }

  /// <summary>
  ///  Assembly information.
  /// </summary>
  [Serializable]
  public struct AssemblyInfo : ISerializable {

    public AssemblyInfo(SerializationInfo info, StreamingContext context) : this() {
      // Reset the property value using the GetValue method.
      CodeBase = (string) info.GetValue("CodeBase", typeof(string));
      Company = (string) info.GetValue("Company", typeof(string));
      Copyright = (string) info.GetValue("Copyright", typeof(string));
      Description = (string) info.GetValue("Description", typeof(string));
      EscapedCodeBase = (string) info.GetValue("EscapedCodeBase", typeof(string));
      FullName = (string) info.GetValue("FullName", typeof(string));
      HashCode = (int) info.GetValue("HashCode", typeof(int));
      Location = (string) info.GetValue("Location", typeof(string));
      ManifestResourceNames = (string[]) info.GetValue("ManifestResourceNames", typeof(string[]));
      Name = (AssemblyName) info.GetValue("Name", typeof(AssemblyName));
      //Namespaces = (IEnumerable<string>) info.GetValue("Namespaces", typeof(IEnumerable<string>));
      Product = (string) info.GetValue("Product", typeof(string));
      Title = (string) info.GetValue("Title", typeof(string));
      Version = (Version) info.GetValue("Version", typeof(Version));
    }

    public void GetObjectData(SerializationInfo info, StreamingContext context) {
      // Use the AddValue method to specify serialized values.
      info.AddValue("CodeBase", CodeBase, typeof(string));
      info.AddValue("Company", Company, typeof(string));
      info.AddValue("Copyright", Copyright, typeof(string));
      info.AddValue("Description", Description, typeof(string));
      info.AddValue("EscapedCodeBase", EscapedCodeBase, typeof(string));
      info.AddValue("FullName", FullName, typeof(string));
      info.AddValue("HashCode", HashCode, typeof(int));
      info.AddValue("Location", Location, typeof(string));
      info.AddValue("ManifestResourceNames", ManifestResourceNames, typeof(string[]));
      info.AddValue("Name", Name, typeof(AssemblyName));
      //info.AddValue("Namespaces", Namespaces, typeof(IEnumerable<string>));
      info.AddValue("Product", Product, typeof(string));
      info.AddValue("Title", Title, typeof(string));
      info.AddValue("Version", Version, typeof(Version));
    }

    public AssemblyInfo(Assembly assembly) : this() {
      Initialize(assembly);
    }

    public void Initialize(Assembly assembly) {
      using (var mem = new MemoryStream()) {
        var formatter = new BinaryFormatter();
        formatter.Serialize(mem, assembly);
        AssemblyData = mem.ToArray();
      }

      CodeBase = assembly.CodeBase;
      Company = assembly.GetCompany();
      Copyright = assembly.GetCopyright();
      //CustomAttributes = assembly.GetCustomAttributes();
      //CustomAttributesData = assembly.GetCustomAttributesData();
      //DefinedTypes = assembly.DefinedTypes;
      Description = assembly.GetDescription();
      //EntryPoint = assembly.EntryPoint;
      EscapedCodeBase = assembly.EscapedCodeBase;
      //Evidence = assembly.Evidence;
      //ExportedTypes = assembly.ExportedTypes;
      FullName = assembly.FullName;
      //CustomAttributesData = assembly.GetCustomAttributesData();
      //Files = assembly.GetFiles();
      HashCode = assembly.GetHashCode();
      //LoadedModules = assembly.GetLoadedModules();
      Location = assembly.Location;
      ManifestResourceNames = assembly.GetManifestResourceNames();
      //Modules = assembly.GetModules();
      //SatelliteAssembly = assembly.GetSatelliteAssembly(CultureInfo.DefaultThreadCurrentCulture);
      Name = assembly.GetName();
      //Namespaces = assembly.GetNamespaces();
      Product = assembly.GetProduct();
      Title = assembly.GetTitle();
      Version = assembly.GetVersion();
    }




    public byte[] AssemblyData { get; private set; }

    public string CodeBase { get; private set; }

    public string Company { get; private set; }

    public string Copyright { get; private set; }

    //public IEnumerable<Attribute> CustomAttributes { get; private set; }

    //public IList<CustomAttributeData> CustomAttributesData { get; private set; }

    //public IEnumerable<TypeInfo> DefinedTypes { get; private set; }
 
    public string Description { get; private set; }

    //public MethodInfo EntryPoint { get; private set; }

    public string EscapedCodeBase { get; private set; }

    //public Evidence Evidence { get; private set; }

    //public IEnumerable<Type> ExportedTypes { get; private set; }

    //public FileStream[] Files { get; private set; }

    public string FullName { get; private set; }
    
    public int HashCode { get; private set; }

    //public Module[] LoadedModules { get; private set; }

    public string Location { get; private set; }

    public string[] ManifestResourceNames { get; private set; }

    //public Module[] Modules { get; private set; }

    public AssemblyName Name { get; private set; }
    
    //public IEnumerable<string> Namespaces { get; private set; }

    public string Product { get; private set; }

    //public Assembly SatelliteAssembly { get; private set; }

    public string Title { get; private set; }

    public Version Version { get; private set; }
  }
}