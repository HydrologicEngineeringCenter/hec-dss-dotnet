using Hec.Dss;
using System.Collections.Concurrent;
// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

var fileName = Path.GetTempFileName();
Console.WriteLine( "attempting to create '"+fileName+"'");
DssReader dss = new DssReader(fileName);