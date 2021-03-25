using System.Collections;
using System.Collections.Generic;

public class ConfigNode
{
    public string Name { get; set; }
    public ConfigNode Parent { get; set; }
    public Dictionary<string, string> Values { get; }
    public List<ConfigArray> Arrays { get; }
    public List<ConfigNode> Children { get; }
    public int Depth { get; set; }

    public void AddChild(ConfigNode node)
    {
        node.Parent = this;
        node.Depth = Depth + 1;
        Children.Add(node);
    }
    public void AddValue(string key, string value)
    {
        Values.Add(key, value);
    }
    public void AddArray(ConfigArray @array)
    {
        @array.Parent = this;
        Arrays.Add(@array);
    }
    
    #region Constructors
    public ConfigNode(string name)
    {
        Name = name;
        Values = new Dictionary<string, string>();
        Children = new List<ConfigNode>();
        Arrays = new List<ConfigArray>();
        Parent = null;
    }
    public ConfigNode(string name, ConfigNode parent)
    {
        Name = name;
        Parent = parent;
        Values = new Dictionary<string, string>();
        Children = new List<ConfigNode>();
        Arrays = new List<ConfigArray>();
    }
    public ConfigNode(string name, Dictionary<string, string> values)
    {
        Name = name;
        Values = values;
        Children = new List<ConfigNode>();
        Arrays = new List<ConfigArray>();
    }
    public ConfigNode(string name, List<ConfigNode> children)
    {
        Name = name;
        Children = children;
        Values = new Dictionary<string, string>();
        Arrays = new List<ConfigArray>();
    }
    public ConfigNode(string name, List<ConfigArray> arrays)
    {
        Name = name;
        Arrays = arrays;
        Children = new List<ConfigNode>();
        Values = new Dictionary<string, string>();
    }
    public ConfigNode(string name, Dictionary<string, string> values, List<ConfigNode> children)
    {
        Name = name;
        Values = values;
        Children = children;
        Arrays = new List<ConfigArray>();
    }
    #endregion
}