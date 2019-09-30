using Microsoft.VisualStudio.TestTools.UnitTesting;
using DefenceAligner;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var edb = new Excel2DB(@"C:\Users\user\Desktop\test.db3");
            edb.ReadExcel(@"C:\Users\user\Desktop\test.xls");
            edb.ReadExcel(@"C:\Users\user\Desktop\test2.xlsx");
            edb.Close();
        }
    }
}
