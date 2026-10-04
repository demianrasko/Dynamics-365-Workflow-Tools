using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Linq;

namespace msdyncrmWorkflowTools.Class
{
    public class SetProcessStage : WorkflowActivityBase
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

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var cloningRecordUrl = ClonningRecordURL.Get(executionContext);

            if (string.IsNullOrEmpty(cloningRecordUrl))
            {
                throw new InvalidPluginExecutionException("Record URL is required.");
            }

            var parsedUrl = Utility.ParseRecordUrl(cloningRecordUrl);
            var objectTypeCode = parsedUrl.ObjectTypeCode;
            var objectId = parsedUrl.Id;
            var entityName = common.GetEntityNameFromCode(objectTypeCode);

            common.Trace($"ObjectTypeCode={objectTypeCode}--ParentId={objectId}");

            var process = Process.Get(executionContext);
            var processStage = ProcessStage.Get(executionContext);

            #endregion

            #region "SetProcessStage Execution"

            Guid? stageId = null;
            if (processStage != null)
            {
                common.Trace("[Dynamics.ChangeBPFandPhase.Execute] Process stage: " + processStage);

                var queryStage = new QueryExpression("processstage")
                {
                    ColumnSet = new ColumnSet()
                };
                queryStage.Criteria.AddCondition(new ConditionExpression("stagename", ConditionOperator.Equal, processStage));

                queryStage.Criteria.AddCondition(new ConditionExpression("processid", ConditionOperator.Equal, process.Id));

                common.Trace("[Dynamics.ChangeBPFandPhase.Execute] Fetching the requested Stage.");
                var stageReference = common.service.RetrieveMultiple(queryStage).Entities.FirstOrDefault();
                if (stageReference == null)
                {
                    throw new InvalidPluginExecutionException(nameof(Process) + " stage " + processStage + " not found");
                }

                stageId = stageReference.Id;
            }

            //*************************
            var procOpp1Req = new RetrieveProcessInstancesRequest
            {
                EntityId = new Guid(objectId),
                EntityLogicalName = entityName
            };

            var procOpp1Resp = (RetrieveProcessInstancesResponse)common.service.Execute(procOpp1Req);

            // Declare variables to store values returned in response
            var processOpp1Id = Guid.Empty;
            var procInstanceLogicalName = string.Empty;

            if (procOpp1Resp.Processes.Entities.Count > 0)
            {
                var activeProcessInstance = procOpp1Resp.Processes.Entities[0];

                processOpp1Id = activeProcessInstance.Id; // Id of the active process instance, which will be used
                                                           // later to retrieve the active path of the process instance

                common.Trace("Current active process instance for the Opportunity record: '{0}'", activeProcessInstance["name"].ToString());

                // Get the BPF underlying entity logical name
                const string uniqueProcessNameAttribute = "uniquename";
                var processEntity = common.service.Retrieve("workflow", process.Id, new ColumnSet(uniqueProcessNameAttribute));
                procInstanceLogicalName = processEntity.Attributes[uniqueProcessNameAttribute].ToString();
            }
            else
            {
                common.Trace("No process instances found for the opportunity record; aborting the sample.");
                Environment.Exit(1);
            }

            common.Trace("Starting the update");
            var processInstanceToUpdate= new Entity(procInstanceLogicalName, processOpp1Id);
            processInstanceToUpdate.Attributes.Add("activestageid", new EntityReference("processstage", stageId.Value));
            common.Trace("Starting the update2");
            common.service.Update(processInstanceToUpdate);
            common.Trace("Starting the update3");
            #endregion
        }
    }
}
