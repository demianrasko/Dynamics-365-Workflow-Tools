using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace msdyncrmWorkflowTools
{
    public class GeoCodeAddress : CodeActivity
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Address")]
        [Default("")]
        public InArgument<String> Address { get; set; }

        [RequiredArgument]
        [Input("Bing Maps Key")]
        [Default("")]
        public InArgument<string> BingMapsKey { get; set; }


      


        [Output("Latitude")]
        public OutArgument<Decimal> Latitude { get; set; }

        [Output("Longitude")]
        public OutArgument<Decimal> Longitude { get; set; }

       

        #endregion

        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            

            string address = this.Address.Get(executionContext);
            string bingMapsKey = this.BingMapsKey.Get(executionContext);
            
            #endregion
            

            string locationsRequest = CreateRequest(address, bingMapsKey);
            Response locationsResponse = MakeRequest(locationsRequest);

            if (locationsResponse != null)
            {
                this.Latitude.Set(executionContext, Convert.ToDecimal(locationsResponse.ResourceSets[0].Resources[0].GeocodePoints[0].Coordinates[0]));
                this.Longitude.Set(executionContext, Convert.ToDecimal(locationsResponse.ResourceSets[0].Resources[0].GeocodePoints[0].Coordinates[1]));
            }        

        }
        public  string CreateRequest(string queryString, string bingMapsKey)
        {
            string UrlRequest = "http://dev.virtualearth.net/REST/v1/Locations/" +
                                 queryString +
                                 "?output=json" +
                                 " &key=" + bingMapsKey;
            return (UrlRequest);
        }

        public  Response MakeRequest(string requestUrl)
        {
            try
            {
                HttpWebRequest request = WebRequest.Create(requestUrl) as HttpWebRequest;
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                        throw new Exception(String.Format(
                        "Server error (HTTP {0}: {1}).",
                        response.StatusCode,
                        response.StatusDescription));

                    DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(Response));
                    object objResponse = jsonSerializer.ReadObject(response.GetResponseStream());
                    Response jsonResponse
                    = objResponse as Response;
                    return jsonResponse;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
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
