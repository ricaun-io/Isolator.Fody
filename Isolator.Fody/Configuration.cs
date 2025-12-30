using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Fody;

public class Configuration
{
    public Configuration(XElement config)
    {
        // Defaults
        EnableWriteBackRefOutParameters = true;
        DisableEventSubscription = false;
        LoadAtModuleInit = true;

        IsolateTypes = new List<string>();
        IsolateInterfaces = new List<string>();

        EnableWriteBackRefOutParameters = ReadBool(config, nameof(EnableWriteBackRefOutParameters), EnableWriteBackRefOutParameters);
        DisableEventSubscription = ReadBool(config, nameof(DisableEventSubscription), DisableEventSubscription);
        LoadAtModuleInit = ReadBool(config, nameof(LoadAtModuleInit), LoadAtModuleInit);

        IsolateTypes = ReadList(config, nameof(IsolateTypes));
        IsolateInterfaces = ReadList(config, nameof(IsolateInterfaces));
    }

    public bool EnableWriteBackRefOutParameters { get; }
    public bool DisableEventSubscription { get; }
    public bool LoadAtModuleInit { get; }
    public List<string> IsolateTypes { get; }
    public List<string> IsolateInterfaces { get; }

    public static bool ReadBool(XElement config, string nodeName, bool @default)
    {
        return ReadBool(config, nodeName) ?? @default;
    }

    public static bool? ReadBool(XElement config, string nodeName)
    {
        var attribute = config.Attribute(nodeName);
        if (attribute is not null)
        {
            try
            {
                return XmlConvert.ToBoolean(attribute.Value.ToLowerInvariant());
            }
            catch
            {
                throw new WeavingException($"Could not parse '{nodeName}' from '{attribute.Value}'.");
            }
        }

        return null;
    }

    public static List<string> ReadList(XElement config, string nodeName)
    {
        var list = new List<string>();

        var attribute = config.Attribute(nodeName);
        if (attribute is not null)
        {
            foreach (var item in attribute.Value.Split('|').Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                list.Add(item.Trim());
            }
        }

        var element = config.Element(nodeName);
        if (element is not null)
        {
            foreach (var item in element.Value
                                        .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                                        .Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                list.Add(item.Trim());
            }
        }

        return list;
    }
}
