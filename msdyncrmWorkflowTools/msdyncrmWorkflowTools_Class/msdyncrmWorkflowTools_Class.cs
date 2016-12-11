using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace msdyncrmWorkflowTools
{
    public class msdyncrmWorkflowTools_Class
    {
        private IOrganizationService service;
        private ITracingService tracing;

        public msdyncrmWorkflowTools_Class(IOrganizationService _service, ITracingService _tracing)
        {
            service = _service;
            tracing = _tracing;
        }

        public msdyncrmWorkflowTools_Class(IOrganizationService _service)
        {
            service = _service;
            tracing = null;
        }

        public void UpdateChildRecords(string relationshipName, string parentEntityType, string parentEntityId, string parentFieldNameToUpdate, string setValueToUpdate, string childFieldNameToUpdate)
        {
            //1) Get child lookup field name
            RetrieveRelationshipRequest req = new RetrieveRelationshipRequest()
            {
                Name = relationshipName
            };
            RetrieveRelationshipResponse res = (RetrieveRelationshipResponse)service.Execute(req);
            OneToManyRelationshipMetadata rel = (OneToManyRelationshipMetadata)res.RelationshipMetadata;
            string childEntityType = rel.ReferencingEntity;
            string childEntityFieldName = rel.ReferencingAttribute;

            //2) retrieve all child records
            QueryByAttribute querybyattribute = new QueryByAttribute(childEntityType);
            querybyattribute.ColumnSet = new ColumnSet(childEntityFieldName);
            querybyattribute.Attributes.AddRange(childEntityFieldName);
            querybyattribute.Values.AddRange(new Guid(parentEntityId));
            EntityCollection retrieved = service.RetrieveMultiple(querybyattribute);

            //2') retrieve parent fielv value
            var valueToUpdate=new object();
            if (parentFieldNameToUpdate != null && parentFieldNameToUpdate != "")
            {
                Entity retrievedEntity = (Entity)service.Retrieve(parentEntityType, new Guid(parentEntityId), new ColumnSet(parentFieldNameToUpdate));
                if (retrievedEntity.Attributes.Contains(parentFieldNameToUpdate))
                {
                    valueToUpdate = retrievedEntity.Attributes[parentFieldNameToUpdate];
                }
                else
                {
                    valueToUpdate = null;
                }
            }
            else
            {
                valueToUpdate = setValueToUpdate;
            }

            //3) update each child record

            foreach (Entity child in retrieved.Entities)
            {
                Entity entUpdate = new Entity(childEntityType);
                entUpdate.Id = child.Id;
                entUpdate.Attributes.Add(childFieldNameToUpdate, valueToUpdate);
                service.Update(entUpdate);
            }


        }

    }
}
