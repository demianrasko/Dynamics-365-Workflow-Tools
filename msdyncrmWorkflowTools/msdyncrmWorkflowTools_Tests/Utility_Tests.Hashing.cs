using Microsoft.VisualStudio.TestTools.UnitTesting;
using msdyncrmWorkflowTools;

namespace msdyncrmWorkflowTools_Tests
{
    public partial class Utility_Tests
    {
        [TestMethod]
        public void Md5Hash_MatchesTheStandardTestVector()
        {
            Assert.AreEqual("900150983cd24fb0d6963f7d28e17f72", Utility.Md5Hash("abc"));
            Assert.AreEqual("d41d8cd98f00b204e9800998ecf8427e", Utility.Md5Hash(string.Empty));
        }

        [TestMethod]
        public void Sha512Hash_MatchesTheStandardTestVector()
        {
            Assert.AreEqual(
                "ddaf35a193617abacc417349ae20413112e6fa4e89a97ea20a9eeee64b55d39a2192992a274fc1a836ba3c23a3feebbd454d4423643ce80e2a9ac94fa54ca49f",
                Utility.Sha512Hash("abc"));
        }
    }
}
