using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class GeoCodeAddress : WorkflowActivityBase
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Address")]
        [Default("")]
        public InArgument<string> Address { get; set; }

        [RequiredArgument]
        [Input("Bing Maps Key")]
        [Default("")]
        public InArgument<string> BingMapsKey { get; set; }

        /// <summary>
        /// Azure Maps subscription key. When set, Azure Maps is used and the Bing Maps key is ignored.
        /// </summary>
        [Input("Azure Maps Key")]
        [Default("")]
        public InArgument<string> AzureMapsKey { get; set; }

        [Output("Latitude")]
        public OutArgument<decimal> Latitude { get; set; }

        [Output("Longitude")]
        public OutArgument<decimal> Longitude { get; set; }

        #endregion

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var address = Address.Get(executionContext);

            var location = Utility.GeocodeAddress(address, BingMapsKey.Get(executionContext), AzureMapsKey.Get(executionContext), common.TracingService);

            if (location == null)
            {
                common.Trace($"No location found for '{address}'.");
                return;
            }

            common.Trace($"Latitude={location.Latitude}, Longitude={location.Longitude}");
            Latitude.Set(executionContext, location.Latitude);
            Longitude.Set(executionContext, location.Longitude);
        }
    }
}
