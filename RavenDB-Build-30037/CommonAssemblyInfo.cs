﻿using System;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyCompany("Hibernating Rhinos")]
[assembly: AssemblyCopyright("© Hibernating Rhinos 2004 - 2016 All rights reserved.")]
[assembly: AssemblyTrademark("")]
[assembly: ComVisible(false)]
[assembly: CLSCompliant(false)]
[assembly: AssemblyTitle("RavenDB")]
[assembly: AssemblyVersion("3.0.0")]
[assembly: AssemblyFileVersion("3.0.30037.0")]
[assembly: AssemblyInformationalVersion("3.0.0 / d9d1e03 / ")]
[assembly: AssemblyProduct("RavenDB")]
[assembly: AssemblyDescription("RavenDB is a second generation LINQ enabled document database for .NET")]

#if !DNXCORE50

[assembly: SuppressIldasm]
#endif

#if DEBUG

[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyDelaySign(false)]
[assembly: NeutralResourcesLanguage("en-US")]
[assembly: RavenVersion(Build = "30037", CommitHash = "d9d1e03", Version = "3.0")]

[AttributeUsage(AttributeTargets.Assembly)]
public class RavenVersionAttribute : Attribute
{
    private string build;
    public string CommitHash { get; set; }
    public string Build
    {
        get
        {
            int _;
            if (int.TryParse(build, out _) == false)
                return "13";
            return build;
        }
        set { build = value; }
    }
    public string Version { get; set; }
}
