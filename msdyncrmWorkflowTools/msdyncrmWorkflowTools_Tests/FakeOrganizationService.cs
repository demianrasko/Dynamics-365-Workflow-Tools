using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;

namespace msdyncrmWorkflowTools_Tests
{
    /// <summary>
    /// In-memory IOrganizationService for unit tests. Each call is recorded; Execute, Retrieve and RetrieveMultiple
    /// answer through handlers the test sets, so Common can be tested without a Dataverse connection.
    /// </summary>
    internal sealed class FakeOrganizationService : IOrganizationService
    {
        public Func<OrganizationRequest, OrganizationResponse> OnExecute { get; set; } =
            request => throw new AssertFailedException($"Unexpected {request.GetType().Name}.");

        public Func<QueryBase, EntityCollection> OnRetrieveMultiple { get; set; } = query => new EntityCollection();

        public Func<string, Guid, ColumnSet, Entity> OnRetrieve { get; set; } = (entityName, id, columns) => new Entity(entityName, id);

        /// <summary>
        /// Runs before an update is recorded; throw from it to make the update fail.
        /// </summary>
        public Action<Entity> OnUpdate { get; set; } = entity => { };

        public List<OrganizationRequest> Executed { get; } = new List<OrganizationRequest>();

        public List<QueryBase> Queries { get; } = new List<QueryBase>();

        public List<ColumnSet> Retrieved { get; } = new List<ColumnSet>();

        public List<Entity> Created { get; } = new List<Entity>();

        public List<Entity> Updated { get; } = new List<Entity>();

        public List<EntityReference> Deleted { get; } = new List<EntityReference>();

        public Guid Create(Entity entity)
        {
            Created.Add(entity);

            return entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id;
        }

        public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet)
        {
            Retrieved.Add(columnSet);

            return OnRetrieve(entityName, id, columnSet);
        }

        public void Update(Entity entity)
        {
            OnUpdate(entity);
            Updated.Add(entity);
        }

        public void Delete(string entityName, Guid id)
        {
            Deleted.Add(new EntityReference(entityName, id));
        }

        public OrganizationResponse Execute(OrganizationRequest request)
        {
            Executed.Add(request);

            return OnExecute(request);
        }

        public List<AssociateCall> Associated { get; } = new List<AssociateCall>();

        public List<AssociateCall> Disassociated { get; } = new List<AssociateCall>();

        /// <summary>
        /// Runs before an association is recorded; throw from it to make the association fail.
        /// </summary>
        public Action OnAssociate { get; set; } = () => { };

        public void Associate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
        {
            OnAssociate();
            Associated.Add(new AssociateCall(new EntityReference(entityName, entityId), relationship, relatedEntities));
        }

        public void Disassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
        {
            Disassociated.Add(new AssociateCall(new EntityReference(entityName, entityId), relationship, relatedEntities));
        }

        public EntityCollection RetrieveMultiple(QueryBase query)
        {
            Queries.Add(query);

            return OnRetrieveMultiple(query);
        }
    }

    /// <summary>
    /// ITracingService that keeps every message, so tests can check what was traced.
    /// </summary>
    internal sealed class TraceRecorder : ITracingService
    {
        public List<string> Messages { get; } = new List<string>();

        public void Trace(string format, params object[] args)
        {
            Messages.Add(args == null || args.Length == 0 ? format : string.Format(format, args));
        }
    }

    /// <summary>
    /// One Associate or Disassociate call recorded by <see cref="FakeOrganizationService"/>.
    /// </summary>
    internal sealed class AssociateCall
    {
        public AssociateCall(EntityReference record, Relationship relationship, EntityReferenceCollection related)
        {
            Record = record;
            Relationship = relationship;
            Related = related;
        }

        public EntityReference Record { get; }

        public Relationship Relationship { get; }

        public EntityReferenceCollection Related { get; }
    }
}
