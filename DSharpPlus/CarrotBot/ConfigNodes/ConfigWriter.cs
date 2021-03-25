using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class ConfigWriter
{
    public static string Write(ConfigNode node, int currentDepth = 0)
    {
        //It feels wrong to do it like this but this way it's nice and neatly indented
        string indent = "";
        for(int i = 0; i < currentDepth; i++)
        {
            indent += "    ";
        }
        string indent2 = indent + "    ";
        string output = $"{indent}{node.Name}\n{indent}{{";
        foreach(KeyValuePair<string, string> pair in node.Values)
        {
            output += $"\n{indent2}{pair.Key} = {pair.Value}";
        }
        for(int i = 0; i < node.Children.Count; i++)
        {
            output += $"\n{Write(node.Children[i], currentDepth + 1)}";
        }
        for(int i = 0; i < node.Arrays.Count; i++)
        {
            ConfigArray currentArray = node.Arrays[i];
            output += $"\n{WriteArray(currentArray, currentDepth + 1)}";
        }
        output += $"\n{indent}}}";
        return output;
    }
    public static string WriteArray(ConfigArray array, int currentDepth = 0)
    {
        string indent = "";
        for(int i = 0; i < currentDepth; i++)
        {
            indent += "    ";
        }
        string indent2 = indent + "    ";
        string output = $"{indent}{array.Name}\n{indent}[";
        foreach(string str in array.Items)
        {
            output += $"\n{indent2}{str}";
        }
        output += $"\n{indent}]";
        return output;
    }
}
