﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

[assembly: InternalsVisibleTo("DotNetTests")]
namespace Hec.Dss
{
  public class DssReader : IDisposable
  {

    private const double UNDEFINED_DOUBLE = -3.402823466e+38;
    private const double UNDEFINED_DOUBLE_2 = -3.4028234663852886E+38;

    protected long[] ifltab = new long[250];
    protected string filename;


    protected int versionNumber;

    DssPathCollection _catalog = null;

    public string Filename
    {
      get { return filename; }
      private set { filename = value; }
    }

    /// <summary>
    /// Empty constructor for DSSWRITER to inherit
    /// </summary>
    protected DssReader()
    {
    }

    protected IntPtr dss;
    /// <summary>
    /// Constructor for DssReader 
    /// </summary>
    /// <param name="filename">Location of DSS file</param>
    public DssReader(string filename,int messageLevel=3 )
    {
      OpenDssFile(filename,messageLevel);
    }
    /// <summary>
    /// returns DSS version 
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns>version or zero on error/invalid file</returns>
    public static int GetVersion(string fileName)
    {
      if (!File.Exists(fileName))
        return 0;
      return DssNative.hec_dss_getFileVersion(fileName);
    }
    
    private void OpenDssFile(string filename, int messageLevel)
    {
      DssGlobals.SetMessageLevel(messageLevel);
      int status = DssNative.hec_dss_open(filename,out dss);

      this.filename = filename;

      switch (status)
      {
        case 0:
            //no problem
            versionNumber = GetDSSFileVersion();
            break;

        case -1:
          throw new Exception("Unable to create the DSS file.");

        case -2:
          throw new Exception("Unable to connect to the DSS file.");

        case -3:
          throw new Exception("Incompatible DSS version.");

        case -10:
          throw new Exception("No filename provided.");
        case -700:
          throw new Exception("This library only supports DSS version 7 files");

        default:
          throw new Exception("Error opening DSS file.");
      }
    }

    /// <summary>
    /// Gives condensed path names of the DSS File.  Condensed means that for every path name that has equal everything but D part, merge it into one path. 
    /// Will always give the path names sorted.
    /// </summary>
    /// <returns></returns>
    public DssPathCollection GetCatalog()
    {
      if (_catalog == null)
      {
        _catalog = GetCatalogInternal();
      }

      return _catalog;

    }

    private DssPathCollection GetCatalogInternal()
    {

      var rawCatalog = GetRawCatalog(dss, out int[] intRecordTypes);
      RecordType[] recordTypes = null;

      recordTypes = ConvertToRecordType(intRecordTypes);

      var condensed = new DssPathCollection(filename, rawCatalog.ToArray(), recordTypes, condensed: true);

      return condensed;
    }

    internal static List<string> GetRawCatalog(IntPtr dss,out int[] recordTypes, string filter="")
    {
      int count = DssNative.hec_dss_record_count(dss);
      int pathBufferSize = DssNative.hec_dss_CONSTANT_MAX_PATH_SIZE();

      byte[] rawCatalog = new byte[count * pathBufferSize];
      recordTypes = new int[count];

      List<string> pathNameList = new List<string>(count);
      byte[] byteFilter = new byte[] { 0 };
      if( filter!= "")
      {
        byteFilter = Encoding.ASCII.GetBytes(filter); 
      }
        var numRecords = DssNative.hec_dss_catalog(dss, rawCatalog, recordTypes, byteFilter, count, pathBufferSize);
        for (int i = 0; i < numRecords; i++)
        {
          int start = i * pathBufferSize;
          var end = System.Array.IndexOf(rawCatalog, (byte)0, start); // get rid of trailing \0\0
          int size = Math.Min(end - start, pathBufferSize);
          pathNameList.Add(Encoding.ASCII.GetString(rawCatalog, start, size));
        }

      return pathNameList;
    }

    private static RecordType[] ConvertToRecordType(int[] recTypes)
    {
      var rval = new RecordType[recTypes.Length];
      for (int i = 0; i < recTypes.Length; i++)
      {
        rval[i] = RecordTypeFromInt(recTypes[i]);
      }
      return rval;
    }

