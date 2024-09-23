using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

// ReSharper disable once CheckNamespace
namespace Microsoft.Build.Tasks;

public class UpdateTailwindContent : Task
{
    
    private static readonly bool IsWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;

    [Required]
    public ITaskItem[] TailwindContentItems { get; set; } = default!;
    
    public override bool Execute()
    {
        return true;
    }
}