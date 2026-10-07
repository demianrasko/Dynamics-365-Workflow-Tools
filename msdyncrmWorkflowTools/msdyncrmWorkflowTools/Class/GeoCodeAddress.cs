using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Geocode Address")]
    public class GeoCodeAddress : WorkflowActivityBase
    {
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

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var location = Utility.GeocodeAddress(Address.Get(executionContext), BingMapsKey.Get(executionContext), AzureMapsKey.Get(executionContext), common.TracingService);

            Latitude.Set(executionContext, location?.Latitude ?? 0);
            Longitude.Set(executionContext, location?.Longitude ?? 0);
        }
    }
}
