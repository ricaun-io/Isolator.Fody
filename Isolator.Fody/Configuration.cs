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
        ContextName = null;
        ContextName = ReadString(config, nameof(ContextName));

        ClassNames = new List<string>();
        ClassNames = ReadList(config, nameof(ClassNames));

        InterfaceNames = new List<string>();
        InterfaceNames = ReadList(config, nameof(InterfaceNames));

        EnableDebug = false;
        EnableDebug = ReadBool(config, nameof(EnableDebug), EnableDebug);

        SkipIsolator = false;
        SkipIsolator = ReadBool(config, nameof(SkipIsolator), SkipIsolator);

        EnableCloneMethods = true;
        EnableCloneMethods = ReadBool(config, nameof(EnableCloneMethods), EnableCloneMethods);

        EnableWriteBackRefOutParameters = true;
        EnableWriteBackRefOutParameters = ReadBool(config, nameof(EnableWriteBackRefOutParameters), EnableWriteBackRefOutParameters);
    }

    public bool SkipIsolator { get; }
    public bool EnableDebug { get; }
    public bool EnableCloneMethods { get; }
    public bool EnableWriteBackRefOutParameters { get; }
    public List<string> ClassNames { get; }
    public List<string> InterfaceNames { get; }
    public string ContextName { get; }

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

    public static string ReadString(XElement config, string nodeName)
    {
        var attribute = config.Attribute(nodeName);
        if (attribute is not null)
        {
            return attribute.Value?.Trim();
        }
        return null;
    }
}
