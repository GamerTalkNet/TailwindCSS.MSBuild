using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GamerTalk.TailwindCSS.MSBuild.Models;

[Serializable]
public class TailwindConfig
{
    
    [JsonPropertyName("content")] 
    public List<string> Content { get; set; } = new List<string>();
    
}