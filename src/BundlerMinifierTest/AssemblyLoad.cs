using BundlerMinifier;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BundlerMinifierTest
{
    [TestClass]
    public class AssemblyLoad
    {
        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            Telemetry.Enabled = false;
        }
    }
}
