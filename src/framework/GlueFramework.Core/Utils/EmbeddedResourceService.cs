using System.Reflection;

namespace GlueFramework.Core.Utils
{
    public class EmbeddedResourceService
    {
        //public const string SCRIPT_FILE = "ChildrenEducation.Module.DBScripts>InitDb.sql";

        public static string GetEmbeddedResourceContent<T>(string name,bool isPartialName = false)
        {
            var assembly = typeof(T).Assembly;
            var allNames = assembly.GetManifestResourceNames();
            var fullName = isPartialName ? allNames.FirstOrDefault(x => x.Contains(name, StringComparison.OrdinalIgnoreCase)) :
                allNames.FirstOrDefault(x => string.Compare(x,name,true) == 0);
            if (fullName == null)
                return "";
            else
                using (var stream = assembly.GetManifestResourceStream(fullName))
                {
                    if (stream == null)
                        return "";
                    using var streamReader = new StreamReader(stream);
                    var content = streamReader.ReadToEnd();
                    return content;
                }
        }

    }
}
