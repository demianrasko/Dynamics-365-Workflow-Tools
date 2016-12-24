using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using msdyncrmWorkflowTools;
using System.ServiceModel;

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
        [Input("Record URL")]
        [ReferenceTarget("")]
        public InArgument<String> RecordURL { get; set; }
        #endregion

        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            String _relationshipName = this.RelationshipName.Get(executionContext);
            String _recordURL = this.RecordURL.Get(executionContext);
            if (_recordURL == null || _recordURL == "")
            {
                return;
            }
            string[] urlParts = _recordURL.Split("?".ToArray());
            string[] urlParams=urlParts[1].Split("&".ToCharArray());
            string ParentObjectTypeCode=urlParams[0].Replace("etc=","");
            string entityName = objCommon.sGetEntityNameFromCode(ParentObjectTypeCode, objCommon.service);
            string ParentId = urlParams[1].Replace("id=", "");
            objCommon.tracingService.Trace("ParentObjectTypeCode=" + ParentObjectTypeCode + "--ParentId=" + ParentId);
            #endregion


            #region "Associate Execution"

            try
            {
                EntityCollection relations = objCommon.getAssociations(objCommon.context.PrimaryEntityName, _relationshipName, entityName, ParentId);


                if (relations.Entities.Count == 0)
                {
                    EntityReferenceCollection relatedEntities = new EntityReferenceCollection();
                    relatedEntities.Add(new EntityReference(entityName, new Guid(ParentId)));
                    Relationship relationship = new Relationship(_relationshipName);
                    objCommon.service.Associate(objCommon.context.PrimaryEntityName, objCommon.context.PrimaryEntityId, relationship, relatedEntities);
                }
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
