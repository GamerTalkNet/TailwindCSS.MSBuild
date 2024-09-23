using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using GamerTalk.TailwindCSS.MSBuild.Models;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace GamerTalk.TailwindCSS.MSBuild;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class UpdateTailwindContent : Task
{

    [Required] 
    public ITaskItem[] TailwindContentItems { get; set; } = default!;
    
    [Required]
    public ITaskItem ProjectPath { get; set; } = default!;
    
    public override bool Execute()
    {
        var configPath = Path.Combine(ProjectPath.ItemSpec, "tailwind.config.js");
        Log.LogMessage(MessageImportance.High, $"Reading config from {configPath}");

        if (!File.Exists(configPath))
            CreateDefaultTailwindConfig(configPath);

        var content = new List<string>();
        var result = ReadTailwindConfigContent(configPath);
        
        foreach (var item in result.content)
        {
            if (!item.Contains('{') && !item.Contains('*') && !item.Contains('!'))
            {
                var filePath = Path.Combine(ProjectPath.ItemSpec, item);
                if(!File.Exists(filePath))
                    continue;
            }
            
            content.Add(item);
        }
        
        //content.AddRange(result.content);

        foreach (var item in TailwindContentItems)
        {
            var thisItem = item.ItemSpec.Replace('\\', '/');
            
            if(content.Any(x => x.Equals(thisItem, StringComparison.OrdinalIgnoreCase)))
                continue;

            if (!thisItem.Contains('{') && !thisItem.Contains('*') && !thisItem.Contains('!'))
            {
                var filePath = Path.Combine(ProjectPath.ItemSpec, thisItem);
                if(!File.Exists(filePath))
                    continue;
            }
            
            content.Add(thisItem);
        }

        var outLines = new List<string>();
        outLines.AddRange(result.before.Where(x => !string.IsNullOrWhiteSpace(x)));
        outLines.Add("  content: [");
        foreach (var contentItem in content)
            outLines.Add($"    \"{contentItem}\"{(contentItem == content.Last() ? "" : ",")}");
        outLines.Add("  ],");
        outLines.AddRange(result.after.Where(x => !string.IsNullOrWhiteSpace(x)));
        
        File.WriteAllLines(configPath, outLines);
        
        Log.LogMessage(MessageImportance.High, File.ReadAllText(configPath));
            
        return true;
    }

    private (List<string> before, List<string> content, List<string> after) ReadTailwindConfigContent(string fileName)
    {
        var lines = File.ReadLines(fileName);

        var contentFound = false;
        var inContent = false;
        var afterContent = false;

        var before = new List<string>();
        var sbContent = new StringBuilder();
        var after = new List<string>();
        
        foreach (var line in lines)
        {
            
            if(afterContent)
                after.Add(line.TrimEnd('\n').TrimEnd('\r'));
            
            if(line.Contains("content:"))
                contentFound = true;
            
            if(!contentFound)
                before.Add(line.TrimEnd('\n').TrimEnd('\r'));
            
            if(contentFound && line.Contains('[') && !afterContent)
                inContent = true;

            if (inContent)
            {
                var thisLine = line.IndexOf('[') == -1 ? line : line.Substring(line.IndexOf('['));

                if (thisLine.EndsWith("],"))
                {
                    thisLine = thisLine.Substring(0, thisLine.Length - 1);
                }

                sbContent.AppendLine(thisLine);
            }

            if (inContent && line.Contains(']'))
            {
                inContent = false;
                afterContent = true;
            }
        }

        var dummyContent = $"{{\n  \"content\": {sbContent}}}";
        Log.LogMessage(MessageImportance.High, $"Content:\n{dummyContent}");
        
        var config = JsonSerializer.Deserialize<TailwindConfig>(dummyContent)
                     ?? throw new InvalidOperationException("Failed to deserialize TailwindCSS content.");
        
        return (before, config.Content, after);
    }
    
    private static void CreateDefaultTailwindConfig(string fileName)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("/** @type {import('tailwindcss').Config} */");
        sb.AppendLine("module.exports = {");
        sb.AppendLine("  content: [],");
        sb.AppendLine("  theme: {");
        sb.AppendLine("    extend: {},");
        sb.AppendLine("  },");
        sb.AppendLine("  plugins: [],");
        sb.AppendLine("}");
        
        File.WriteAllText(fileName, sb.ToString());
    }
}