    private static RecordType RecordTypeFromInt(int recType)
    {
      RecordType rval = RecordType.Unknown;

      if (recType >= 100 && recType < 110)
      { // see zdataTypeDescriptions.h
        if (recType == 102 || recType == 107)
          rval = RecordType.RegularTimeSeriesProfile;
        else
        {
          rval = RecordType.RegularTimeSeries;
        }
      }
      else if (recType >= 110 && recType < 200)
        rval = RecordType.IrregularTimeSeries;
      else if (recType >= 200 && recType < 300)
        rval = RecordType.PairedData;
      else if (recType >= 300 && recType < 400)
        rval = RecordType.Text;
      else if (recType >= 400 && recType < 450)
        rval = RecordType.Grid;
      else if (recType == 450)
        rval = RecordType.Tin;
      else if (recType == 20)
        rval = RecordType.LocationInfo;

      return rval;
    }



    /// <summary>
    /// Returns true if the value is valid
    /// and not flagged missing or undefined.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool IsValid(double value)
    {
      return value != UNDEFINED_DOUBLE && value != UNDEFINED_DOUBLE_2
        && !IsMissing(value);
    }

    /// <summary>
    /// Returns true if the value represents 'missing'
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool IsMissing(double value)
    {
      return value == -901.0
      || value == -902.0;
    }

    public RecordType GetRecordType(DssPath path)
    {
      //Try to get the record type the fast way, which is just assuming the path exists.
      string pathName = path.FullPath;
      if (path is DssPathCondensed)
      {
        pathName = (path as DssPathCondensed).GetComprisingDSSPaths()[0].FullPath;
      }

      int rtInt = DssNative.hec_dss_dataType(dss, path.FullPath);

      if (rtInt <=0)
      {
        //Try again, but slower version.

        var catalog = GetCatalog();
        var cmp = new DssPath.DatelessComparer();
        pathName = catalog.Where(p => cmp.Equals(p, path)).First().PathWithoutRange;
        rtInt = DssNative.hec_dss_dataType(dss, path.FullPath);
      }

      return RecordTypeFromInt(rtInt);
    }

    
    /// <summary>
    /// GetTimeSeries reads time series data.
    /// if the time windows (startDateTime, endDateTime) are not defined the whole data set is returned
    /// </summary>
    /// <param name="dssPath"></param>
    /// <param name="compression"></param>
    /// <param name="startDateTime"></param>
    /// <param name="endDateTime"></param>
    /// <param name="PreReadCatalog"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private TimeSeries GetTimeSeries(DssPath dssPath, TimeWindow.ConsecutiveValueCompression compression, DateTime startDateTime = default(DateTime), DateTime endDateTime = default(DateTime), bool PreReadCatalog = true)
    {
      ValidateTsPathExists(dssPath, PreReadCatalog);

      bool datesProvided = startDateTime != default(DateTime) && endDateTime != default(DateTime);


      Hec.Dss.Time.DateTimeToHecDateTime(startDateTime, out string startDate, out string startTime);
      Hec.Dss.Time.DateTimeToHecDateTime(endDateTime, out string endDate, out string endTime);

      if (!datesProvided ) { // return whole data-set
        const int boolFullSet = 1;
        int firstValidJulian = 0, firstSeconds = 0, lastValidJulian = 0, lastSeconds = 0;
        DssNative.hec_dss_tsGetDateTimeRange(dss, dssPath.FullPath, boolFullSet,
            ref firstValidJulian, ref firstSeconds, ref lastValidJulian, ref lastSeconds);
        Time.JulianToHecDateTime(firstValidJulian, firstSeconds, out startDate, out startTime);
        Time.JulianToHecDateTime(lastValidJulian, lastSeconds, out endDate, out endTime);

      }
      int numberValues = 0;
      int qualityElementSize=0;
      // find array size needed.
      int status = DssNative.hec_dss_tsGetSizes(this.dss, dssPath.PathWithoutDate, startDate, startTime,
              endDate, endTime, ref numberValues, ref qualityElementSize);

      
      
      int[] times = new int [numberValues];
      double[] values = new double[numberValues];
      int qualityLength = numberValues * qualityElementSize;
      var quality = new int[qualityLength];
      int numberValuesRead = 0;
      int julianBaseDate = 0, timeGranularitySeconds = 60;
      ByteString units = new ByteString(64);
      ByteString type = new ByteString(64);

      status = DssNative.hec_dss_tsRetrieve(dss, dssPath.PathWithoutRange, 
          startDate, startTime, endDate, endTime, times, values, numberValues,
         ref numberValuesRead, quality, qualityLength, ref julianBaseDate, ref timeGranularitySeconds,
         units.Data, units.Length, type.Data, type.Length);

      TimeSeries ts = new TimeSeries();
      
      if (status == 0)
      {
        /* LATER: change TimeSeries.Times and TimeSeries.Values to  Memory<int> and Memory<double>
         * This can avoid the possible need to resize.
        var mt = new System.Memory<int>(times);
        var t = mt.Slice(0, numberValuesRead);
        var x = t.Span;
        var ta = x[3];
        */
        Array.Resize(ref times,numberValuesRead);
        ts.Times = Time.DateTimesFromJulianArray(times, timeGranularitySeconds, julianBaseDate);
        Array.Resize(ref values, numberValuesRead);
        if (qualityLength > 0)
          ts.Qualities = quality;
        ts.Values = values;
        ts.DataType = type.ToString();
        ts.Units = units.ToString();
        ts.Path = new DssPath(dssPath.FullPath);

        ts.LocationInformation = GetLocationInfo(dssPath.FullPath);

        if (compression != TimeWindow.ConsecutiveValueCompression.None)
          ts = ts.Compress(compression);
      }
      else {
        return GetEmptyTimeSeries(dssPath);
      }


      if (dssPath.IsRegular) // only applies to regular interval
        ts.Trim();

      return ts;

    }

