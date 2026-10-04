using System.Activities;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools
{
   public class GetSharepointLocationURL : CodeActivity
    {
        [RequiredArgument]
        [Input("Record URL")]
        public InArgument<string> RecordURL { get; set; }

        [Output("SharepointLocationURL")]
        public OutArgument<string> SharepointLocationURL { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");

            var recordId = GetRecordIdFromURL(executionContext);

            var locatioColl = GetSharepointLocation(objCommon.service, recordId);

            string absoluteURL;
            absoluteURL = GetAbsoluteURLFromLocation(objCommon, locatioColl);

            SharepointLocationURL.Set(executionContext, absoluteURL);
        }

        private static string GetAbsoluteURLFromLocation(Common objCommon, EntityCollection locatioColl)
        {
            string absoluteURL;
            if (locatioColl.Entities.Count > 0)
            {
                var retrieveRequest = new RetrieveAbsoluteAndSiteCollectionUrlRequest
                {
                    Target = new EntityReference(locatioColl[0].LogicalName, locatioColl[0].Id)
                };
                var retriveResponse = (RetrieveAbsoluteAndSiteCollectionUrlResponse)objCommon.service.Execute(retrieveRequest);

                absoluteURL = retriveResponse.AbsoluteUrl.ToString();
                objCommon.tracingService.Trace("Absolute URL of document location record is '{0}'." + retriveResponse.AbsoluteUrl.ToString());
            }
            else
            {
                absoluteURL = "URL Not found";
            }

            return absoluteURL;
        }

        private string GetRecordIdFromURL(CodeActivityContext executionContext)
        {
            var _recordURL = RecordURL.Get<string>(executionContext);

            var urlParts = _recordURL.Split("?".ToArray());
            var urlParams = urlParts[1].Split("&".ToCharArray());
            var recordId = urlParams[1].Replace("id=", string.Empty);
            return recordId;
        }

        private static EntityCollection GetSharepointLocation(IOrganizationService service, string regardingobjectid)
        {
            var QEsharepointdocumentlocation = new QueryExpression("sharepointdocumentlocation");
            QEsharepointdocumentlocation.ColumnSet.AddColumns("absoluteurl", "sharepointdocumentlocationid", "relativeurl");
            QEsharepointdocumentlocation.Criteria.AddCondition("regardingobjectid", ConditionOperator.Equal, regardingobjectid);

            var locatioColl = service.RetrieveMultiple(QEsharepointdocumentlocation);
            return locatioColl;
        }
    }
}
