using Microsoft.VisualStudio.TestTools.UnitTesting;
using msdyncrmWorkflowTools;

namespace msdyncrmWorkflowTools_Tests
{
    [TestClass]
    public class StringFunctions_Tests
    {
        [TestMethod]
        public void StringFunctions1()
        {
            string capitalizedText = string.Empty, paddedText = string.Empty, replacedText = string.Empty, subStringText = string.Empty, regexText = string.Empty, uppercaseText = string.Empty, lowercaseText = string.Empty;
            var regexSuccess = false;
            var withoutSpaces = string.Empty;

            var test = Utility.StringFunctions(true, "Demian", "w", true, 150, true,
                "w", "w", 150, 0, true, "w",
                ref capitalizedText, ref paddedText, ref replacedText, ref subStringText, ref regexText,
                ref uppercaseText, ref lowercaseText, ref regexSuccess, ref withoutSpaces);

            Assert.AreEqual(capitalizedText, "Demian");
            Assert.AreEqual(paddedText, "wwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwDemian");
            Assert.AreEqual(replacedText, "Demian");
            Assert.AreEqual(subStringText, "Demian");
            Assert.AreEqual(regexText, string.Empty);
            Assert.AreEqual(uppercaseText, "DEMIAN");
            Assert.AreEqual(lowercaseText, "demian");
            Assert.AreEqual(regexSuccess, false);
        }
        [TestMethod]
        public void StringFunctions2()
        {
            string capitalizedText = string.Empty, paddedText = string.Empty, replacedText = string.Empty, subStringText = string.Empty, regexText = string.Empty, uppercaseText = string.Empty, lowercaseText = string.Empty;
            var regexSuccess = false;
            var withoutSpaces = string.Empty;

            var test = Utility.StringFunctions(true, "Demian", "w", true, 10, true,
                "w", "w", 150, 0, true, "w",
                ref capitalizedText, ref paddedText, ref replacedText, ref subStringText, ref regexText,
                ref uppercaseText, ref lowercaseText, ref regexSuccess, ref withoutSpaces);

            Assert.AreEqual(capitalizedText, "Demian");
            Assert.AreEqual(paddedText, "wwwwDemian");
            Assert.AreEqual(replacedText, "Demian");
            Assert.AreEqual(subStringText, "Demian");
            Assert.AreEqual(regexText, string.Empty);
            Assert.AreEqual(uppercaseText, "DEMIAN");
            Assert.AreEqual(lowercaseText, "demian");
            Assert.AreEqual(regexSuccess, false);
        }
    }
}
