using System;
using System.Activities;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;


namespace msdyncrmWorkflowTools
{

   
    public class DisassociateEntity : CodeActivity
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Relationship Name")]
        [Default("")]        
        public InArgument<String> RelationshipName { get; set; }

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


            #region "Disassociate Execution"

            var relatedEntities = new EntityReferenceCollection();
            relatedEntities.Add(new EntityReference(entityName, new Guid(ParentId)));
            var relationship = new Relationship(_relationshipName);
            objCommon.service.Disassociate(objCommon.context.PrimaryEntityName, objCommon.context.PrimaryEntityId, relationship,relatedEntities);
            
            #endregion

        }


    }
}
