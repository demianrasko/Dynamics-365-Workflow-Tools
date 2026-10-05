using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace msdyncrmWorkflowTools
{
    public partial class Common
    {
        /// <summary>
        /// Recalculates the prices of an opportunity, quote, order or invoice.
        /// </summary>
        public void CalculatePrice(EntityReference target)
        {
            Trace($"Calculating the price of {target.LogicalName} {target.Id}");
            Service.Execute(new CalculatePriceRequest { Target = target });
        }

        /// <summary>
        /// Qualifies a lead, optionally creating an account, contact and opportunity (in the organization's base
        /// currency, for an existing account or contact when one is given).
        /// </summary>
        public void QualifyLead(EntityReference lead, bool createAccount, bool createContact, bool createOpportunity,
            EntityReference existingAccount, EntityReference existingContact, int status)
        {
            var request = new QualifyLeadRequest
            {
                LeadId = new EntityReference(EntityNames.Lead, lead.Id),
                CreateAccount = createAccount,
                CreateContact = createContact,
                CreateOpportunity = createOpportunity,
                OpportunityCurrencyId = (EntityReference)GetOrganizationSetting(AttributeNames.BaseCurrencyId),
                Status = new OptionSetValue(status)
            };

            if (existingAccount != null)
            {
                request.OpportunityCustomerId = new EntityReference(EntityNames.Account, existingAccount.Id);
            }
            else if (existingContact != null)
            {
                request.OpportunityCustomerId = new EntityReference(EntityNames.Contact, existingContact.Id);
            }

            Trace($"Qualifying lead {lead.Id}");
            Service.Execute(request);
        }

        /// <summary>
        /// Adds a marketing list to a campaign.
        /// </summary>
        public void AddListToCampaign(Guid listId, Guid campaignId)
        {
            Trace($"Adding marketing list {listId} to campaign {campaignId}");
            Service.Execute(new AddItemCampaignRequest { CampaignId = campaignId, EntityId = listId, EntityName = EntityNames.List });
        }

        /// <summary>
        /// Copies the members of one marketing list to another.
        /// </summary>
        public void CopyListMembers(Guid sourceListId, Guid targetListId)
        {
            Trace($"Copying members of marketing list {sourceListId} to {targetListId}");
            Service.Execute(new CopyMembersListRequest { SourceListId = sourceListId, TargetListId = targetListId });
        }

        /// <summary>
        /// Converts a dynamic marketing list to a static one.
        /// </summary>
        public void CopyDynamicListToStatic(Guid listId)
        {
            Trace($"Copying dynamic marketing list {listId} to a static list");
            Service.Execute(new CopyDynamicListToStaticRequest { ListId = listId });
        }

        /// <summary>
        /// Creates a quote, with its products, from an opportunity.
        /// </summary>
        /// <returns>The new quote.</returns>
        public EntityReference CreateQuoteFromOpportunity(Guid opportunityId)
        {
            var response = (GenerateQuoteFromOpportunityResponse)Service.Execute(new GenerateQuoteFromOpportunityRequest
            {
                OpportunityId = opportunityId,
                ColumnSet = new ColumnSet(AttributeNames.QuoteId, AttributeNames.Name)
            });

            Trace($"Quote {response.Entity.Id} created from opportunity {opportunityId}");

            return response.Entity.ToEntityReference();
        }

        /// <summary>
        /// Closes a quote as won.
        /// </summary>
        /// <param name="quote">The quote.</param>
        /// <param name="subject">Subject of the quote close activity.</param>
        public void WinQuote(EntityReference quote, string subject)
        {
            Trace($"Winning quote {quote.Id}");

            Service.Execute(new WinQuoteRequest
            {
                QuoteClose = new Entity(EntityNames.QuoteClose)
                {
                    [AttributeNames.Subject] = subject,
                    [AttributeNames.QuoteId] = quote
                },
                Status = new OptionSetValue(-1)
            });
        }

        /// <summary>
        /// Resolves a case (status reason Problem Solved).
        /// </summary>
        public void ResolveCase(Guid incidentId, string subject, string description)
        {
            Trace($"Resolving case {incidentId}");

            Service.Execute(new CloseIncidentRequest
            {
                IncidentResolution = new Entity(EntityNames.IncidentResolution)
                {
                    [AttributeNames.IncidentId] = new EntityReference(EntityNames.Incident, incidentId),
                    [AttributeNames.Subject] = subject,
                    [AttributeNames.Description] = description
                },
                Status = new OptionSetValue(5)
            });
        }

        public Guid CreateOpportunityProduct(EntityReference opportunity,
            EntityReference existingProduct, EntityReference uom, decimal quantity)
        {
            var opportunityProduct = new Entity(EntityNames.OpportunityProduct)
            {
                [AttributeNames.OpportunityId] = new EntityReference(opportunity.LogicalName, opportunity.Id),
                [AttributeNames.ProductId] = new EntityReference(existingProduct.LogicalName, existingProduct.Id),
                [AttributeNames.UomId] = new EntityReference(uom.LogicalName, uom.Id),
                [AttributeNames.Quantity] = quantity
            };

            return Service.Create(opportunityProduct);
        }

        /// <summary>
        /// Whether a record (account, contact or lead) is a member of a marketing list.
        /// </summary>
        /// <param name="listId">The marketing list.</param>
        /// <param name="memberId">The record to look for.</param>
        public bool IsMemberOfMarketingList(Guid listId, Guid memberId)
        {
            return Service.RetrieveMultiple(Queries.MarketingListMembership(listId, memberId)).Entities.Count > 0;
        }

        /// <summary>
        /// Adds a record (account, contact or lead) to a marketing list.
        /// </summary>
        public void AddToMarketingList(Guid listId, EntityReference member)
        {
            Trace($"Adding {member.LogicalName} {member.Id} to marketing list {listId}");

            Service.Execute(new AddMemberListRequest
            {
                ListId = listId,
                EntityId = member.Id
            });
        }

        /// <summary>
        /// Removes a record (account, contact or lead) from a marketing list.
        /// </summary>
        public void RemoveFromMarketingList(Guid listId, Guid memberId)
        {
            Trace($"Removing {memberId} from marketing list {listId}");

            Service.Execute(new RemoveMemberListRequest
            {
                ListId = listId,
                EntityId = memberId
            });
        }

        /// <summary>
        /// Removes an account, contact or lead from every marketing list it is a member of.
        /// </summary>
        /// <returns>The number of lists the record was removed from.</returns>
        /// <exception cref="InvalidPluginExecutionException">The record is not an account, contact or lead.</exception>
        public int RemoveFromAllMarketingLists(EntityReference member)
        {
            if (member.LogicalName != EntityNames.Account && member.LogicalName != EntityNames.Contact && member.LogicalName != EntityNames.Lead)
            {
                throw new InvalidPluginExecutionException("Remove From All Marketing Lists only supports account, contact or lead records.");
            }

            var memberships = Service.RetrieveMultiple(Queries.MarketingListMemberships(member.Id)).Entities;

            foreach (var membership in memberships)
            {
                RemoveFromMarketingList(membership.GetAttributeValue<EntityReference>(AttributeNames.ListId).Id, member.Id);
            }

            Trace($"Removed {member.LogicalName} {member.Id} from {memberships.Count} marketing list(s).");

            return memberships.Count;
        }

        /// <summary>
        /// Recalculates a goal now instead of waiting for the server's scheduled recalculation.
        /// </summary>
        /// <param name="goalId">The goal to recalculate.</param>
        public void RecalculateGoal(Guid goalId)
        {
            Trace($"Recalculating goal {goalId}");

            var request = new RecalculateRequest
            {
                Target = new EntityReference(EntityNames.Goal, goalId)
            };

            Service.Execute(request);
        }
    }
}
