using Microsoft.VisualStudio.TestTools.UnitTesting;
using msdyncrmWorkflowTools;
using System;

namespace msdyncrmWorkflowTools_IntegrationTests
{
    /// <summary>
    /// The outside services the activities call, for real: currency conversion (Frankfurter, no key), geocoding (Azure
    /// Maps, AZURE_MAPS_KEY) and translation (Azure AI Translator, TRANSLATOR_KEY and TRANSLATOR_REGION). Without a
    /// key the test is inconclusive. Save the keys with tools\Set-ServiceKeys.ps1.
    /// </summary>
    [TestClass]
    [TestCategory("LiveServices")]
    public class ExternalServices_LiveTests
    {
        [TestMethod]
        public void CurrencyConvert_ConvertsWithTodaysRate()
        {
            var dollars = Utility.CurrencyConvert(100m, "EUR", "USD");

            // a sanity range, not a rate check
            Assert.IsTrue(dollars > 50m && dollars < 200m, $"100 EUR = {dollars} USD");
            Assert.AreEqual(100m, Utility.CurrencyConvert(100m, "usd", " USD "));
        }

        [TestMethod]
        [ExpectedException(typeof(Microsoft.Xrm.Sdk.InvalidPluginExecutionException))]
        public void CurrencyConvert_UnknownCurrencyFails()
        {
            Utility.CurrencyConvert(100m, "EUR", "XYZ");
        }

        [TestMethod]
        public void GeocodeAddress_FindsAnAddressWithAzureMaps()
        {
            var key = Key("AZURE_MAPS_KEY");

            var location = Utility.GeocodeAddress("1 Microsoft Way, Redmond, WA 98052, USA ", null, key);

            Assert.IsNotNull(location);
            Assert.AreEqual(47.64m, Math.Round(location.Latitude, 2));
            Assert.AreEqual(-122.13m, Math.Round(location.Longitude, 2));
        }

        [TestMethod]
        public void TranslateText_TranslatesWithAzureTranslator()
        {
            var key = Key("TRANSLATOR_KEY");

            Assert.AreEqual("Olá", Utility.TranslateText("Hola", "pt", key, Environment.GetEnvironmentVariable("TRANSLATOR_REGION")
                ?? Environment.GetEnvironmentVariable("TRANSLATOR_REGION", EnvironmentVariableTarget.User)));
        }

        private static string Key(string variable)
        {
            var key = Environment.GetEnvironmentVariable(variable) ?? Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.User);

            if (string.IsNullOrWhiteSpace(key))
            {
                Assert.Inconclusive($"Set {variable} (tools\\Set-ServiceKeys.ps1) to run this live test.");
            }

            return key;
        }
    }
}
