using System;
using System.IO;
using System.Linq;
using BundlerMinifier;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BundlerMinifierTest
{
    [TestClass]
    public class EncodingTest
    {
        private const string TEST_BUNDLE = "../../artifacts/test1.json";
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
            File.Delete("../../artifacts/" + _guid + ".json");
            File.Delete("../../artifacts/foo.js");
            File.Delete("../../artifacts/foo.min.js");
            File.Delete("../../artifacts/foo.min.js.map");
            File.Delete("../../artifacts/foo.css");
            File.Delete("../../artifacts/foo.min.css");
            File.Delete("../../artifacts/foo.html");
            File.Delete("../../artifacts/foo.min.html");
            File.Delete("../../artifacts/encoding/encoding.js");
            File.Delete("../../artifacts/encoding/encoding.min.js");
        }

        [TestMethod, TestCategory("Encoding")]
        public void ProcessWithDifferentEncoding()
        {
            _processor.Process("../../artifacts/encoding/encoding.json");

            string jsResult = File.ReadAllText("../../artifacts/encoding/encoding.js");
            Assert.AreEqual("var bom = 'àèéèùì';\r\nvar nobom = 'àèéèùì'", jsResult);
        }

        [TestMethod, TestCategory("Encoding")]
        public void Encoding()
        {
            string jsResult = FileHelpers.ReadAllText("../../artifacts/encoding.js");
            Assert.AreEqual("var test = 'æøå';", jsResult);
        }
    }
}
