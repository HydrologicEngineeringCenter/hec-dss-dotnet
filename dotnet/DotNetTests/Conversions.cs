using DSSUnitTests;
using Hec.Dss;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetTests
{
   [TestClass]
   public class Conversions
   {

      [TestMethod]
      public void ConvertDSS6ToDSS7()
      {
         // DSS 6 conversion was removed from the native hecdss library (7-JA and later)
         var fn6 = TestUtility.GetCopyForTesting("sample6.dss");
         var fn7 = TestUtility.GetSimpleTempFileName(".dss");
#pragma warning disable CS0618 // ConvertFromDSS6ToDSS7 is obsolete
         int status = DssWriter.ConvertFromDSS6ToDSS7(fn6, fn7);
#pragma warning restore CS0618
         Console.WriteLine(  "Status = "+status);
         Assert.AreNotEqual(0, status);
        }

      [TestMethod]
      public void OpenDSS6GivesVersionMessage()
      {
         var fn6 = TestUtility.GetCopyForTesting("sample6.dss");
         var e = Assert.ThrowsException<Exception>(() => new DssReader(fn6));
         Assert.AreEqual("This library only supports DSS version 7 files", e.Message);
      }
   }
}
