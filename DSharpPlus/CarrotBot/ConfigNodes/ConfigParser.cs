using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using CarrotBot;

public class ConfigParser
{
    public static ConfigNode Parse(string contents)
    {
        try
        {
            string[] lines = contents.Split('\n');
            string previousLine = "";
            string line = lines[0];
            int currentIndex = 0;
            //Skip any preceding comments before using a line as the name
            while(line.StartsWith("//"))
            {
                currentIndex++;
                line = lines[currentIndex];
            }
            //Create the ConfigNode object and find its name based on the current line 
            ConfigNode output = new ConfigNode(line.Trim());
            ConfigNode currentNode = output;
            bool arrayReadMode = false;
            ConfigArray currentArray = null;
            for(int i = currentIndex; i < lines.Length; i++)
            {
                previousLine = line;
                line = lines[i].Trim();
                //Ignore any line that starts with //
                if(line.StartsWith("//"))
                {
                    line = previousLine;
                    continue;
                }
                if(line.Contains("=") && !arrayReadMode)
                {
                    currentNode.Values.Add(line.Split('=')[0].Trim(), line.Split('=')[1].Trim());
                }
                if(line.Contains("{") && !previousLine.Contains("{") && previousLine != output.Name && !arrayReadMode)
                {
                    ConfigNode newNode = new ConfigNode(Regex.Replace(previousLine, @"[^\w\-]", "", RegexOptions.None, TimeSpan.FromSeconds(1)), currentNode);
                    currentNode.AddChild(newNode);
                    currentNode = newNode;
                }
                if(line.Contains("[") && !previousLine.Contains("[") && !arrayReadMode)
                {
                    currentArray = new ConfigArray(Regex.Replace(previousLine, @"[^\w\-]", "", RegexOptions.None, TimeSpan.FromSeconds(1)), currentNode);
                    currentNode.AddArray(currentArray);
                    arrayReadMode = true;
                }
                if(arrayReadMode && !line.Contains("]") && !line.Contains("["))
                {
                    currentArray.Items.Add(line);
                }
                if(line.Contains("]") && arrayReadMode)
                {
                    arrayReadMode = false;
                }
                if(line.Contains("}"))
                {
                    currentNode = currentNode.Parent;
                }
            }
            return output;
        }
        catch(Exception e)
        {
            Logger.Log(e.ToString());
            Logger.Log($"Invalid ConfigNode with first line: {contents.Split('\n')[0]}");
            return null;
        }      
    }
    public static bool TryParse(string contents, out ConfigNode output)
    {
        try
        {
            string[] lines = contents.Split('\n');
            string previousLine = "";
            string line = lines[0];
            int currentIndex = 0;
            //Skip any preceding comments before using a line as the name
            while(line.StartsWith("#"))
            {
                currentIndex++;
                line = lines[currentIndex];
            }
            //Create the ConfigNode object and find its name based on the current line 
            output = new ConfigNode(line.Trim());
            ConfigNode currentNode = output;
            bool arrayReadMode = false;
            ConfigArray currentArray = null;
            for(int i = currentIndex; i < lines.Length; i++)
            {
                previousLine = line;
                line = lines[i].Trim();
                if(line.Contains("=") && !arrayReadMode)
                {
                    currentNode.Values.Add(line.Split('=')[0].Trim(), line.Split('=')[1].Trim());
                }
                if(line.Contains("{") && !previousLine.Contains("{") && previousLine != output.Name && !arrayReadMode)
                {
                    ConfigNode newNode = new ConfigNode(Regex.Replace(previousLine, @"[^\w\-]", "", RegexOptions.None, TimeSpan.FromSeconds(1)), currentNode);
                    currentNode.AddChild(newNode);
                    currentNode = newNode;
                }
                if(line.Contains("[") && !previousLine.Contains("[") && !arrayReadMode)
                {
                    currentArray = new ConfigArray(Regex.Replace(previousLine, @"[^\w\-]", "", RegexOptions.None, TimeSpan.FromSeconds(1)), currentNode);
                    currentNode.AddArray(currentArray);
                    arrayReadMode = true;
                }
                if(arrayReadMode && !line.Contains("]") && !line.StartsWith("#"))
                {
                    currentArray.Items.Add(line);
                }
                if(line.Contains("]") && arrayReadMode)
                {
                    arrayReadMode = false;
                }
                if(line.Contains("}"))
                {
                    currentNode = currentNode.Parent;
                }
            }
            return true;
        }
        catch(Exception e)
        {
            Logger.Log(e.ToString());
            Logger.Log($"Invalid ConfigNode with first line: {contents.Split('\n')[0]}");
            output = null;
            return false;
        }
    }
}