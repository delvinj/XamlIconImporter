using System.Xml;
using System.Xml.Linq;

namespace XamlIconImporter;

internal static class SafeXamlReader
{
    public static XDocument Load(string path)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            IgnoreComments = false,
            IgnoreWhitespace = true
        };

        using var reader = XmlReader.Create(path, settings);
        return XDocument.Load(reader, LoadOptions.None);
    }
}
