using System;
using System.Collections.Generic;

/// <summary>
/// Array of string values for use in configs
/// </summary>
public class ConfigArray
{
    public string Name { get; set; }
    public List<string> Items { get; set; }
    public ConfigNode Parent { get; set; }

    #region Constructors

    public ConfigArray(string name)
    {
        Name = name;
        Items = new List<string>();
        Parent = null;
    }
    public ConfigArray(string name, ConfigNode parent)
    {
        Name = name;
        Parent = parent;
        Items = new List<string>();
    }
    public ConfigArray(string name, List<string> items)
    {
        Name = name;
        Items = items;
        Parent = null;
    }
    public ConfigArray(string name, ConfigNode parent, List<string> items)
    {
        Name = name;
        Items = items;
        Parent = parent;
    }

    #endregion
}