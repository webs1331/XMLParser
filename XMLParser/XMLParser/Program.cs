using System.Xml.Linq;

namespace XMLParser;

internal class Program
{
    static async Task Main(string[] args)
    {
        var requiredFields = new List<string>
        {
            "origin",
            "pubdate",
            "title",
            "geoform",
            "abstract",
            "purpose",
            "caldate",
            "current",
            "progress",
            "update",
            "themekt",
            "themekey",
            "accconst",
            "useconst",
            "ptcontac",
            "cntorgp",
            "cntaddr",
            "cntvoice",
            "cntemail",
            "datacred",
            "native",
            "logic",
            "complete",
            "horizpar",
            "lineage",
            "procdesc",
            "procdate",
            "enttypl",
            "enttypd",
            "cntorgp",
            "cntaddr"
        };

        foreach (string file in Directory.EnumerateFiles("C:\\Users\\cqb13\\Desktop\\xmldocs", "*.xml"))
        {
            var outputFilePath = $"C:\\Users\\cqb13\\Desktop\\Logs\\{Path.GetFileNameWithoutExtension(file)}.txt";
            var logOutputText = new List<string>();

            XElement dataset = XElement.Load(file);

            foreach (var requiredField in requiredFields)
            {
                var field = dataset.Descendants(requiredField).FirstOrDefault();

                if (field == null)
                {
                    logOutputText.Add($"Missing {requiredField.ToUpper()}");
                }
                else
                {
                    //we did find a field, lets check it's value
                    if (field.IsEmpty)
                    {
                        logOutputText.Add($"Found {requiredField.ToUpper()} but value was empty.");
                        continue;
                    }
                    
                    logOutputText.Add($"Found {requiredField.ToUpper()} with value '{field.Value}'.");
                }
            }

            logOutputText.AddRange(ProcessAttributes(dataset));
            logOutputText.AddRange(CheckAddressBlocks(dataset));

            await File.WriteAllLinesAsync(outputFilePath, logOutputText);
        }
    }

    public static List<string> ProcessAttributes(XElement dataset)
    {
        var parent = "attr";

        var requiredFields = new List<string>
        {
            "attrlabl",
            "attrdef",
            "attrdefs",
            "attrdomv"
        };

        var logOutputText = new List<string>();
        var attributes = dataset.Descendants(parent);
        bool isDomvMissing = false;

        foreach (var attribute in attributes)
        {
            string attributeLabel = null;

            foreach (var requiredField in requiredFields)
            {
                var attributeChildren = attribute.Descendants(requiredField);

                if (!attributeChildren.Any() || attributeChildren.First().IsEmpty)
                    logOutputText.Add($"{requiredField.ToUpper()} missing for attribute {attributeLabel ?? "missing label"}.");

                if (requiredField == "attrlabl" && attributeChildren.Any())
                    attributeLabel = attributeChildren.First().Value;

                if (requiredField == "attrdomv" && !attributeChildren.Any())
                    isDomvMissing = true;
            }

            if(!isDomvMissing)
                logOutputText.AddRange(CheckAttributeDomv(attribute, attributeLabel));

            isDomvMissing = false;
        }
        
        return logOutputText;
    }

    public static List<string> CheckAttributeDomv(XElement attribute, string attributeLabel)
    {
        var logOutputText = new List<string>();

        var edomParent = "edom";
        var edomRequiredFields = new List<string>
        {
            "edomv",
            "edomvd",
            "edomvds",
        };

        var udomParent = "udom";
        var rdomParent = "rdom";

        var edomvChildren = attribute.Descendants(edomParent).ToList();
        var udomvChild = attribute.Descendants(udomParent).FirstOrDefault();
        var rdomvChildren = attribute.Descendants(rdomParent).ToList();

        if (!edomvChildren.Any() && udomvChild == null && !rdomvChildren.Any())
        {
            logOutputText.Add($"No edom, udom or rdom found for attribute {attributeLabel.ToUpper()}");

            return logOutputText;
        }

        if (edomvChildren.Any())
        {
            foreach (var edomRequiredField in edomRequiredFields)
            {
                var edomvChild = edomvChildren.Descendants(edomRequiredField).FirstOrDefault();

                if (edomvChild == null || edomvChild.IsEmpty)
                    logOutputText.Add($"Missing tag or value for {edomRequiredField.ToUpper()} for {attributeLabel.ToUpper()}");
            }
        }

        if (udomvChild != null && !udomvChild.IsEmpty)
        {
            string udomvChildValue = udomvChild.Value.Trim().ToLower().Replace(".", string.Empty);

            if (udomvChildValue is "none"
                or "code"
                or "name"
                or "date"
                or "code and description"
                or "description"
                or "text"
                or "name"
                or "null"
                or "unknown"
                or "mm/dd/yyyy hhmi"
                or "year")
            {
                logOutputText.Add($"Incorrect udomv format/value: '{udomvChild.Value}' for {attributeLabel.ToUpper()}");
            }
        }

        return logOutputText;
    }

    public static List<string> CheckAddressBlocks(XElement dataset)
    {
        var logOutputText = new List<string>();

        var parents = new List<string> {"idinfo", "distinfo", "metainfo"};

        var requiredFields = new List<string>
        {
            "addrtype",
            "city",
            "state",
            "postal",
            "country"
        };

        foreach (var parent in parents)
        {
            var addressParent = dataset.Descendants(parent).ToList();
            var cntAddr = addressParent.Descendants("cntaddr").ToList();

            foreach (var requiredField in requiredFields)
            {
                var addressField = cntAddr.Descendants(requiredField).FirstOrDefault();
                if (addressField == null || addressField.IsEmpty)
                    logOutputText.Add($"Missing tag or value for {requiredField.ToUpper()} for {parent.ToUpper()} address.");
            }
        }

        return logOutputText;
    }
}