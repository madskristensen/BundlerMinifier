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
            File.Delete("../../../artifacts/globbing/out1.js");
            File.Delete("../../../artifacts/globbing/out1.min.js");
            File.Delete("../../../artifacts/globbing/out2.js");
            File.Delete("../../../artifacts/globbing/out2.min.js");
            var outDirectory = "../../../artifacts/out/";
            if (Directory.Exists(outDirectory))
            {
                Directory.Delete(outDirectory, true);
            }

        }

        [TestMethod, TestCategory("Globbing")]
        public void OneFolder()
        {
            _processor.Process("../../../artifacts/globbingOneFolder.json");

            string out1 = File.ReadAllText(new FileInfo("../../../artifacts/globbing/out1.js").FullName);
            Assert.AreEqual(out1, "var a = 1;");

            string out1Min = File.ReadAllText(new FileInfo("../../../artifacts/globbing/out1.min.js").FullName);
            Assert.AreEqual(out1Min, "var a=1;");
        }

        [TestMethod, TestCategory("Globbing")]
        public void Subfolders()
        {
            _processor.Process("../../../artifacts/globbingSubFolders.json");

            string out2 = File.ReadAllText(new FileInfo("../../../artifacts/globbing/out2.js").FullName);
            Assert.AreEqual(out2, "var a = 1;\r\nvar b = 2;");

            string out2Min = File.ReadAllText(new FileInfo("../../../artifacts/globbing/out2.min.js").FullName);
            Assert.AreEqual(out2Min, "var a=1,b=2;");
        }

        [TestMethod, TestCategory("Globbing")]
        public void DontBundleOutputFile()
        {
            _processor.Process("../../../artifacts/globbingSubFolders.json");
            _processor.Process("../../../artifacts/globbingSubFolders.json");

            string out2 = File.ReadAllText(new FileInfo("../../../artifacts/globbing/out2.js").FullName);
            Assert.AreEqual(out2, "var a = 1;\r\nvar b = 2;");

            string out2Min = File.ReadAllText(new FileInfo("../../../artifacts/globbing/out2.min.js").FullName);
            Assert.AreEqual(out2Min, "var a=1,b=2;");
        }

        [TestMethod, TestCategory("Globbing")]
        public void BundleMultipleToOutputDirectory()
        {
            _processor.Process("../../../artifacts/globbingOutputDirectory.json");

            string[] outFiles = Directory.GetFiles("../../../artifacts/out/","*", SearchOption.TopDirectoryOnly);
            Assert.AreEqual(12, outFiles.Count());

            string in1 = File.ReadAllText(new FileInfo("../../../artifacts/globbing/a.js").FullName);
            string out1 = File.ReadAllText(new FileInfo("../../../artifacts/out/a.js").FullName);
            Assert.AreEqual(out1, in1);

            string out1Min = File.ReadAllText(new FileInfo("../../../artifacts/out/a.min.js").FullName);
            Assert.AreEqual("var a=1;", out1Min);

            string out2Min = File.ReadAllText(new FileInfo("../../../artifacts/out/foo.min.css").FullName);
            Assert.AreEqual("body{background:url(../test2/image.png?foo=hat)}", out2Min);
            
        }

        [TestMethod, TestCategory("Globbing")]
        public void BundleMultipleToOutputDirectoryRecursive()
        {
            _processor.Process("../../../artifacts/globbingOutputDirectory.json");

            string[] outFiles = Directory.GetFiles("../../../artifacts/out/", "*", SearchOption.AllDirectories);
            Assert.AreEqual(14, outFiles.Count());

            Assert.IsTrue(Directory.Exists("../../../artifacts/out/sub"));
            Assert.IsTrue(File.Exists("../../../artifacts/out/sub/b.js"));

            string out1Min = File.ReadAllText(new FileInfo("../../../artifacts/out/sub/b.min.js").FullName);
            Assert.AreEqual("var b=2;", out1Min);
        }


    }
}
