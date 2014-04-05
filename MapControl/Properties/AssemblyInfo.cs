﻿using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;

#if SILVERLIGHT
[assembly: AssemblyTitle("Silverlight Map Control")]
[assembly: AssemblyDescription("XAML Map Control Library for Silverlight")]
#else
[assembly: AssemblyTitle("WPF Map Control")]
[assembly: AssemblyDescription("XAML Map Control Library for WPF")]
[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]
#endif

[assembly: AssemblyProduct("XAML Map Control")]
[assembly: AssemblyCompany("Clemens Fischer")]
[assembly: AssemblyCopyright("Copyright © 2014 Clemens Fischer")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyVersion("1.12.0")]
[assembly: AssemblyFileVersion("1.12.0")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
