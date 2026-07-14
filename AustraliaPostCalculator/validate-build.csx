// Simple validation script to check compilation
#r "nuget: Microsoft.NET.Sdk.Web"

using System;
using System.IO;
using System.Diagnostics;

var projectPath = "e:\\workspaces\\Tierdrop\\code\\OpenSourceSupport\\AustraliaPostCalculator";
var dotnetPath = "dotnet";

try 
{
    var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = dotnetPath,
            Arguments = "build",
            WorkingDirectory = projectPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }
    };

    process.Start();
    string output = process.StandardOutput.ReadToEnd();
    string error = process.StandardError.ReadToEnd();
    process.WaitForExit();

    if (process.ExitCode == 0)
    {
        Console.WriteLine("✅ Build successful!");
        Console.WriteLine(output);
    }
    else
    {
        Console.WriteLine("❌ Build failed!");
        Console.WriteLine("Error output:");
        Console.WriteLine(error);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Exception occurred: {ex.Message}");
}
