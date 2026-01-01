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
        EnableCloneMethods = true;
        EnableWriteBackRefOutParameters = true;
        DisableEventSubscription = false;
        LoadAtModuleInit = true;

        ClassNames = new List<string>();
        InterfaceNames = new List<string>();

        EnableCloneMethods = ReadBool(config, nameof(EnableCloneMethods), EnableCloneMethods);
        EnableWriteBackRefOutParameters = ReadBool(config, nameof(EnableWriteBackRefOutParameters), EnableWriteBackRefOutParameters);
        DisableEventSubscription = ReadBool(config, nameof(DisableEventSubscription), DisableEventSubscription);
        LoadAtModuleInit = ReadBool(config, nameof(LoadAtModuleInit), LoadAtModuleInit);

        ClassNames = ReadList(config, nameof(ClassNames));
        InterfaceNames = ReadList(config, nameof(InterfaceNames));
    }

    public bool EnableCloneMethods { get; }
    public bool EnableWriteBackRefOutParameters { get; }
    public bool DisableEventSubscription { get; }
    public bool LoadAtModuleInit { get; }
    public List<string> ClassNames { get; }
    public List<string> InterfaceNames { get; }

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
