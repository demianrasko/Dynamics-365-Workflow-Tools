using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;

namespace msdyncrmWorkflowTools
{
    public class DistributeWFActivityOneToMany : CodeActivity
    {
        [Input("Relationship Name"), RequiredArgument]
        public InArgument<string> RelationshipName { get; set; }

        [ReferenceTarget("workflow"), Input("Distributed Workflow"), RequiredArgument]
        public InArgument<EntityReference> Workflow { get; set; }


        protected void Distribute(CodeActivityContext executionContext)
        {

            ICollection<Guid> is2 = this.GatherKeys(executionContext);
            Guid id = this.Workflow.Get(executionContext).Id;
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            foreach (Guid guid2 in is2)
            {
                ExecuteWorkflowRequest request = new ExecuteWorkflowRequest
                {
                    EntityId = guid2,
                    WorkflowId = id
                };
                service.Execute(request);
            }
        }

        protected override void Execute(CodeActivityContext executionContext)
        {
            this.Distribute(executionContext);
        }

        protected ICollection<Guid> GatherKeys(CodeActivityContext executionContext)
        {
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            OneToManyRelationshipMetadata relationship = this.GetRelationship(executionContext);
            QueryByAttribute attribute = new QueryByAttribute
            {
                EntityName = relationship.ReferencingEntity
            };
            attribute.Attributes.Add(relationship.ReferencingAttribute);
            attribute.Values.Add(context.PrimaryEntityId.ToString());
            RetrieveMultipleRequest request = new RetrieveMultipleRequest
            {
                Query = attribute
            };
            List<Guid> list = new List<Guid>();
            RetrieveMultipleResponse response = (RetrieveMultipleResponse)service.Execute(request);
            foreach (Entity entity in response.EntityCollection.Entities)
            {
                list.Add(entity.Id);
            }
            return list;
        }

        private OneToManyRelationshipMetadata GetRelationship(CodeActivityContext executionContext)
        {
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            RetrieveRelationshipRequest request = new RetrieveRelationshipRequest
            {
                Name = this.RelationshipName.Get(executionContext),
                RetrieveAsIfPublished = false
            };
            RetrieveRelationshipResponse response = (RetrieveRelationshipResponse)service.Execute(request);
            if (!(response.RelationshipMetadata is OneToManyRelationshipMetadata))
            {
                throw new Exception("Relationship is not One to Many");
            }
            return (OneToManyRelationshipMetadata)response.RelationshipMetadata;
        }
    }
}
