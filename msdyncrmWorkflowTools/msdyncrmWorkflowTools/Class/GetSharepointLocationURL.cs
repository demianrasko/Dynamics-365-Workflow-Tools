using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
   public class GetSharepointLocationURL : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Record URL")]
        public InArgument<string> RecordURL { get; set; }

        [Output("SharepointLocationURL")]
        public OutArgument<string> SharepointLocationURL { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var recordId = GetRecordIdFromURL(executionContext);

            var locatioColl = GetSharepointLocation(common.service, recordId);

            string absoluteURL;
            absoluteURL = GetAbsoluteURLFromLocation(common, locatioColl);

            SharepointLocationURL.Set(executionContext, absoluteURL);
        }

        private static string GetAbsoluteURLFromLocation(Common common, EntityCollection locatioColl)
        {
            string absoluteURL;
            if (locatioColl.Entities.Count > 0)
            {
                var retrieveRequest = new RetrieveAbsoluteAndSiteCollectionUrlRequest
                {
                    Target = new EntityReference(locatioColl[0].LogicalName, locatioColl[0].Id)
                };
                var retriveResponse = (RetrieveAbsoluteAndSiteCollectionUrlResponse)common.service.Execute(retrieveRequest);

                absoluteURL = retriveResponse.AbsoluteUrl.ToString();
                common.Trace("Absolute URL of document location record is '{0}'." + retriveResponse.AbsoluteUrl.ToString());
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

            var parsedUrl = Utility.ParseRecordUrl(_recordURL);
            var recordId = parsedUrl.Id;
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
