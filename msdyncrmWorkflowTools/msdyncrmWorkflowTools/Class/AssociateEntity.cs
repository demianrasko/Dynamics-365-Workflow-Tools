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
        public InArgument<String> RelationshipName { get; set; }

        [RequiredArgument]
        [Input("Relationship Entity Name")]
        [Default("")]
        public InArgument<String> RelationshipEntityName { get; set; }

        [RequiredArgument]
        [Input("Record URL")]
        [ReferenceTarget("")]
        public InArgument<String> RecordURL { get; set; }

        

        #endregion

        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var _relationshipName = RelationshipName.Get(executionContext);
            var _relationshipEntityName = RelationshipEntityName.Get(executionContext);
            var _recordURL = RecordURL.Get(executionContext);
            if (_recordURL == null || _recordURL == "")
            {
                return;
            }
            var urlParts = _recordURL.Split("?".ToArray());
            var urlParams=urlParts[1].Split("&".ToCharArray());
            var ParentObjectTypeCode=urlParams[0].Replace("etc=","");
            var entityName = objCommon.sGetEntityNameFromCode(ParentObjectTypeCode, objCommon.service);
            var ParentId = urlParams[1].Replace("id=", "");
            objCommon.tracingService.Trace("ParentObjectTypeCode=" + ParentObjectTypeCode + "--ParentId=" + ParentId);
            #endregion


            #region "Associate Execution"

            try
            {
                var commonClass = new msdyncrmWorkflowTools_Class(objCommon.service);
                commonClass.AssociateEntity(objCommon.context.PrimaryEntityName, objCommon.context.PrimaryEntityId, _relationshipName, _relationshipEntityName, entityName, ParentId);

                
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
            catch (System.Exception ex)
            {
                objCommon.tracingService.Trace("Error : {0} - {1}", ex.Message, ex.StackTrace);
                //throw ex;
            }
            #endregion

        }


    }
}
