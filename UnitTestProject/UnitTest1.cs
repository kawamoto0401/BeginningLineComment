using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

using BeginningLineComment;
using System.Text.RegularExpressions;

namespace UnitTestProject
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            string str = "";

            str = UserConvert.ConvertComment("\t", "//");
            Assert.AreEqual("//\t", str);

            str = UserConvert.ConvertComment("sss\n", "//");
            Assert.AreEqual("//sss\n//", str);
  
            str = UserConvert.ConvertComment("{\n", "//");
            Assert.AreEqual("//{\n//", str);

            str = UserConvert.ConvertComment("\n", "//");
            Assert.AreEqual("//\n//", str);

            str = UserConvert.ConvertComment("\r", "//");
            Assert.AreEqual("//\r//", str);

            str = UserConvert.ConvertComment("\r\n", "//");
            Assert.AreEqual("//\r\n//", str);

            str = UserConvert.ConvertComment("A\nB\r\nC\r", "//");
            Assert.AreEqual("//A\n//B\r\n//C\r//", str);

            str = UserConvert.ConvertComment("{\r\n{\r\n", "//");
            Assert.AreEqual("//{\r\n//{\r\n//", str);

            str = UserConvert.ConvertComment("{\n{\n", "//");
            Assert.AreEqual("//{\n//{\n//", str);

            str = UserConvert.ConvertComment("{\r{\r", "//");
            Assert.AreEqual("//{\r//{\r//", str);

        }
    }
}
