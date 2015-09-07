using System;
using System.IO;
using System.Linq;
using BundlerMinifier;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BundlerMinifierTest
{
    [TestClass]
    public class GlobbingTest
    {
        private BundleFileProcessor _processor;
        private Guid _guid;

        [TestInitialize]
        public void Setup()
        {
            _processor = new BundleFileProcessor();
            _guid = Guid.NewGuid();
        }

        [TestCleanup]
        public void Cleanup()
        {
            File.Delete("../../artifacts/globbing/out1.js");
            File.Delete("../../artifacts/globbing/out1.min.js");
            File.Delete("../../artifacts/globbing/out2.js");
            File.Delete("../../artifacts/globbing/out2.min.js");
        }

        [TestMethod]
        public void OneFolder()
        {
            _processor.Process("../../artifacts/globbingOneFolder.json");

            string out1 = File.ReadAllText(new FileInfo("../../artifacts/globbing/out1.js").FullName);
            Assert.AreEqual(out1, "var a = 1;");

            string out1Min = File.ReadAllText(new FileInfo("../../artifacts/globbing/out1.min.js").FullName);
            Assert.AreEqual(out1Min, "var a=1");
        }

        [TestMethod]
        public void Subfolders()
        {
            _processor.Process("../../artifacts/globbingSubFolders.json");

            string out2 = File.ReadAllText(new FileInfo("../../artifacts/globbing/out2.js").FullName);
            Assert.AreEqual(out2, "var a = 1;\r\nvar b = 2;");

            string out2Min = File.ReadAllText(new FileInfo("../../artifacts/globbing/out2.min.js").FullName);
            Assert.AreEqual(out2Min, "var a=1,b=2");
        }
    }
}
