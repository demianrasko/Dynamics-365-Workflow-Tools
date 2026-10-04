using System;
using System.Activities;
using System.Linq;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools
{
    public class AssociateEntity : CodeActivity
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Relationship Name")]
        [Default("")]        
        public InArgument<string> RelationshipName { get; set; }

        [RequiredArgument]
        [Input("Relationship Entity Name")]
        [Default("")]
        public InArgument<string> RelationshipEntityName { get; set; }

        [RequiredArgument]
        [Input("Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> RecordURL { get; set; }

        

        #endregion

        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var relationshipName = RelationshipName.Get(executionContext);
            var relationshipEntityName = RelationshipEntityName.Get(executionContext);
            var recordUrl = RecordURL.Get(executionContext);

            if (string.IsNullOrEmpty(recordUrl))
            {
                return;
            }

            var urlParts = recordUrl.Split("?".ToArray());
            var urlParams=urlParts[1].Split("&".ToCharArray());
            var parentObjectTypeCode=urlParams[0].Replace("etc=",string.Empty);
            var entityName = objCommon.GetEntityNameFromCode(parentObjectTypeCode, objCommon.service);
            var parentId = urlParams[1].Replace("id=", string.Empty);

            objCommon.tracingService.Trace($"ParentObjectTypeCode={parentObjectTypeCode}--ParentId={parentId}");
            #endregion

            #region "Associate Execution"

            try
            {
                objCommon.AssociateEntity(objCommon.context.PrimaryEntityName, objCommon.context.PrimaryEntityId, relationshipName, relationshipEntityName, entityName, parentId);
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                objCommon.tracingService.Trace("Error : {0} - {1}", ex.Message, ex.StackTrace);
                //throw ex;
                // if (ex.Detail.ErrorCode != 2147220937)//ignore if the error is a duplicate insert
                //{
                // throw ex;
                //}
            }
            catch (Exception ex)
            {
                objCommon.tracingService.Trace("Error : {0} - {1}", ex.Message, ex.StackTrace);
                //throw ex;
            }
            #endregion
        }
    }
}
