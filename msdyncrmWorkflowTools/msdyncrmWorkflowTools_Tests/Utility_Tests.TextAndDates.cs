using Microsoft.VisualStudio.TestTools.UnitTesting;
using msdyncrmWorkflowTools;

namespace msdyncrmWorkflowTools_Tests
{
    public partial class Utility_Tests
    {
        [TestMethod]
        public void ExpandEscapes_TurnsBackslashCodesIntoCharacters()
        {
            Assert.AreEqual("a\nb\tc\rd", Utility.ExpandEscapes(@"a\nb\tc\rd"));
            Assert.AreEqual(@"keep \ and \x", Utility.ExpandEscapes(@"keep \\ and \x"));
            Assert.AreEqual(", ", Utility.ExpandEscapes(", "));
            Assert.IsNull(Utility.ExpandEscapes(null));
        }
    }
}
