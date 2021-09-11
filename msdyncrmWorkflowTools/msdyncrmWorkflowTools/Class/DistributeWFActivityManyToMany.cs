using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;

namespace msdyncrmWorkflowTools
{
    public class DistributeWFActivityManyToMany : CodeActivity
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

            ManyToManyRelationshipMetadata relationship = this.GetRelationship(executionContext);
            string intersectEntityName = relationship.IntersectEntityName;
            if ((relationship.Entity1LogicalName == context.PrimaryEntityName) && (relationship.Entity2LogicalName == context.PrimaryEntityName))
            {
                ICollection<Guid> is2 = this.GatherKeysInternal(executionContext, relationship.Entity1IntersectAttribute, relationship.Entity2LogicalName, relationship.Entity2IntersectAttribute, intersectEntityName);
                ICollection<Guid> is3 = this.GatherKeysInternal(executionContext, relationship.Entity2IntersectAttribute, relationship.Entity1LogicalName, relationship.Entity1IntersectAttribute, intersectEntityName);
                HashSet<Guid> source = new HashSet<Guid>();
                foreach (Guid guid in is2)
                {
                    if (!object.Equals(guid, context.PrimaryEntityId))
                    {
                        source.Add(guid);
                    }
                }
                foreach (Guid guid in is3)
                {
                    if (!object.Equals(guid, context.PrimaryEntityId) && !source.Contains(guid))
                    {
                        source.Add(guid);
                    }
                }
                return source.ToList<Guid>();
            }
            if (relationship.Entity1LogicalName == context.PrimaryEntityName)
            {
                return this.GatherKeysInternal(executionContext, relationship.Entity1IntersectAttribute, relationship.Entity2LogicalName, relationship.Entity2IntersectAttribute, intersectEntityName);
            }
            return this.GatherKeysInternal(executionContext, relationship.Entity2IntersectAttribute, relationship.Entity1LogicalName, relationship.Entity1IntersectAttribute, intersectEntityName);
        }

        private ICollection<Guid> GatherKeysInternal(CodeActivityContext executionContext, string primaryAttribute, string secondaryEntity, string secondaryAttribute, string intersection)
        {
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            QueryExpression expression = new QueryExpression();
            LinkEntity item = new LinkEntity();
            LinkEntity entity2 = new LinkEntity();
            ConditionExpression expression2 = new ConditionExpression();
            expression.EntityName = secondaryEntity;
            expression.LinkEntities.Add(item);
            item.LinkEntities.Add(entity2);
            entity2.LinkCriteria.Conditions.Add(expression2);
            item.LinkToEntityName = intersection;
            item.LinkFromAttributeName = item.LinkToAttributeName = secondaryAttribute;
            entity2.LinkToEntityName = context.PrimaryEntityName;
            entity2.LinkFromAttributeName = entity2.LinkToAttributeName = primaryAttribute;
            expression2.AttributeName = primaryAttribute;
            expression2.Operator = ConditionOperator.Equal;
            expression2.Values.Add(context.PrimaryEntityId.ToString());
            RetrieveMultipleRequest request = new RetrieveMultipleRequest
            {
                Query = expression
            };
            List<Guid> list = new List<Guid>();
            RetrieveMultipleResponse response = (RetrieveMultipleResponse)service.Execute(request);
            foreach (Entity entity3 in response.EntityCollection.Entities)
            {
                list.Add(entity3.Id);
            }
            return list;
        }

        private ManyToManyRelationshipMetadata GetRelationship(CodeActivityContext executionContext)
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
            if (!(response.RelationshipMetadata is ManyToManyRelationshipMetadata))
            {
                throw new Exception("Relationship is not Many to Many");
            }
            return (ManyToManyRelationshipMetadata)response.RelationshipMetadata;
        }

    }
}