    private void ValidateTsPathExists(DssPath dssPath, bool PreReadCatalog)
    {
      if (PreReadCatalog)
      {
        if (!PathExists(dssPath))
        {
          throw new Exception("Error: Path does not exist '" + dssPath.FullPath + "'");
        }
        RecordType rt = GetRecordType(dssPath);

        if (rt != RecordType.RegularTimeSeries && rt != RecordType.IrregularTimeSeries)
        {
          throw new Exception("Error reading path " + dssPath.FullPath + ".  This path is not a time series");
        }
      }
    }

    /// <summary>
    /// Gets a TimeSeries from the DSS file.  Can be either irregular or regular time series data.  
    /// data is returned as doubles
    /// If there is a problem, values and times will be null.
    /// if the time windows (startDateTime, endDateTime) are not defined the whole data set is returned
    /// </summary>
    ///  <param name="dssPath"></param>
    ///  <param name="startDateTime">starting DateTime (optional overrides path D part)</param>
    /// <param name="endDateTime">ending DateTime (optional overrides path D part)</param>
    /// <param name="behavior"></param>
    /// <returns></returns>
    public TimeSeries GetTimeSeries(DssPath dssPath, DateTime startDateTime = default(DateTime), 
      DateTime endDateTime = default(DateTime), TimeWindow.TimeWindowBehavior behavior = TimeWindow.TimeWindowBehavior.StrictlyInclusive,
      TimeWindow.ConsecutiveValueCompression compression = TimeWindow.ConsecutiveValueCompression.None)
    {
      if(startDateTime > endDateTime)
      {
        return GetEmptyTimeSeries(dssPath);
      }


      if (behavior == TimeWindow.TimeWindowBehavior.StrictlyInclusive)
        return GetTimeSeries(dssPath, compression, startDateTime, endDateTime);
      else if (behavior == TimeWindow.TimeWindowBehavior.Cover)
      {
        if (startDateTime == default(DateTime) && endDateTime == default(DateTime))
          return GetTimeSeries(dssPath, compression, startDateTime, endDateTime);
        if (dssPath.IsRegular) // time series is regular
        {
          DateTime t1 = startDateTime.AddSeconds(-TimeWindow.SecondsInInterval(dssPath));
          DateTime t2 = endDateTime.AddSeconds(TimeWindow.SecondsInInterval(dssPath));
          var ts = GetTimeSeries(dssPath, compression, t1, t2);
          ts = TimeWindow.Trim(ts, startDateTime, endDateTime);
          return ts;
        }
        else // time series is irregular
        { // add 10% buffer
          TimeSpan diff = endDateTime - startDateTime;
          DateTime t1 = startDateTime.AddHours(-diff.TotalHours*0.1);
          DateTime t2 = endDateTime.AddHours(diff.TotalMinutes*0.1);
          var ts = GetTimeSeries(dssPath, compression, t1, t2);
          return ts;
        }
      }

      return GetEmptyTimeSeries(dssPath);
    }

