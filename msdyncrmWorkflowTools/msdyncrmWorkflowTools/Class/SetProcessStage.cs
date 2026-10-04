using System;
using System.Activities;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools.Class
{
    public class SetProcessStage : CodeActivity
    {
        [RequiredArgument]
        [Input("Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> ClonningRecordURL { get; set; }

        [Input("Process")]
        [ReferenceTarget("workflow")]
        public InArgument<EntityReference> Process { get; set; }

        [Input("Process Stage Name")]
        public InArgument<string> ProcessStage { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var cloningRecordUrl = ClonningRecordURL.Get(executionContext);
            
            if (string.IsNullOrEmpty(cloningRecordUrl))
            {
                return;
            }

            var urlParts = cloningRecordUrl.Split("?".ToArray());
            var urlParams = urlParts[1].Split("&".ToCharArray());
            var objectTypeCode = urlParams[0].Replace("etc=", string.Empty);
            var objectId = urlParams[1].Replace("id=", string.Empty);
            var entityName = objCommon.GetEntityNameFromCode(objectTypeCode, objCommon.service);

            objCommon.tracingService.Trace($"ObjectTypeCode={objectTypeCode}--ParentId={objectId}");

            var process = Process.Get(executionContext);
            var processStage = ProcessStage.Get(executionContext);

            #endregion

            #region "SetProcessStage Execution"

            Guid? stageId = null;
            if (processStage != null)
            {
                objCommon.tracingService.Trace("[Dynamics.ChangeBPFandPhase.Execute] Process stage: " + processStage);
                Entity stageReference;

                var queryStage = new QueryExpression("processstage")
                {
                    ColumnSet = new ColumnSet()
                };
                queryStage.Criteria.AddCondition(new ConditionExpression("stagename", ConditionOperator.Equal, processStage));

                queryStage.Criteria.AddCondition(new ConditionExpression("processid", ConditionOperator.Equal, process.Id));

                objCommon.tracingService.Trace("[Dynamics.ChangeBPFandPhase.Execute] Fetching the requested Stage.");
                try
                {
                    stageReference = objCommon.service.RetrieveMultiple(queryStage).Entities.FirstOrDefault();
                    if (stageReference == null)
                    {
                        throw new InvalidPluginExecutionException(nameof(Process) + " stage " + processStage + " not found");
                    }

                    stageId = stageReference.Id;
                }
                catch (Exception e)
                {
                    objCommon.tracingService.Trace(
                        $"[Dynamics.ChangeBPFandPhase.Execute] Error trying to retrieve the requested stage. Exception: {e}");
                    throw new InvalidPluginExecutionException(
                        $"An error occurred while trying to fetch process stage {processStage}. Exception message: {e.Message}. Inner Exception: {e}");
                }
            }

            //*************************
            var procOpp1Req = new RetrieveProcessInstancesRequest
            {
                EntityId = new Guid(objectId),
                EntityLogicalName = entityName
            };

            var procOpp1Resp = (RetrieveProcessInstancesResponse)objCommon.service.Execute(procOpp1Req);

            // Declare variables to store values returned in response
            var processOpp1Id = Guid.Empty;
            var procInstanceLogicalName = string.Empty;

            if (procOpp1Resp.Processes.Entities.Count > 0)
            {
                var activeProcessInstance = procOpp1Resp.Processes.Entities[0];
            
                processOpp1Id = activeProcessInstance.Id; // Id of the active process instance, which will be used
                                                           // later to retrieve the active path of the process instance

                objCommon.tracingService.Trace("Current active process instance for the Opportunity record: '{0}'", activeProcessInstance["name"].ToString());

                // Get the BPF underlying entity logical name
                const string uniqueProcessNameAttribute = "uniquename";
                var processEntity = objCommon.service.Retrieve("workflow", process.Id, new ColumnSet(uniqueProcessNameAttribute));
                procInstanceLogicalName = processEntity.Attributes[uniqueProcessNameAttribute].ToString();
            }
            else
            {
                objCommon.tracingService.Trace("No process instances found for the opportunity record; aborting the sample.");
                Environment.Exit(1);
            }

            objCommon.tracingService.Trace("Starting the update");
            var processInstanceToUpdate= new Entity(procInstanceLogicalName, processOpp1Id);
            processInstanceToUpdate.Attributes.Add("activestageid", new EntityReference("processstage", stageId.Value));
            objCommon.tracingService.Trace("Starting the update2");
            objCommon.service.Update(processInstanceToUpdate);
            objCommon.tracingService.Trace("Starting the update3");
            #endregion
        }
    }
}
