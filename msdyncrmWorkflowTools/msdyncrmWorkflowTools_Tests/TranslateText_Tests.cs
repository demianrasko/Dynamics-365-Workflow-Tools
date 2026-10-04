using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using msdyncrmWorkflowTools;
using System;

namespace msdyncrmWorkflowTools_Tests
{
    [TestClass]
    public class TranslateText_Tests
    {
        CrmService objService = new CrmService();

        // Live tests need a Translator resource: set TRANSLATOR_KEY (and TRANSLATOR_REGION for a regional resource).
        private static string Key => Environment.GetEnvironmentVariable("TRANSLATOR_KEY");
        private static string Region => Environment.GetEnvironmentVariable("TRANSLATOR_REGION");

        private string Translate(string text, string language)
        {
            if (string.IsNullOrEmpty(Key))
            {
                Assert.Inconclusive("Set the TRANSLATOR_KEY environment variable to run the live Translator tests.");
            }

            return new Common(objService.service).TranslateText(text, language, Key, Region);
        }

        [TestMethod]
        public void TranslateText1()
        {
            Assert.AreEqual("Olá", Translate("Hola", "pt"));
        }

        [TestMethod]
        public void TranslateText2()
        {
            Assert.AreEqual("Hello", Translate("Hola", "en"));
        }

        [TestMethod]
        public void TranslateText3()
        {
            Assert.AreEqual("Hola", Translate("Hello", "es"));
        }

        [TestMethod]
        public void TranslateText_EmptyTextReturnsEmpty()
        {
            Assert.AreEqual(string.Empty, new Common(objService.service).TranslateText(string.Empty, "es", "unused"));
        }

        [TestMethod]
        public void BuildTranslatorRequest_EscapesText()
        {
            Assert.AreEqual("[{\"Text\":\"Say \\\"hi\\\"\\n\"}]", Utility.BuildTranslatorRequest("Say \"hi\"\n"));
        }

        [TestMethod]
        public void ParseTranslatorResponse_ReadsTranslation()
        {
            var response = "[{\"detectedLanguage\":{\"language\":\"en\",\"score\":1.0},\"translations\":[{\"text\":\"Hola\",\"to\":\"es\"}]}]";
            Assert.AreEqual("Hola", Utility.ParseTranslatorResponse(response));
        }

        [TestMethod]
        public void ParseTranslatorResponse_ErrorThrowsWithServiceMessage()
        {
            try
            {
                Utility.ParseTranslatorResponse("{\"error\":{\"code\":401001,\"message\":\"The request is not authorized because credentials are missing or invalid.\"}}");
                Assert.Fail("Expected InvalidPluginExecutionException");
            }
            catch (InvalidPluginExecutionException ex)
            {
                StringAssert.Contains(ex.Message, "401001");
                StringAssert.Contains(ex.Message, "credentials are missing or invalid");
            }
        }
    }
}