    /// <summary>
    /// GetEmptyTimeSeries returns a time series without the data
    /// used to lookup the units, and data type of a series.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public TimeSeries GetEmptyTimeSeries(DssPath path)
    {

       if (path is DssPathCondensed)
         {
          var parts = ((DssPathCondensed)path).GetComprisingDSSPaths();
          path = parts[0];
         }
      int size = 64;
      ByteString units = new ByteString(size);
      ByteString type = new ByteString(size);

      int status = DssNative.hec_dss_tsRetrieveInfo(dss, path.FullPath, units.Data, size, type.Data, size);

      var rval = new TimeSeries();
      if (status != 0)
      {
        return rval;
      }
      rval.Path = path;
      rval.Units = units.ToString();
      rval.DataType = type.ToString();

      //TODO location info
     // var locationInfo = new LocationInformation(tss.locationStruct);
      //rval.LocationInformation = locationInfo;
      return rval;
    }

    /// <summary>
    /// Gets the double values and ordinates from the paired data at the given path name. 
    /// If there is a problem, ordinates and values will be null.
    /// </summary>
    /// <param name="pathname">pathname for the paired data</param>
    public PairedData GetPairedData(string pathname, bool metaDataOnly=false)
    {
      int numberOrdinates = 0;
      int numberCurves = 0;
      int labelsLength = 0;
      int size = 128;
      int status;
      ByteString unitsIndependent = new ByteString(size);
      ByteString unitsDependent = new ByteString(size);
      ByteString typeIndependent = new ByteString(size);
      ByteString typeDependent = new ByteString(size);

      var rval = new PairedData();

      status = DssNative.hec_dss_pdRetrieveInfo(dss, pathname, ref numberOrdinates, ref numberCurves,
        unitsIndependent.Data, unitsIndependent.Data.Length,
        unitsDependent.Data, unitsDependent.Data.Length,
        typeIndependent.Data, typeIndependent.Data.Length,
        typeDependent.Data, typeDependent.Data.Length,
        ref labelsLength);

      if (status != 0 || numberOrdinates<=0 || numberCurves<=0 )
        return rval;

      rval.UnitsIndependent = unitsIndependent.ToString();
      rval.UnitsDependent = unitsDependent.ToString();  
      rval.TypeDependent = typeDependent.ToString(); 
      rval.TypeIndependent = typeIndependent.ToString() ;

      if (metaDataOnly)
        return rval;

      double[] Ordinates = new double[numberOrdinates];
      double[] Values = new double[numberOrdinates*numberCurves];
      ByteString labels = new ByteString(labelsLength);

      status = DssNative.hec_dss_pdRetrieve(dss, pathname, Ordinates, Ordinates.Length,
        Values, Values.Length, ref numberOrdinates, ref numberCurves,
         unitsIndependent.Data, unitsIndependent.Data.Length,
        unitsDependent.Data, unitsDependent.Data.Length,
        typeIndependent.Data, typeIndependent.Data.Length,
        typeDependent.Data, typeDependent.Data.Length,
        labels.Data, labels.Data.Length);

      if (status != 0)
      {
        return rval;
      }

      rval.Path = new DssPath(pathname);
      rval.Ordinates = Ordinates;

      
      int k = 0;
      rval.Values = new List<double[]>();
      for (int col = 0; col < numberCurves; col++)
      {
        var curve = new double[numberOrdinates];
        for (int row = 0; row < curve.Length; row++)
        {
          curve[row] = Values[k++];
        }
        rval.Values.Add(curve);
      }
      List<String> titles = new List<String>();
      titles.AddRange(labels.ToStringArray());
      rval.Labels = titles;

      rval.LocationInformation = GetLocationInfo(pathname);
      return rval;
    }

