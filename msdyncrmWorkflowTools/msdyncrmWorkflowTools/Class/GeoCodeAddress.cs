using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

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


      


        [Output("Latitude")]
        public OutArgument<decimal> Latitude { get; set; }

        [Output("Longitude")]
        public OutArgument<decimal> Longitude { get; set; }

       

        #endregion

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {

            #region "Read Parameters"
            

            var address = Address.Get(executionContext);
            var bingMapsKey = BingMapsKey.Get(executionContext);
            
            #endregion
            

            var locationsRequest = CreateRequest(address, bingMapsKey);
            var locationsResponse = MakeRequest(locationsRequest);

            if (locationsResponse != null)
            {
                Latitude.Set(executionContext, Convert.ToDecimal(locationsResponse.ResourceSets[0].Resources[0].GeocodePoints[0].Coordinates[0]));
                Longitude.Set(executionContext, Convert.ToDecimal(locationsResponse.ResourceSets[0].Resources[0].GeocodePoints[0].Coordinates[1]));
            }        

        }
        public  string CreateRequest(string queryString, string bingMapsKey)
        {
            var UrlRequest = "http://dev.virtualearth.net/REST/v1/Locations/" +
                                 queryString +
                                 "?output=json" +
                                 " &key=" + bingMapsKey;
            return (UrlRequest);
        }

        public  Response MakeRequest(string requestUrl)
        {
            var request = WebRequest.Create(requestUrl) as HttpWebRequest;
            using (var response = request.GetResponse() as HttpWebResponse)
            {
                if (response.StatusCode != HttpStatusCode.OK)
                    throw new InvalidPluginExecutionException(string.Format(
                    "Bing Maps server error (HTTP {0}: {1}).",
                    response.StatusCode,
                    response.StatusDescription));

                var jsonSerializer = new DataContractJsonSerializer(typeof(Response));
                var objResponse = jsonSerializer.ReadObject(response.GetResponseStream());
                var jsonResponse
                = objResponse as Response;
                return jsonResponse;
            }
        }




    }


    
        [DataContract]
        public class Response
        {
            [DataMember(Name = "copyright")]
            public string Copyright { get; set; }
            [DataMember(Name = "brandLogoUri")]
            public string BrandLogoUri { get; set; }
            [DataMember(Name = "statusCode")]
            public int StatusCode { get; set; }
            [DataMember(Name = "statusDescription")]
            public string StatusDescription { get; set; }
            [DataMember(Name = "authenticationResultCode")]
            public string AuthenticationResultCode { get; set; }
            [DataMember(Name = "errorDetails")]
            public string[] errorDetails { get; set; }
            [DataMember(Name = "traceId")]
            public string TraceId { get; set; }
            [DataMember(Name = "resourceSets")]
            public ResourceSet[] ResourceSets { get; set; }
        }


        [DataContract]
        public class ResourceSet
        {
            [DataMember(Name = "estimatedTotal")]

            public long EstimatedTotal { get; set; }
            [DataMember(Name = "resources")]
            public Location[] Resources { get; set; }
        }

        [DataContract]
        public class Point
        {
            /// <summary>
            /// Latitude,Longitude
            /// </summary>
            [DataMember(Name = "coordinates")]
            public double[] Coordinates { get; set; }
        }


        [DataContract]
        public class BoundingBox
        {
            [DataMember(Name = "southLatitude")]
            public double SouthLatitude { get; set; }
            [DataMember(Name = "westLongitude")]
            public double WestLongitude { get; set; }
            [DataMember(Name = "northLatitude")]
            public double NorthLatitude { get; set; }
            [DataMember(Name = "eastLongitude")]
            public double EastLongitude { get; set; }
        }

        [DataContract]
        public class GeocodePoint : Point
        {
            [DataMember(Name = "calculationMethod")]
            public string CalculationMethod { get; set; }
            [DataMember(Name = "usageTypes")]
            public string[] UsageTypes { get; set; }
        }

        [DataContract(Namespace = "http://schemas.microsoft.com/search/local/ws/rest/v1")]
        public class Location
        {
            [DataMember(Name = "boundingBox")]
            public BoundingBox BoundingBox { get; set; }
            [DataMember(Name = "name")]
            public string Name { get; set; }
            [DataMember(Name = "point")]
            public Point Point { get; set; }
            [DataMember(Name = "entityType")]
            public string EntityType { get; set; }
            [DataMember(Name = "address")]
            public Address Address { get; set; }
            [DataMember(Name = "confidence")]
            public string Confidence { get; set; }
            [DataMember(Name = "geocodePoints")]
            public GeocodePoint[] GeocodePoints { get; set; }
            [DataMember(Name = "matchCodes")]
            public string[] MatchCodes { get; set; }
        }

        [DataContract]
        public class Address
        {
            [DataMember(Name = "addressLine")]
            public string AddressLine { get; set; }
            [DataMember(Name = "adminDistrict")]
            public string AdminDistrict { get; set; }
            [DataMember(Name = "adminDistrict2")]
            public string AdminDistrict2 { get; set; }
            [DataMember(Name = "countryRegion")]
            public string CountryRegion { get; set; }
            [DataMember(Name = "formattedAddress")]
            public string FormattedAddress { get; set; }
            [DataMember(Name = "locality")]
            public string Locality { get; set; }
            [DataMember(Name = "postalCode")]
            public string PostalCode { get; set; }
        }
    


}
