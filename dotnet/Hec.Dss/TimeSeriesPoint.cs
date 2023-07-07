using System;
using System.Collections.Generic;

namespace Hec.Dss
{
  public class TimeSeriesPoint
  {
    public int Quality { get; set; }

    public TimeSeriesPoint(DateTime dateTime, double value, int quality=-1)
    {
      DateTime = dateTime;
      Value = value;
      Quality = quality;
    }

    public override string ToString()
    {
      Time.DateTimeToHecDateTime(this.DateTime, out string d, out string t);
      return d + " " + t + ", " + this.Value.ToString("F2");
    }

    public DateTime DateTime { get; set; }

    public double Value { get; set; }

    

  }
}