    public TimeSeriesProfile GetTimeSeriesProfile(string pathname)
    {
      return GetTimeSeriesProfile(new DssPath(pathname));
    }
    public TimeSeriesProfile GetTimeSeriesProfile(DssPath pathname, DateTime startDateTime=default, DateTime endDateTime=default)
    {
      throw new NotImplementedException("");
/*      var rval = new TimeSeriesProfile();
      ZStructTimeSeriesWrapper tss = CreateTSWrapper(pathname,startDateTime, endDateTime);

      int retrieveDoublesFlag = 2; // get doubles
      int retrieveFlagTrim = -1;
      int boolRetrieveQualityNotes = 0;
      int status = DSS.ZTsRetrieve(ref ifltab, ref tss, retrieveFlagTrim, retrieveDoublesFlag, boolRetrieveQualityNotes);
      if (status != 0)
      {
        throw new Exception("Error reading path " + pathname + " in GetTimeSeriesProfile(" + pathname + ")");
      }

      rval.ColumnValues = tss.DoubleProfileDepths;
      rval.Times = Time.DateTimesFromJulianArray(tss.Times,tss.TimeGranularitySeconds,tss.JulianBaseDate);
      rval.Values = DoubleArrayToMatrix(tss.DoubleProfileValues, rval.ColumnValues.Length, tss.NumberValues);
      return rval;
*/
    }

    private static double[,] DoubleArrayToMatrix(double[] values, int numCols, int numRows)
    {
      double[,] data = new double[numRows, numCols];
      int counter = 0;
      for (int i = 0; i < data.GetLength(0); i++)
      {
        for (int j = 0; j < numCols; j++)
        {
          data[i, j] = values[counter++];
        }
      }

      return data;
    }



    public bool PathExists(string pathname, DateTime startDateTime = default(DateTime), DateTime endDateTime = default(DateTime))
    {
      return PathExists(new DssPath(pathname), startDateTime, endDateTime);
    }
    /// <summary>
    /// Returns true if a path exists.
    /// Ignore D part when startDateTime and endDateTime are provided
    /// if the optional, date range  arguments, are not omitted exact mathching is performed.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="startDateTime"></param>
    /// <param name="endDateTime"></param>
    /// <returns></returns>
    public bool PathExists(DssPath path, DateTime startDateTime = default(DateTime), DateTime endDateTime = default(DateTime))
    {

      bool datesProvided = (startDateTime != default(DateTime) && endDateTime != default(DateTime));

      int count = 0;

      var catalog = GetCatalog();

      var cmp = new DssPath.DatelessComparer();
      if (!datesProvided)
      {
        count = catalog.Count(p => cmp.Equals(p, path));
      }
      else
      {
        var x = catalog.Count(p => cmp.Equals(p, path));

        count = catalog.Count(p => cmp.Equals(p, path)
              && (p is DssPathCondensed
                       && Time.ConvertFromHecDateTime((p as DssPathCondensed).DPartStart) >= startDateTime
                       && Time.ConvertFromHecDateTime((p as DssPathCondensed).DPartEnd) <= endDateTime));
      }

      return count > 0;

    }


    /// <summary>
    /// Checks if the path exists in the DSS file.
    /// exact match required
    /// </summary>
    /// <param name="path"></param>
    /// <returns>True if the path exists, false otherwise.</returns>
    public bool ExactPathExists(DssPath path)
    {
      var p = GetCatalog().FindExactPath(path.FullPath);
      if(p == DssPath.NotFound )
        return false;

      return true;
    }

