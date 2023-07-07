using Hec.Dss;
using System.Collections.Concurrent;
// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

var fileName = Path.GetTempFileName();
Console.WriteLine( "attempting to create '"+fileName+"'");
using DssReader dss = new DssReader(fileName,0);
var version = DssReader.GetVersion(fileName);
Console.WriteLine(   "version:  "+version);
version = DssReader.GetVersion("does not exist2");
Console.WriteLine("version:  " + version);