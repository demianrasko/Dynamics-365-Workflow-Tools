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

   
    public class CheckAssociateEntity : CodeActivity
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


        [Output("Result")]
        public OutArgument<bool> Result { get; set; }
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
                string fetchXML = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                                      <entity name='"+ objCommon.context.PrimaryEntityName + @"'>
                                        <link-entity name='"+ _relationshipName + @"' from='"+ objCommon.context.PrimaryEntityName + @"id' to='"+ objCommon.context.PrimaryEntityName + @"id' visible='false' intersect='true'>
                                         <link-entity name='"+ entityName + @"' from='"+ entityName + @"id' to='"+ entityName + @"id' alias='ac'>
                                                <filter type='and'>
                                                  <condition attribute='"+ entityName + @"id' operator='eq' value='"+ ParentId + @"' />
                                                </filter>
                                              </link-entity>
                                        </link-entity>
                                      </entity>
                                    </fetch>";
                objCommon.tracingService.Trace(String.Format("FetchXML: {0} ", fetchXML));
                EntityCollection relations = objCommon.service.RetrieveMultiple(new FetchExpression(fetchXML));

                if (relations.Entities.Count > 0)
                {
                    this.Result.Set(executionContext, true);
                }
                else
                {
                    this.Result.Set(executionContext, false);
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