    /// <summary>
    /// Gives you the value used for flagging missing values in time series data.  Compatible with DSS 6 and 7
    /// </summary>
    /// <returns>The missing flag value</returns>
    public float CheckMissingFlag()
    {
      return -3.402823466e+38F;
    }

    /// <summary>
    /// Will tell you the file version of the currently opened DSS file.
    /// </summary>
    /// <returns>6 or 7 depending on the version</returns>
    public int GetDSSFileVersion()
    {
      return DssNative.hec_dss_getVersion(dss);
    }



    /// <summary>
    /// Retrieves location Info data
    /// </summary>
    /// <param name="pathname">pathname to the time series in DSS.</param>
    /// <returns>If successful, the location data in the pathname.  Otherwise returns empty LocationData
    public LocationInformation GetLocationInfo(string pathname)
    {
      double x = 0.0, y = 0.0, z = 0.0;
      int coordinateSystem=0, coordinateID=0,horizontalUnits =0, horizontalDatum=0, verticalUnits=0, verticalDatum= 0;
      ByteString timeZoneName = new ByteString(128);
      ByteString supplemental = new ByteString(5000); // TO DO... how big?
      var locStatus = DssNative.hec_dss_locationRetrieve(dss, pathname, ref x, ref y, ref z,
                                                      ref coordinateSystem, ref coordinateID,
                                                      ref horizontalUnits, ref horizontalDatum,
                                                      ref verticalUnits, ref verticalDatum,
                                                      timeZoneName.Data, timeZoneName.Data.Length,
                                                      supplemental.Data, supplemental.Data.Length);

      var rval = new LocationInformation();
      if (locStatus != 0)
        return rval;

      rval.XOrdinate = x;
      rval.YOrdinate = y;
      rval.ZOrdiante = z; 
      rval.CoordinateSystem = (CoordinateSystem)coordinateSystem;
      rval.CoordinateID = coordinateID; 
      rval.HorizontalUnits = horizontalUnits;
      rval.HorizontalDatum = horizontalDatum; 
      rval.VerticalUnits = verticalUnits;
      rval.VerticalDatum = verticalDatum;
      rval.TimeZoneName = timeZoneName.ToString();
      rval.Supplemental = supplemental.ToString();
      return rval;
    }


