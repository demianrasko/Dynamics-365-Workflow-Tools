using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using msdyncrmWorkflowTools;

namespace msdyncrmWorkflowTools_Tests
{
    public partial class Utility_Tests
    {
        [TestMethod]
        public void GeocodeUrls_EscapeTheAddressAndUseHttps()
        {
            var bing = Utility.BuildBingGeocodeUrl(" 1 Main St #200 & Co/4 ", "bing key");
            var azure = Utility.BuildAzureMapsGeocodeUrl("1 Main St #200", "azure-key");

            Assert.AreEqual("https://dev.virtualearth.net/REST/v1/Locations?maxResults=1&query=1%20Main%20St%20%23200%20%26%20Co%2F4&key=bing%20key", bing);
            Assert.AreEqual("https://atlas.microsoft.com/geocode?api-version=2023-06-01&top=1&query=1%20Main%20St%20%23200&subscription-key=azure-key", azure);
        }

        [TestMethod]
        public void ParseBingGeocodeResponse_ReadsTheFirstGeocodePoint()
        {
            const string json = "{\"statusCode\":200,\"resourceSets\":[{\"estimatedTotal\":1,\"resources\":[{\"point\":{\"coordinates\":[1,2]},\"geocodePoints\":[{\"coordinates\":[47.640068,-122.129858]}]}]}]}";

            var location = Utility.ParseBingGeocodeResponse(json);

            Assert.AreEqual(47.640068m, location.Latitude);
            Assert.AreEqual(-122.129858m, location.Longitude);
        }

        [TestMethod]
        public void ParseBingGeocodeResponse_NoMatchIsNull()
        {
            Assert.IsNull(Utility.ParseBingGeocodeResponse("{\"statusCode\":200,\"resourceSets\":[{\"estimatedTotal\":0,\"resources\":[]}]}"));
        }

        [TestMethod]
        public void ParseBingGeocodeResponse_ErrorThrowsWithTheDetails()
        {
            try
            {
                Utility.ParseBingGeocodeResponse("{\"statusCode\":401,\"statusDescription\":\"Unauthorized\",\"errorDetails\":[\"Access was denied.\"]}");
                Assert.Fail("Expected InvalidPluginExecutionException");
            }
            catch (InvalidPluginExecutionException ex)
            {
                StringAssert.Contains(ex.Message, "401");
                StringAssert.Contains(ex.Message, "Access was denied.");
            }
        }

        [TestMethod]
        public void ParseAzureMapsGeocodeResponse_SwapsGeoJsonLongitudeLatitude()
        {
            const string json = "{\"type\":\"FeatureCollection\",\"features\":[{\"type\":\"Feature\",\"geometry\":{\"type\":\"Point\",\"coordinates\":[-122.138669,47.630359]}}]}";

            var location = Utility.ParseAzureMapsGeocodeResponse(json);

            Assert.AreEqual(47.630359m, location.Latitude);
            Assert.AreEqual(-122.138669m, location.Longitude);
        }

        [TestMethod]
        public void ParseAzureMapsGeocodeResponse_NoMatchIsNull()
        {
            Assert.IsNull(Utility.ParseAzureMapsGeocodeResponse("{\"type\":\"FeatureCollection\",\"features\":[]}"));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPluginExecutionException))]
        public void ParseAzureMapsGeocodeResponse_ErrorThrows()
        {
            Utility.ParseAzureMapsGeocodeResponse("{\"error\":{\"code\":\"401 Unauthorized\",\"message\":\"Invalid subscription key.\"}}");
        }

        [TestMethod]
        public void GeocodeAddress_EmptyAddressIsNullWithoutARequest()
        {
            Assert.IsNull(Utility.GeocodeAddress("  ", "key"));
        }
    }
}
