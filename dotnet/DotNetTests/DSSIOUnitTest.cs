﻿using Hec.Dss;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DSSUnitTests
{
  /// <summary>
  /// Summary description for UnitTest1
  /// </summary>
  [TestClass]
  public class DSSIOUnitTest
  {

    public DSSIOUnitTest()
    {
    }

    private TestContext testContextInstance;

    /// <summary>
    ///Gets or sets the test context which provides
    ///information about and functionality for the current test run.
    ///</summary>
    public TestContext TestContext
    {
      get
      {
        return testContextInstance;
      }
      set
      {
        testContextInstance = value;
      }
    }


    [TestMethod]
    public void GenerateRandomRecordsAndRead()
    {
      var dssFile = TestUtility.GetCopyForTesting("version7AlbersGridsTimeSeries.dss");
      GenerateRecords(dssFile);
      RunReadTest(dssFile);
    }

    


    [TestMethod]
    public void TestGetRecordType()
    {
      string dssFileName = TestUtility.GetCopyForTesting("sample7.dss");

      using (DssReader r = new DssReader(dssFileName))
      {
        var catalog = r.GetCatalog();
        var path = catalog.GetCondensedPath(new DssPath("//SACRAMENTO/PRECIP-INC/11Jul1877 - 30Jun2009/1Day/OBS/"));
        var recordType = r.GetRecordType(path);
        Assert.AreEqual(RecordType.RegularTimeSeries,recordType);
      }
    }

    [TestMethod]
    public void TestGetRecordTypeNoDPart()
    {
      var dssFile = TestUtility.GetCopyForTesting("sample7.dss");
      using (DssReader r = new DssReader(dssFile))
      {
        var recordType = r.GetRecordType(new DssPath("//SACRAMENTO/PRECIP-INC//1Day/OBS/"));
        Assert.IsTrue(recordType == RecordType.RegularTimeSeries);
      }
    }

    
    

    [TestMethod]
    public void GetHeaderInformationTimeSeries()
    {
      string dssFile = TestUtility.GetCopyForTesting(@"benchmarks6\BaldEDmbrk7.dss");
      string path = "/BALD EAGLE LOC HAV/105178.6/FLOW-CUM/17Feb1999-23Feb1999/1Minute/DAMBRKSIMBRCH/";
      using (DssReader dss = new DssReader(dssFile))
      {
        DssPathCollection paths = dss.GetCatalog();
        var dsspath = paths.FindExactPath(path);
        var ts = dss.GetEmptyTimeSeries(dsspath);
        Assert.IsTrue(ts.Units == "ACRE-FT");
        Assert.IsTrue(ts.DataType == "INST-CUM");
      }
    }

    [TestMethod]
    public void GetHeaderInformationPairedData()
    {
      string dssFile = TestUtility.GetCopyForTesting(@"benchmarks6\BaldEDmbrk7.dss");
      string path = @"/BALD EAGLE LOC HAV//LOCATION-ELEV//19FEB1999 2055/DAMBRKSIMBRCH/";
      using (DssReader dss = new DssReader(dssFile))
      {
        DssPathCollection paths = dss.GetCatalog();
        var dsspath = paths.FindExactPath(path);
        // read meta-data only.
        var pd = dss.GetPairedData(dsspath.FullPath,true);
        Assert.AreEqual("FEET", pd.UnitsDependent);
        Assert.AreEqual("UNT", pd.TypeDependent);
        Assert.AreEqual("Riv Sta",pd.UnitsIndependent);

        // read full record
        pd = dss.GetPairedData(dsspath.FullPath);
        Assert.AreEqual("FEET",pd.UnitsDependent );
        Assert.AreEqual("UNT",pd.TypeDependent);
        Assert.AreEqual("Riv Sta", pd.UnitsIndependent);

        Assert.AreEqual(178, pd.Ordinates.Length);
        Assert.AreEqual(1, pd.CurveCount);
      }
    }

    [TestMethod]
    public void CheckIfComparisonWorks()
    {
      var path1 = new DssPath("A", "", "", "", "", "");
      var path2 = new DssPath("B", "", "", "", "", "");
      Assert.IsTrue(path1.CompareTo(path2) < 0);

      var path3 = new DssPath("A", "A", "", "", "", "");
      var path4 = new DssPath("A", "B", "", "", "", "");
      Assert.IsTrue(path3.CompareTo(path4) < 0);

      var paths = new List<DssPath>();
      paths.Add(path4);
      paths.Add(path1);
      paths.Add(path2);
      paths.Add(path3);

      paths = paths.OrderBy(q => q).ToList();
      Assert.IsTrue(paths[0] == path1);
      Assert.IsTrue(paths[1] == path3);
      Assert.IsTrue(paths[2] == path4);
      Assert.IsTrue(paths[3] == path2);
    }

    [TestMethod]
    public void CheckGetCorrespondingCondensedPath()
    {
      string dssFile = TestUtility.GetCopyForTesting(@"benchmarks6\Ark7.dss");
      DssPath path = new DssPath(@"/A34-01 A34-01/-2600.0/FLOW/01JAN2003/1DAY/VER 6.1.3 03-05/");
      using (DssReader reader = new DssReader(dssFile))
      {
        DssPathCollection paths = reader.GetCatalog();
        DssPathCondensed conPath = paths.GetCondensedPath(path);
        Assert.IsTrue(conPath != null);
      }
    }

    [TestMethod]
    public void CheckIfDPartComingOutAsDateTimeNoRange()
    {
      var dssFile = TestUtility.GetCopyForTesting("version7AlbersGrid.dss");
      string path = @"/SHG/TRUCKEE RIVER/TEMP-AIR/31JAN2016:2400//INTERPOLATED-ROUNDED/";
      using (DssReader dss = new DssReader(dssFile))
      {
        DssPathCollection paths = dss.GetCatalog();
        var dsspath = paths.FindExactPath(path);
        if (dsspath != DssPath.NotFound)
        {
          var dpartdt = dsspath.GetDPartAsDateTime();
          Assert.IsTrue(dpartdt.Month == 2);
          Assert.IsTrue(dpartdt.Day == 1);
          Assert.IsTrue(dpartdt.Year == 2016);
          Assert.IsTrue(dpartdt.Hour == 0);
          Assert.IsTrue(dpartdt.Minute == 0);
        }
        else
        {
          Assert.Fail();
        }
      }
    }

    [TestMethod]
    public void CheckIfDPartComingOutAsDateTimeWithRange()
    {
      string dssFile = TestUtility.GetCopyForTesting("version7AlbersGridsTimeSeries.dss");
      string path = @"//BERLIN/FLOW/01May2016-01Jun2016/1Hour/Q0W0/";
      using (DssReader dss = new DssReader(dssFile))
      {
        DssPathCollection paths = dss.GetCatalog();
        var dsspath = paths.FindExactPath(path);
        if (dsspath != DssPath.NotFound)
        {
          var dpartdt = (dsspath as DssPathCondensed).GetDPartAsDateTimeRange();
          Assert.IsTrue(dpartdt.Item1.Month == 5);
          Assert.IsTrue(dpartdt.Item1.Day == 1);
          Assert.IsTrue(dpartdt.Item1.Year == 2016);
          Assert.IsTrue(dpartdt.Item2.Month == 6);
          Assert.IsTrue(dpartdt.Item2.Day == 1);
          Assert.IsTrue(dpartdt.Item2.Year == 2016);
        }
        else
        {
          Assert.Fail();
        }
      }
    }


    private static void GenerateRecords(string dssFile)
    {
      string[] possibleCParts = { "%", "Area", "Code", "Coeff", "Conc", "Cond", "Count", "Currency", "Current", "Depth", "Dir", "Dist", "Elev", "Energy", "Evap", "EvapRate", "Fish", "Flow", "Frost", "Head", "Height", "Irrad", "Length", "Opening", "Power", "Precip", "Pres", "Rad", "Ratio", "Rotation", "Speed", "SpinRate", "Stage", "Stor", "Temp", "Thick", "Timig", "Travel", "Turb", "TurbF", "TurbJ", "TurbN", "Volt", "Volume", "Width", "pH" };
      string[] possibleUnits = { "%", "ft2", "n/a", "n/a", "ppm", "umho/cm", "unit", "$", "ampere", "in", "deg", "mi", "ft", "MWH", "in", "in/day", "unit", "cfs", "in", "ft", "ft", "langley/min", "ft", "ft", "MW", "in", "in-hg", "langley", "n/a", "deg", "mph", "rpm", "ft", "ac-ft", "F", "in", "sec", "mi", "JTU", "FNU", "JTU", "NTU", "volt", "ft3", "ft", "su" };
      using (DssWriter dss = new DssWriter(dssFile))
      {
        Random rng = new Random();
        DateTime start = new DateTime(1969, 1, 1);
        DateTime randomDate = generateRandomDate(rng, start);
        for (int i = 0; i < 100; i++)
        {
          int cPartchosen = rng.Next(0, possibleCParts.Length);
          string CPart = possibleCParts[cPartchosen];
          string pathname = GenerateRandomPathName(CPart, turnIntoAcceptableDate(randomDate.ToString("MM-dd-yyyy")), rng);
          dss.Write(new TimeSeries(pathname, new double[300], randomDate, possibleUnits[cPartchosen], "INST-VAL"));
        }
      }
    }

    private static string turnIntoAcceptableDate(string v)
    {
      string[] split = v.Split('-');
      string month = "";
      if (split[0] == "01")
        month = "Jan";
      else if (split[0] == "02")
        month = "Feb";
      else if (split[0] == "03")
        month = "Mar";
      else if (split[0] == "04")
        month = "Apr";
      else if (split[0] == "05")
        month = "May";
      else if (split[0] == "06")
        month = "Jun";
      else if (split[0] == "07")
        month = "Jul";
      else if (split[0] == "08")
        month = "Aug";
      else if (split[0] == "09")
        month = "Sep";
      else if (split[0] == "10")
        month = "Oct";
      else if (split[0] == "11")
        month = "Nov";
      else
        month = "Dec";
      return split[1] + month + split[2];
    }

    private static string GenerateRandomPathName(string cpart, string dateTime, Random rng)
    {
      string[] possibleAParts = { "Cathedral", "Marsh", "Establishment", "Alley", "Couch", "Comet", "Playground", "Coast" };
      string possibleBPart = "Location";

      string[] possibleEParts = { "1Second", "1Minute", "1Hour", "1Day", "1Week", "1Month", "1Year" };
      string F = ((char)rng.Next(0, 128)).ToString();
      while (F == "/")
        F = ((char)rng.Next(0, 128)).ToString();

      return "/" + possibleAParts[rng.Next(0, possibleAParts.Length)] + "/" + possibleBPart + "/" + cpart + "/" + dateTime + "/" + possibleEParts[rng.Next(0, possibleEParts.Length)] + "/" + F + "/";
    }

    private static DateTime generateRandomDate(Random gen, DateTime start)
    {

      int range = (DateTime.Today - start).Days;
      return start.AddDays(gen.Next(range));
    }
    private static void RunReadTest(string dssFile)
    {
      using (DssReader dss = new DssReader(dssFile))
      {
        DssPathCollection paths = dss.GetCatalog();
        var pathsFound = paths.FilterByPart();
        string path = pathsFound[0].FullPath;
      }
    }

    
  }
}