    /// <summary>
    /// Gives you a grid object with a specified path name in the DSS file
    /// </summary>
    /// <param name="pathname">pathname to the grid</param>
    /// <param name="retrieveData">Whether to retrieve data or not</param>
    /// <returns>Returns an albers grid if the grid is albers, a specified grid if its a specified grid, or a generic grid otherwise.  Returns null if there is a problem.</returns>
    public Grid GetGrid(string pathname, bool retrieveData)
    {
      int type=0;
      int dataType = 0;
      int lowerLeftCellX = 0;
      int lowerLeftCellY = 0;
      int numberOfCellsX = 0;
      int numberOfCellsY = 0;
      int numberOfRanges = 0;
      int srsDefinitionType = 0;
      int timeZoneRawOffset = 0;
      int isInterval = 0;
      int isTimeStamped = 0;
      ByteString dataUnits = new ByteString(64);
      ByteString dataSource = new ByteString(128); 
      ByteString srsName = new ByteString(64);
      ByteString srsDefinition = new ByteString(1024*8);
      ByteString timeZoneID = new ByteString(64);
      float cellSize = 0;
      float xCoordOfGridCellZero = 0;
      float yCoordOfGridCellZero = 0;
      float nullValue = 0;
      float maxDataValue = 0;
      float minDataValue = 0;
      float meanDataValue = 0;
      float[] rangeLimitTable = new float[0];
      int[] numberEqualOrExceedingRangeLimit = new int[0];
      float[] data= new float[0];
      // First read to find out the sizes 
      int metaDataOnly = 0;
      
      DssNative.hec_dss_gridRetrieve(dss, pathname, metaDataOnly, ref type, ref dataType,
      ref lowerLeftCellX, ref lowerLeftCellY,
      ref numberOfCellsX, ref numberOfCellsY,
      ref numberOfRanges, ref srsDefinitionType,
      ref timeZoneRawOffset, ref isInterval,
      ref isTimeStamped,
      dataUnits.Data, dataUnits.Length,
      dataSource.Data, dataSource.Length,
      srsName.Data, srsName.Length,
      srsDefinition.Data, srsDefinition.Length,
      timeZoneID.Data, timeZoneID.Length,
      ref cellSize, ref xCoordOfGridCellZero,
      ref yCoordOfGridCellZero, ref nullValue,
      ref maxDataValue, ref minDataValue,
      ref meanDataValue,
      rangeLimitTable, rangeLimitTable.Length,
      numberEqualOrExceedingRangeLimit,
      data, data.Length);


      // Read again with data Arrays allocated.
      int readData = 1;
      rangeLimitTable = new float[numberOfRanges];
      numberEqualOrExceedingRangeLimit = new int[numberOfRanges];
      data = new float[numberOfCellsX* numberOfCellsY];

      DssNative.hec_dss_gridRetrieve(dss, pathname, readData, ref type, ref dataType,
      ref lowerLeftCellX, ref lowerLeftCellY,
      ref numberOfCellsX, ref numberOfCellsY,
      ref numberOfRanges, ref srsDefinitionType,
      ref timeZoneRawOffset, ref isInterval,
      ref isTimeStamped,
      dataUnits.Data, dataUnits.Length,
      dataSource.Data, dataSource.Length,
      srsName.Data, srsName.Length,
      srsDefinition.Data, srsDefinition.Length,
      timeZoneID.Data, timeZoneID.Length,
      ref cellSize, ref xCoordOfGridCellZero,
      ref yCoordOfGridCellZero, ref nullValue,
      ref maxDataValue, ref minDataValue,
      ref meanDataValue,
      rangeLimitTable, rangeLimitTable.Length,
      numberEqualOrExceedingRangeLimit,
      data, data.Length);
      
      // construct Grid
      var rval = new Grid();
      rval.PathName = pathname;
      rval.DataType = (DssDataType)dataType; //DssDataType.INST_VAL;
      rval.GridType = (GridType)type; //GridType.ALBERS;

      rval.LowerLeftCellX = lowerLeftCellX;
      rval.LowerLeftCellY = lowerLeftCellY;
      rval.NumberOfCellsX = numberOfCellsX;
      rval.NumberOfCellsY = numberOfCellsY;
      rval.SRSDefinitionType = srsDefinitionType;
      rval.TimeZoneRawOffset = timeZoneRawOffset;
      rval.IsInterval = isInterval == 1;
      rval.IsTimeStamped = isTimeStamped == 1;
      rval.Units = dataUnits.ToString();
      rval.DataSource = dataSource.ToString();
      rval.SRSName = srsName.ToString();
      rval.SRSDefinition = srsDefinition.ToString();
      rval.TimeZoneID = timeZoneID.ToString();
      rval.CellSize = cellSize;
      rval.XCoordOfGridCellZero = xCoordOfGridCellZero;
      rval.YCoordOfGridCellZero = yCoordOfGridCellZero;
      rval.NullValue = nullValue;
      rval.MaxDataValue = maxDataValue;
      rval.MinDataValue = minDataValue;
      rval.MeanDataValue = meanDataValue;
      rval.RangeLimitTable = rangeLimitTable;
      rval.NumberEqualOrExceedingRangeLimit = numberEqualOrExceedingRangeLimit;
      rval.Data = data;

      return rval;

    }

    /// <summary>
    /// Gives you a grid object with a specified path name in the DSS file
    /// </summary>
    /// <param name="path">pathname to the grid</param>
    /// <param name="retrieveData">Whether to retrieve data or not</param>
    /// <returns>Returns an albers grid if the grid is albers, a specified grid if its a specified grid, or a generic grid otherwise.  Returns null if there is a problem.</returns>
    public Grid GetGrid(DssPath path, bool retrieveData)
    {
      return GetGrid(path.FullPath, retrieveData);
    }

    public void Dispose()
    {
      DssNative.hec_dss_close(dss);
    }

  }

}

