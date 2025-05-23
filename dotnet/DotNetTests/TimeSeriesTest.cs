﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Hec.Dss;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DSSUnitTests
{
  [TestClass]
  public class TimeSeriesTest
  {
    //[TestMethod]
    public void QueryWithCondensedPath()
    {
      var filename = TestUtility.GetCopyForTesting("MoRiverObs_TimeWindowTest.dss");
      using (DssReader r = new DssReader(filename))
      {
        var catalog = r.GetCatalog();
        var paths = catalog.FilterByPart("CHARITON RIVER", "PRAIRIE HILL, MO", "FLOW", "", "15MIN", "USGS");
        var path1 = paths[0];
        var t1 = new DateTime(2008, 9, 2);
        var t2 = new DateTime(2008, 10, 11);
        var ts = r.GetTimeSeries(path1, t1, t2);

      }
    }

    [TestMethod]
    public void QueryTimeWindow()
    {
      var filename = TestUtility.GetCopyForTesting("MoRiverObs_TimeWindowTest.dss");

      using (DssReader r = new DssReader(filename))
      {
        var t1 = new DateTime(2008, 9, 2);
        var t2 = new DateTime(2008, 10, 11);
        var ts = r.GetTimeSeries(new DssPath("/CHARITON RIVER/PRAIRIE HILL, MO/FLOW//15MIN/USGS/"), t1, t2);



        Console.WriteLine("first point : " + ts[0]);
        Console.WriteLine("last pont   : " + ts[ts.Count - 1]);
        Assert.IsTrue(ts[0].DateTime == t1);
        Assert.IsTrue(ts[ts.Count - 1].DateTime == t2);

        Console.WriteLine(ts);

      }


    }


    [TestMethod]
    public void TestOutofRangeTimeWindow7()
    {
      var filename = TestUtility.GetCopyForTesting("rainfall7.dss");

      ReadWithDates(filename);

    }

    /// <summary>
    /// The query for data is outside the range of the stored data.
    /// we expect to return an empty  time series (but please don't crash!)
    /// </summary>
    /// <param name="filename"></param>
    private static void ReadWithDates(string filename)
    {
      using (DssReader r = new DssReader(filename))
      {
        var c = r.GetCatalog().ToDataTable();
        DateTime t1 = new DateTime(2004, 12, 1);
        DateTime t2 = t1.AddYears(1);
        var ts = r.GetTimeSeries(new DssPath("//TYRONE 4 NE BALD EAG/PRECIP-INC/01JUN1972/1HOUR/OBS/"), t1, t2);
        Assert.IsTrue(ts.Count == 0);
      }
    }

    [TestMethod]
    public void TestOutofRangeTimeWindow()
    {
      var filename = TestUtility.GetCopyForTesting("rainfall7.dss");
      ReadWithDates(filename);
    }


    [TestMethod]
    public void RegularTSRecordType()
    {
      var filename = TestUtility.GetCopyForTesting("MoRiverObs7.dss");
      using (DssReader r = new DssReader(filename))
      {
        var recordType = r.GetRecordType(new DssPath("/MISSOURI RIVER/BOONVILLE, MO/FLOW/01OCT1987/15MIN/USGS/"));
        Assert.IsTrue(recordType == RecordType.RegularTimeSeries);
      }
    }

    [TestMethod]
      [Ignore]
      public void TestTimeSeriesProfile()
    {
      string filename = TestUtility.GetSimpleTempFileName(".dss");
      int numRows = 10000;
      DateTime t1 = new DateTime(2022, 1, 1);
      DateTime t2 = new DateTime(2022, 1, 2);

      using (var w = new DssWriter(filename))
      {
        var p = CreateHourlyProfileData(numRows, 12, t1);
        w.Write(p, true);

        var p2 = w.GetTimeSeriesProfile(p.Path);
        Assert.AreEqual(numRows, p2.Count);
        var psubset = w.GetTimeSeriesProfile(p.Path, t1, t2);
        Assert.AreEqual(25, psubset.Count);

        Assert.AreEqual(p.ColumnValues.Length, p2.ColumnValues.Length);
        for (int r = 0; r < p.Values.GetLength(0); r++)
        {
          for (int c = 0; c < p.Values.GetLength(1); c++)
          {
            Assert.AreEqual(p.Values[r, c], p2.Values[r, c], 0.0001);
          }
        }


      }

    }

    private static TimeSeriesProfile CreateHourlyProfileData(int numRows, int numColumns, DateTime startDate)
    {
      TimeSeriesProfile p = new TimeSeriesProfile();
      p.StartDateTime = DateTime.Now.Date;

      int _rows = numRows;
      int _columns = numColumns;


      double[,] data = new double[_rows, _columns];
      double[] columnValues = new double[_columns];
      DateTime[] dates = new DateTime[_rows];
      DateTime t = startDate;
      int counter = 1;
      for (int i = 0; i < data.GetLength(0); i++)
      {
        for (int j = 0; j < _columns; j++)
        {
          data[i, j] = counter++; // Math.Cos(j) * Math.PI + i;
          columnValues[j] = j;
        }
        dates[i] = t;
        t = t.AddHours(1);

      }

      p.Times = dates;
      p.Values = data;
      p.ColumnValues = columnValues;
      p.Units = "cfs";
      p.ColumnUnits = "";
      p.DataType = "Inst-Val";
      p.Path = new DssPath("/dotnet/profile/ensemble-flow//1Hour/365/");

      var tbl = p.ToDataTable();
      //Console.WriteLine(tbl);
      return p;
    }

    [TestMethod]
    public void ReadMinimalTimeSeriesV7()
    {
      using (DssReader r = new DssReader(TestUtility.GetCopyForTesting("sample7.dss")))
      {
        var ts = r.GetEmptyTimeSeries(new DssPath("//SACRAMENTO/PRECIP-INC/01Jan1877/1Day/OBS/"));
        Assert.AreEqual("INCHES", ts.Units);
        Assert.AreEqual("PER-CUM", ts.DataType);
      }
    }

    [TestMethod]
    public void ReadCondensedPathName()
    {
      using (DssReader r = new DssReader(TestUtility.GetCopyForTesting("sample7.dss")))
      {
        var catalog = r.GetCatalog();
        var path = catalog.GetCondensedPath(new DssPath("//SACRAMENTO/PRECIP-INC/11Jul1877 - 30Jun2009/1Day/OBS/"));
        var timeSeries = r.GetTimeSeries(path);
        Assert.AreEqual(48202, timeSeries.Values.Length);
      }
    }

    [TestMethod]
    public void ReadCondensedPathNameWithTimeWindow()
    {
      using (DssReader r = new DssReader(TestUtility.GetCopyForTesting("sample7.dss")))
      {
        var catalog = r.GetCatalog();
        var path = catalog.GetCondensedPath(new DssPath("//SACRAMENTO/PRECIP-INC/11Jul1877 - 30Jun2009/1Day/OBS/"));
        var timeSeries = r.GetTimeSeries(path, new DateTime(1950, 11, 1), new DateTime(1952, 10, 31));
        Assert.AreEqual(731, timeSeries.Values.Length);
      }
    }

    [TestMethod]
    public void Read1944PathWith1950TimeWindow()
    {
      using (DssReader r = new DssReader(TestUtility.GetCopyForTesting("sample7.dss")))
      {
        var path = new DssPath("//SACRAMENTO/PRECIP-INC/01JAN1944/1Day/OBS/");
        var timeSeries = r.GetTimeSeries(path, new DateTime(1950, 11, 1), new DateTime(1952, 10, 31));
        Assert.AreEqual(731, timeSeries.Values.Length);
      }
    }


    [TestMethod]
    public void ReadMissingBlocks()
    {
      using (DssReader r = new DssReader(TestUtility.GetCopyForTesting("sample7.dss")))
      {
        var path = new DssPath("//SACRAMENTO/PRECIP-INC/01JAN1944/1Day/OBS/");
        var timeSeries = r.GetTimeSeries(path, new DateTime(2009, 11, 1), new DateTime(2025, 10, 31));
        Assert.AreEqual(0, timeSeries.Values.Length);
      }
    }

    [TestMethod]
    public void TryReadCondensedPathNameFail()
    {
      using (DssReader r = new DssReader(TestUtility.GetCopyForTesting("sample7.dss")))
      {
        var catalog = r.GetCatalog();
        bool result = catalog.TryGetCondensedPath(new DssPath("//SACRAMENTO/PRECIP-INC/nothing/1Day/OBS/"), out DssPathCondensed nothing);
        Assert.IsFalse(result);
      }
    }


    [TestMethod]
    public void AppendDailyRegular()
    {
      var fn = TestUtility.GetSimpleTempFileName(".dss");

      var t1 = new DateTime(1965, 1, 1);
      var ts = CreateSampleTimeSeries(t1, "cfs", "Inst-Val");
      var t2 = ts.Times[ts.Times.Length - 1].AddDays(1);
      var ts2 = CreateSampleTimeSeries(t2, "cfs", "Inst-Val");
      ts.Path = new DssPath("/dotnet/csharp/values//1Day//");
      ts2.Path = ts.Path;

      using (DssWriter w = new DssWriter(fn))
      {
        w.Write(ts);
        w.Write(ts2);
        var all = w.GetTimeSeries(ts.Path);
        Assert.AreEqual(2000, all.Count);
      }
    }

    [TestMethod]
    public void ReadWriteDailyRegular()
    {
      var fn = TestUtility.GetSimpleTempFileName(".dss");

      var t = new DateTime(1965, 1, 1);
      var ts = CreateSampleTimeSeries(t, "cfs", "Inst-Val");
      ts.Path = new DssPath("/dotnet/csharp/values//1Day//");
      ts.LocationInformation = new LocationInformation();
      ts.LocationInformation.XOrdinate = 9875.12;
      ts.LocationInformation.YOrdinate = 8764.01;
      ts.LocationInformation.ZOrdiante = 7653.90;
      ts.LocationInformation.CoordinateID = 2;
      ts.LocationInformation.CoordinateSystem = CoordinateSystem.Local;
      ts.LocationInformation.HorizontalDatum = 1;
      ts.LocationInformation.HorizontalUnits = 3;
      ts.LocationInformation.VerticalDatum = 3;
      ts.LocationInformation.VerticalUnits = 2;
      ts.LocationInformation.Supplemental = "supplemental info:Jan 26, 2023";
      ts.LocationInformation.TimeZoneName = "America/Anchorage";
      File.Delete(fn);
      using (DssWriter w = new DssWriter(fn))
      {
        w.Write(ts);
        var ts2 = w.GetTimeSeries(ts.Path, ts.StartDateTime, ts.Times[ts.Times.Length - 1]);
        Console.WriteLine(ts2.Path);
        Compare(ts, ts2);
      }
    }

    [TestMethod]
    public void ReadWriteExistingDailyRegular()
    {
      var fn = TestUtility.GetSimpleTempFileName(".dss");

      var t = new DateTime(1965, 1, 1);
      var ts = CreateSampleTimeSeries(t, "cfs", "Inst-Val");
      ts.Path = new DssPath("/dotnet/csharp/values//1Day//");

      using (DssWriter w = new DssWriter(fn))
      {
        w.Write(ts);
        t = new DateTime(1965, 1, 1);
        var ts2 = w.GetTimeSeries(ts.Path, ts.StartDateTime, ts.Times[ts.Times.Length - 1]);

        var t2 = new DateTime(1966, 1, 1);
        var ts3 = w.GetTimeSeries(ts.Path, t2, t2.AddDays(2));
        ts3.Values[0] = 1;
        ts3.Values[1] = 1;
        w.Write(ts3);

        var ts4 = w.GetTimeSeries(ts.Path, ts.StartDateTime, ts.Times[ts.Times.Length - 1]);

        Assert.AreEqual(ts4.Count, ts2.Count);

      }
    }

    [TestMethod]
    public void ReadWriteSecondRegular()
    {
      var fn = TestUtility.GetSimpleTempFileName(".dss");
      var t = new DateTime(1965, 1, 1, 1, 1, 1);
      var ts = CreateSampleTimeSeries(t, "cfs", "Inst-Val", 1);
      ts.Path = new DssPath("/dotnet/csharp/values//1Second//");


      using (DssWriter w = new DssWriter(fn))
      {
        w.Write(ts);
        var ts2 = w.GetTimeSeries(ts.Path, ts.StartDateTime, ts.Times[ts.Times.Length - 1]);
        Console.WriteLine(ts2.Path);
        Compare(ts, ts2);
        for (int i = 0; i < ts2.Count; i++)
        {
          Console.WriteLine(ts2[i].DateTime + " " + ts2[i].Value);
        }
      }
    }



    [TestMethod]
    public void ReadWriteBasicIrregular()
    {
      var fn = TestUtility.GetSimpleTempFileName(".dss");

      var t = new DateTime(1965, 1, 1);
      var ts = CreateSampleTimeSeries(t, "CFS", "PER-AVER", size: 10000);
      ts.Path = new DssPath("/dotnet/csharp/values//IR-Year//");

      using (DssWriter w = new DssWriter(fn))
      {
        w.Write(ts);
        var ts2 = w.GetTimeSeries(ts.Path, ts.StartDateTime, ts.Times[ts.Times.Length - 1]);
        Console.WriteLine(ts2.Path);
        Compare(ts, ts2);
      }
    }



    [TestMethod]
    public void ReadWriteSecondIrregular()
    {
      var fn = TestUtility.GetSimpleTempFileName(".dss");

      var t = new DateTime(1970, 2, 28);
      var ts = CreateSampleTimeSeries(t, "feet", "PER-AVER", 1);

      ts.Path = new DssPath("/dotnet/csharp/values//IR-Day//");

      using (DssWriter w = new DssWriter(fn))
      {
        w.Write(ts);
        var ts2 = w.GetTimeSeries(ts.Path, ts.StartDateTime, ts.Times[ts.Times.Length - 1]);
        Console.WriteLine(ts2.Path);
        Compare(ts, ts2);
      }
    }



    private static void Compare(TimeSeries ts, TimeSeries ts2)
    {
      Assert.AreEqual(ts.Units, ts2.Units);
      Assert.AreEqual(ts.DataType, ts2.DataType);
      for (int i = 0; i < Math.Min(ts2.Count, ts.Count); i++)
      {
        Assert.AreEqual(ts.Times[i], ts2.Times[i]);
        Assert.AreEqual(ts.Values[i], ts2.Values[i],0.01);
      }

      Assert.AreEqual(ts.LocationInformation.XOrdinate, ts2.LocationInformation.XOrdinate);
      Assert.AreEqual(ts.LocationInformation.YOrdinate, ts2.LocationInformation.YOrdinate);
      Assert.AreEqual(ts.LocationInformation.ZOrdiante, ts2.LocationInformation.ZOrdiante);
      Assert.AreEqual(ts.LocationInformation.CoordinateID, ts2.LocationInformation.CoordinateID);
      Assert.AreEqual(ts.LocationInformation.CoordinateSystem, ts2.LocationInformation.CoordinateSystem);
      Assert.AreEqual(ts.LocationInformation.HorizontalDatum, ts2.LocationInformation.HorizontalDatum);
      Assert.AreEqual(ts.LocationInformation.HorizontalUnits, ts2.LocationInformation.HorizontalUnits);
      Assert.AreEqual(ts.LocationInformation.VerticalDatum, ts2.LocationInformation.VerticalDatum);
      Assert.AreEqual(ts.LocationInformation.VerticalUnits, ts2.LocationInformation.VerticalUnits);
      Assert.AreEqual(ts.LocationInformation.Supplemental, ts2.LocationInformation.Supplemental);
      Assert.AreEqual(ts.LocationInformation.TimeZoneName, ts2.LocationInformation.TimeZoneName);
    }

    public static TimeSeries CreateSampleTimeSeries(DateTime t, string units = "", string dataType = "", int secondIncrement = 86400, int size = 1000)
    {
      var times = new List<DateTime>();
      var values = new List<double>();

      for (int i = 0; i < size; i++)
      {
        times.Add(t);
        values.Add(i + .01);
        if (secondIncrement == 86400)
          t = t.AddDays(1);
        else
          t = t.AddSeconds(secondIncrement);
      }

      var ts = new TimeSeries();
      ts.Times = times.ToArray();
      ts.Values = values.ToArray();
      ts.DataType = dataType;
      ts.Units = units;
      ts.LocationInformation = new LocationInformation();
      return ts;
    }



    /// <summary>
    /// Test reading specific range of data. 
    /// The D part (intentially out of range) should be ignored,
    /// </summary>
    [TestMethod]
    public void ReadTsIgnoreDPart()
    {
      DssPath path = new DssPath("//SACRAMENTO/TEMP-MIN/01Jan2012/1Day/OBS/");

      using DssReader r = new DssReader(TestUtility.GetCopyForTesting("sample7.dss"));
      DateTime t1 = new DateTime(1965, 1, 1);
      DateTime t2 = new DateTime(1965, 11, 1);
      var ts = r.GetTimeSeries(path, t1, t2);

      //12 Feb 1965, 24:00	36.00
      DateTime t = new DateTime(1965, 2, 13);   // next day because of 24:00

      //ts.WriteToConsole();
      int idx = ts.IndexOf(t);
      Assert.IsTrue(idx >= 0);
      Assert.AreEqual(36, ts.Values[idx], 0.001);

      //Assert.AreEqual(11, ts.Count); 
      Console.WriteLine(ts.Values.Length);
    }

    [TestMethod]
    public void ReadTsWholeDataSet_D_Part_is_Range()
    {
      DssPath path = new DssPath("//SACRAMENTO/TEMP-MIN/11Jul1877-30Jun2009/1Day/OBS/");

      using DssReader r = new DssReader(TestUtility.GetCopyForTesting("sample7.dss"));
      var ts = r.GetTimeSeries(path);

      Assert.AreEqual(48202, ts.Values.Length);

    }
    [TestMethod]
    public void ReadTsSingleRecordNoTimeWindow()
    {
      DssPath path = new DssPath("//SACRAMENTO/TEMP-MIN/01Jan1877/1Day/OBS/");
      var fn = TestUtility.GetCopyForTesting("sample7.dss");
      using DssReader r = new DssReader(fn);
      var ts = r.GetTimeSeries(path);

      Assert.AreEqual(48202, ts.Count);
      //DateTime 
      //11jul1877 24:00  60 DEG-F
      DateTime t = new DateTime(1877, 7, 12);   // next day because of 24:00

      //ts.WriteToConsole();
      int idx = ts.IndexOf(t);
      Assert.IsTrue(idx >= 0);
      Assert.IsTrue(ts.IsRegular());
      Assert.IsTrue(TimeSeries.IsRegular(ts.Times));
      Assert.AreEqual(60, ts.Values[idx], 0.001);

      Console.WriteLine(ts.Values.Length);
    }
    [TestMethod]
    public void ReadTsWithLocationInfo()
    {
      DssPath path = new DssPath("/MISSISSIPPI/ST. LOUIS/FLOW//1Day/OBS/");
      using DssReader r = new DssReader(TestUtility.GetCopyForTesting("sample7.dss"));
      DateTime t1 = DateTime.Parse("1861-01-02");
      DateTime t2 = DateTime.Parse("1861-01-18");

      var s = r.GetTimeSeries(path, t1, t2);

      var loc = s.LocationInformation;

      Console.WriteLine(loc.CoordinateID);
      Console.WriteLine(loc.XOrdinate);
      Console.WriteLine(loc.YOrdinate);
      Console.WriteLine(loc.ZOrdiante);

      Assert.AreEqual(-901047.2, loc.XOrdinate);
      Assert.AreEqual(383744.4, loc.YOrdinate);
      Assert.AreEqual(CoordinateSystem.LatLong, loc.CoordinateSystem);

      var dt = s.ToDataTable();
      //Assert.AreEqual(15, s.Count);
      Assert.AreEqual(1861, s[14].DateTime.Year);
      Assert.AreEqual(39800, s[16].Value);

      Console.WriteLine(s);


      // read location standalone.

      //var loc2 = r.GetLocationInfo("/MISSISSIPPI/ST. LOUIS/Location Info///");
      var loc2 = r.GetLocationInfo(path.FullPath);

      Console.WriteLine(loc2);

      Assert.AreEqual(-901047.2, loc2.XOrdinate);
      Assert.AreEqual(383744.4, loc2.YOrdinate);
      Assert.AreEqual(CoordinateSystem.LatLong, loc2.CoordinateSystem);


    }

    [TestMethod]
    [ExpectedException(typeof(Exception))]
    public void BogusPathTest()
    {
      DssPath path = new DssPath("/AmazonRiver/ST. LOUIS/FLOW//1Day/OBS/");
      using DssReader r = new DssReader(TestUtility.GetCopyForTesting("sample7.dss"));
      r.GetTimeSeries(path);
    }

    private void ReadSampleExtendedDataFile(string filename, out int[] times, out double[] vals, out string[] rawDates)
    {
      var t = new List<int>();
      var v = new List<double>();
      var rt = new List<string>();

      var data = File.ReadAllLines(filename);
      for (int i = 0; i < data.Length; i++)
      {// split on comma, convert to Julian
        var tokens = data[i].Split(',');
        if (tokens.Length != 2)
        {
          Console.WriteLine("Error parsing : " + data[i]);
        }
        else
        {
          var dateParts = tokens[0].Split('/');  //  1/1/-10000
          int m = Convert.ToInt32(dateParts[0]);
          int d = Convert.ToInt32(dateParts[1]);
          int y = Convert.ToInt32(dateParts[2]);
          int j = Time.YearMonthDayToJulian(y, m, d);
          t.Add(j);
          rt.Add(tokens[0]);
          var x = Convert.ToDouble(tokens[1]);
          v.Add(x);
        }
      }

      rawDates = rt.ToArray();
      times = t.ToArray();
      vals = v.ToArray();

    }

    [TestMethod]
    public void PathNotFoundIssueV7()
    {
      string fn = TestUtility.GetCopyForTesting("path_issue7.dss");
      ReadWithRange(fn);

    }

    private static void ReadWithRange(string fn)
    {
      string path = "/CHARITON RIVER/PRAIRIE HILL, MO/FLOW//15MIN/USGS/";

      DssReader r = new DssReader(fn);

      var p = new DssPath(path);
      Assert.IsTrue(r.PathExists(p));

      DateTime t1 = new DateTime(2008, 9, 2);
      DateTime t2 = new DateTime(2008, 10, 12);

      var ts = r.GetTimeSeries(p, t1, t2);
      Assert.IsTrue(ts.Count > 100);

      Console.WriteLine(ts.Count);
    }

    [TestMethod]
    public void ToDataTableWithQuality()
    {
      var pathString = "/regular-time-series/GAPT/FLOW//6Hour/forecast1/";

      DssPath path = new DssPath(pathString);
      using DssReader r = new DssReader(TestUtility.GetCopyForTesting("examples-all-data-types.dss"));
      var ts = r.GetTimeSeries(path);
      var dt = ts.ToDataTable();
      var i = (int)dt.Rows[0]["Quality"];
      Assert.AreEqual(0,i);
    }


    /// <summary>
    /// Create 10 days of data
    /// </summary>
    [TestMethod]
    public void BlockTest()
    {
      String fileName = TestUtility.GetSimpleTempFileName(".dss");
      using DssWriter dss = new DssWriter(fileName);
      DateTime t = new DateTime(2020, 1, 2);

      var ts_12Min = CreateSampleTimeSeries(t, "cfs", "Inst-VAl", 12 * 60, 14400 / 12);
      var ts_1Min = CreateSampleTimeSeries(t, "cfs", "Inst-VAl", 1 * 60, 14400 / 1);
      ts_12Min.Path = new DssPath("/test/block_12/c//12Minute/f/");
      ts_1Min.Path = new DssPath("/test/block_1/c//1Minute/f/");

      dss.Write(ts_12Min);
      dss.Write(ts_1Min);
    }

    [TestMethod]
    public void OverwriteTimeSeriesUsingTimeSeriesPoints()
    {
      DssPath path = new DssPath("//SACRAMENTO/PRECIP-INC//1Day/OBS/");
      using DssReader r = new DssReader(TestUtility.GetCopyForTesting("sample7.dss"));
      var ts = r.GetTimeSeries(path);
      var dt = ts.ToDataTable();
    }

    [TestMethod]
    public void TimeIntervalMethodTest()
    {
      DssPath path = new DssPath("/MISSISSIPPI/ST. LOUIS/FLOW//1Day/OBS/");
      using DssReader r = new DssReader(TestUtility.GetCopyForTesting("sample7.dss"));
      var s = TimeWindow.GetInterval(r.GetTimeSeries(path));
      Debug.WriteLine(s);
    }
    [TestMethod]
    public void ErrorWithZeroTimeSpan()
    {
      TimeSpan span = new TimeSpan();
      Assert.AreEqual("IR-YEAR", TimeWindow.GetInterval(span));
    }


      [TestMethod]
    public void CreateVersion7FileExplicit()
    {
      string fn = TestUtility.GetSimpleTempFileName(".dss");
      using DssWriter w = new DssWriter(fn, 7);
      Assert.IsTrue(w.GetDSSFileVersion() == 7);

    }



    [TestMethod]
    public void TestTrim()
    {
      TrimWorkout(new double[] { -902.0, -901.0, -3.402823466e+38, -3.4028234663852886E+38 }, 0, "all missing");
      TrimWorkout(new double[] { -902.0, 12.0, 14.0 }, 2, "first missing");
      TrimWorkout(new double[] { 12.0, 14.0, -901.0 }, 2, "last missing");
      TrimWorkout(new double[] { 12.0, 14.0, -901.0, 17.0, 18.0 }, 5, "missing point in middle (should not trim)");

    }
    private static void TrimWorkout(double[] values, int expectedCountAfterTrim, string message)
    {
      TimeSeries s = new TimeSeries();
      s.Values = values;
      s.Times = new DateTime[values.Length];
      DateTime t = DateTime.Now;
      for (int i = 0; i < values.Length; i++)
      {
        s.Times[i] = t.AddDays(i);
      }
      s.Trim();
      Assert.AreEqual(expectedCountAfterTrim, s.Count, message);

    }

    [TestMethod()]
    public void CheckIsRegularInterval()
    {
      DateTime t1 = new DateTime(2022, 1, 1);
      DateTime t2 = new DateTime(2022, 1, 2);
      DateTime t3 = new DateTime(2022, 1, 3);

      bool reg = TimeSeries.IsRegular(new DateTime[] { t1, t2, t3 });
      Assert.IsTrue(reg);
      reg = TimeSeries.IsRegular(new DateTime[] { t1, t3, t2 });
      Assert.IsFalse(reg);
    }


    [TestMethod]
    public void OneSecondEPart()
    {
      string dssFile = TestUtility.GetSimpleTempFileName(".dss");
      var ts = CreateSampleTimeSeries(new DateTime(2000, 4, 1),"","",1);
      ts.Path = new DssPath("/////1Second/test/");
      using DssWriter dss = new DssWriter(dssFile);
      dss.Write(ts, true);

      var ts2 = dss.GetTimeSeries(ts.Path);
      Compare(ts,ts2);
      
    }
  }
}
