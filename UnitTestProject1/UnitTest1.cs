using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            Console.WriteLine();
        }
        [TestMethod]
        public void Test()
        {
            try
            {
                int i = 0;
                Console.WriteLine(5 / i);
            }
            catch (Exception e)
            {
             // Log.Instance.Debug(e);
            }
        }
    }
}
