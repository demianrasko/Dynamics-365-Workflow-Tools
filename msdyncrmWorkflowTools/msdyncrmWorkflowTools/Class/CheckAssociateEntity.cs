using System.Activities;
using System.Linq;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools
{

   
    public class CheckAssociateEntity : CodeActivity
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Relationship Name")]
        [Default("")]        
        public InArgument<string> RelationshipName { get; set; }

        [RequiredArgument]
        [Input("Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> RecordURL { get; set; }


        [Output("Result")]
        public OutArgument<bool> Result { get; set; }
        #endregion

        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var _relationshipName = RelationshipName.Get(executionContext);
            var _recordURL = RecordURL.Get(executionContext);
            if (_recordURL == null || _recordURL == string.Empty)
            {
                return;
            }
            var urlParts = _recordURL.Split("?".ToArray());
            var urlParams=urlParts[1].Split("&".ToCharArray());
            var ParentObjectTypeCode=urlParams[0].Replace("etc=",string.Empty);
            var entityName = objCommon.GetEntityNameFromCode(ParentObjectTypeCode, objCommon.service);
            var ParentId = urlParams[1].Replace("id=", string.Empty);
            objCommon.tracingService.Trace("ParentObjectTypeCode=" + ParentObjectTypeCode + "--ParentId=" + ParentId);
            #endregion


            #region "Associate Execution"

            try
            {
                var relations = objCommon.GetAssociations(objCommon.context.PrimaryEntityName, objCommon.context.PrimaryEntityId,_relationshipName, entityName, ParentId);

                if (relations.Entities.Count > 0)
                {
                    Result.Set(executionContext, true);
                }
                else
                {
                    Result.Set(executionContext, false);
                }
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                if (ex.Detail.ErrorCode != 2147220937)//ignore if the error is a duplicate insert
                {
                    throw ex;
                }
            }
            #endregion

        }


    }
}
