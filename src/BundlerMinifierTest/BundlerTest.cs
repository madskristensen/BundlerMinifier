using System;
using System.IO;
using System.Linq;
using BundlerMinifier;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BundlerMinifierTest
{
    [TestClass]
    public class BundlerTest
    {
        private const string TEST_BUNDLE = "../../artifacts/test1.json";
        private BundleHandler _bundler;
        private BundleFileProcessor _processor;
        private Guid _guid;

        [TestInitialize]
        public void Setup()
        {
            _bundler = new BundleHandler();
            _processor = new BundleFileProcessor();
            _guid = Guid.NewGuid();
        }

        [TestCleanup]
        public void Cleanup()
        {
            File.Delete("../../artifacts/" + _guid + ".json");
            File.Delete("../../artifacts/foo.js");
            File.Delete("../../artifacts/foo.min.js");
            File.Delete("../../artifacts/foo.css");
            File.Delete("../../artifacts/foo.min.css");
            File.Delete("../../artifacts/foo.html");
            File.Delete("../../artifacts/foo.min.html");
        }

        [TestMethod]
        public void IsSupported()
        {
            var files1 = new[] { "file.js", "file2.js" };
            var result1 = BundleFileProcessor.IsSupported(files1);
            Assert.IsTrue(result1);

            var files2 = new[] { "file.js", "file2.css" };
            var result2 = BundleFileProcessor.IsSupported(files2);
            Assert.IsFalse(result2);

            var files3 = new[] { null, "file2.css" };
            var result3 = BundleFileProcessor.IsSupported(files3);
            Assert.IsTrue(result3);
        }

        [TestMethod]
        public void GetBundles()
        {
            var bundles = BundleHandler.GetBundles(TEST_BUNDLE);
            Assert.AreEqual(3, bundles.Count());
        }

        [TestMethod]
        public void AddBundles()
        {
            var bundle = new Bundle();
            bundle.IncludeInProject = true;
            bundle.OutputFileName = _guid + ".js";
            bundle.InputFiles.AddRange(new[] { "file1.js", "file2.js" });

            string filePath = "../../artifacts/" + _guid + ".json";
            _bundler.AddBundle(filePath, bundle);

            var bundles = BundleHandler.GetBundles(filePath);
            Assert.AreEqual(1, bundles.Count());
        }

        [TestMethod]
        public void AddBundleToExisting()
        {
            var bundle = new Bundle();
            bundle.IncludeInProject = true;
            bundle.OutputFileName = _guid + ".js";
            bundle.InputFiles.AddRange(new[] { "file1.js", "file2.js" });

            string filePath = "../../artifacts/" + _guid + ".json";
            File.Copy(TEST_BUNDLE, filePath);
            _bundler.AddBundle(filePath, bundle);

            var bundles = BundleHandler.GetBundles(filePath);
            Assert.AreEqual(4, bundles.Count());
        }

        [TestMethod]
        public void Process()
        {
            _processor.Process(TEST_BUNDLE);

            // JS
            string jsResult = File.ReadAllText("../../artifacts/foo.min.js");
            Assert.AreEqual("var file1=1,file2=2", jsResult);

            // CSS
            string cssResult = File.ReadAllText("../../artifacts/foo.min.css");
            Assert.AreEqual("body{background:#ff0}body{display:block}", cssResult);

            // HTML
            string htmlResult = File.ReadAllText("../../artifacts/foo.min.html");
            Assert.AreEqual("<div>hatæ</div><span tabindex=2> <i> hat </i> </span>", htmlResult);
        }

        [TestMethod]
        public void ProcessWithDirectory()
        {
            _processor.Process(TEST_BUNDLE.Replace("test1", "test2"));

            // JS
            string jsResult = File.ReadAllText("../../artifacts/foo.min.js");
            Assert.AreEqual("var file1=1,file2=2", jsResult);
        }

        [TestMethod]
        public void Encoding()
        {
            string jsResult = BundleMinifier.ReadAllText("../../artifacts/encoding.js");
            Assert.AreEqual("var test = 'æøå';", jsResult);
}
    }
}
