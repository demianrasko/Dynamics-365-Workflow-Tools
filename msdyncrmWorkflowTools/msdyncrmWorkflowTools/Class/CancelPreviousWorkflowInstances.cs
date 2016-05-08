using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
namespace msdyncrmWorkflowTools.Class
{
    public class CancelPreviousWorkflowInstances : CodeActivity
    {
        [RequiredArgument]
        [Input("Delete Workflow Instances")]
        public InArgument<bool> DeleteWorkflowInstances { get; set; }

        public enum AsyncOperationState
        {
            [EnumMember]Completed = 3
        }
        

        public enum AsyncoperationStatuscode
        {
            [EnumMember]WaitingForResources = 0,
            [EnumMember]Waiting = 10,
            [EnumMember]InProgress = 20,
            [EnumMember]Pausing = 21,
            [EnumMember]Canceling = 22,
            [EnumMember]Succeeded = 30,
            [EnumMember]Failed = 31,
            [EnumMember]Canceled = 32,
        }

        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            var context = executionContext.GetExtension<IWorkflowContext>();
            
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            bool deleteWorkflowInstances = this.DeleteWorkflowInstances.Get(executionContext);
            objCommon.tracingService.Trace(String.Format("deleteWorkflowInstances: {0} ", deleteWorkflowInstances.ToString()));

            #endregion

            objCommon.tracingService.Trace("Retrieving jobname...executionContext.WorkflowInstanceId:{0}", executionContext.WorkflowInstanceId.ToString());
            var systemJobName = GetSystemJobName(objCommon.service, context.PrimaryEntityId, context.OperationId.ToString());
            objCommon.tracingService.Trace("jobname: {0}", systemJobName);
            if (string.IsNullOrEmpty(systemJobName))
            {
                return;
            }
            objCommon.tracingService.Trace("KillExistingWorkflowInstances...");
            KillExistingWorkflowInstances(objCommon.service, systemJobName, context.PrimaryEntityId, context.OperationId, deleteWorkflowInstances, objCommon);


        }
        private static string GetSystemJobName(IOrganizationService service, Guid primaryEntityId, string activityInstance)
        {
            var query = new QueryExpression("asyncoperation")
            {
                ColumnSet = new ColumnSet(new[] { "name" }),
                Criteria = new FilterExpression { FilterOperator = LogicalOperator.And }
            };

            //query.Criteria.AddCondition("regardingobjectid", ConditionOperator.Equal, primaryEntityId);
            //query.Criteria.AddCondition("statuscode", ConditionOperator.Equal, (int)AsyncoperationStatuscode.Waiting);
            //query.Criteria.AddCondition("workflowactivationid", ConditionOperator.Equal, new Guid(activityInstance));
            query.Criteria.AddCondition("asyncoperationid", ConditionOperator.Equal, new Guid(activityInstance));
            var response = service.RetrieveMultiple(query);

            return response.Entities.Count == 0 ? null : response.Entities[0]["name"].ToString();
        }

        public static void KillExistingWorkflowInstances(IOrganizationService crmService, string workflowName,
                                                         Guid entityId, Guid asyncOperationId, bool deleteWorkflowInstances, Common objCommon)
        {
            var results = GetWorkflowInstances(crmService, workflowName, entityId, asyncOperationId);

            if (results.Entities.Count == 0)
            {
                return;
            }

            var errorList = new StringBuilder();


            string fetchParty = @"<fetch version='1.0' output-format='xml - platform' mapping='logical' distinct='true'>
                                                <entity name='asyncoperation'>
                                                    <attribute name = 'statuscode'/>
                                                    <attribute name = 'statecode'/>
                                                        <filter type = 'or' >
                                                            <condition attribute = 'statuscode' operator= 'eq' value = '" + AsyncoperationStatuscode.WaitingForResources + @"' />
                                                            <condition attribute = 'statuscode' operator= 'eq' value = '" + AsyncoperationStatuscode.Waiting + @"' />
                                                            <condition attribute = 'statuscode' operator= 'eq' value = '" + AsyncoperationStatuscode.Pausing + @"' />
                                                            <condition attribute = 'statuscode' operator= 'eq' value = '" + AsyncoperationStatuscode.InProgress + @"' />
                                                         </filter>
                                                </entity>
                                            </fetch> ";
            objCommon.tracingService.Trace(fetchParty);

            RetrieveMultipleRequest fetchRequest1 = new RetrieveMultipleRequest
            {
                Query = new FetchExpression(fetchParty)
            };
            
            EntityCollection returnCollection = ((RetrieveMultipleResponse)crmService.Execute(fetchRequest1)).EntityCollection;
            


            foreach (var singleWorkflowInstance in returnCollection.Entities)
            {
                singleWorkflowInstance.Attributes["statuscode"] = new OptionSetValue((int)AsyncoperationStatuscode.Canceled);
                singleWorkflowInstance.Attributes["statecode"] = new OptionSetValue((int)AsyncOperationState.Completed);

                crmService.Update(singleWorkflowInstance);

                if (deleteWorkflowInstances)
                {
                    crmService.Delete("asyncoperation", singleWorkflowInstance.Id);
                }
            }

            
        }

        private static EntityCollection GetWorkflowInstances(IOrganizationService crmService, string workflowName, Guid entityId,
                                                             Guid asyncOperationId)
        {
            var query = new QueryExpression
            {
                EntityName = "asyncoperation",
                ColumnSet = new ColumnSet(new[] { "name", "statuscode", "asyncoperationid", "regardingobjectid" }),
                Criteria = new FilterExpression { FilterOperator = LogicalOperator.And }
            };

            query.Criteria.AddCondition("name", ConditionOperator.Equal, workflowName);
            query.Criteria.AddCondition("regardingobjectid", ConditionOperator.Equal, entityId);
            query.Criteria.AddCondition("asyncoperationid", ConditionOperator.NotEqual, asyncOperationId);

            return crmService.RetrieveMultiple(query);
        }
    }
}
