using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
namespace msdyncrmWorkflowTools.Class
{
    public class SendEmailToUsersInRole : CodeActivity
    {
        [Input("Security Role")]
        [RequiredArgument]
        [ReferenceTarget("role")]
        public InArgument<EntityReference> SecurityRoleLookup
        {
            get;
            set;
        }

        [Input("Email Template")]
        [RequiredArgument]
        [ReferenceTarget("template")]
        public InArgument<EntityReference> EmailTemplateLookup
        {
            get;
            set;
        }



        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            EntityReference securityRoleLookup = this.SecurityRoleLookup.Get(executionContext);
            objCommon.tracingService.Trace(String.Format("marketingList: {0} ", securityRoleLookup.Id.ToString()));

            EntityReference emailTemplateLookup = this.EmailTemplateLookup.Get(executionContext);
            objCommon.tracingService.Trace(String.Format("campaign: {0} ", emailTemplateLookup.Id.ToString()));


            #endregion
            objCommon.tracingService.Trace("Init");

            var userList = objCommon.service.RetrieveMultiple(new FetchExpression(BuildFetchXml(securityRoleLookup.Id)));
            objCommon.tracingService.Trace("Retrieved Data");
            
            foreach (var user in userList.Entities)
            {
                try
                {
                    objCommon.tracingService.Trace("user creating email");
                    Entity entity = CreateEmailFromTemplate(objCommon.service, emailTemplateLookup, user.Id);

                    objCommon.tracingService.Trace("created email: {0}", entity.Id);
                    SendEmail(objCommon.service, entity);
                }
                catch (System.Exception ex)
                {
                    objCommon.tracingService.Trace("error:"+ex.ToString());
                }
            }


        }

        public void SendEmail(IOrganizationService service, Entity entity)
        {
            var sendEmailRequest = new SendEmailRequest
            {
                EmailId = entity.Id,
                TrackingToken = String.Empty,
                IssueSend = true
            };

            service.Execute(sendEmailRequest);
        }

        public Entity CreateEmailFromTemplate(IOrganizationService service, EntityReference template, Guid userId)
        {
            var request = new InstantiateTemplateRequest
            {
                TemplateId = template.Id,
                ObjectId = userId,
                ObjectType = "systemuser"
            };

            var response = (InstantiateTemplateResponse)service.Execute(request);

            var entity = response.EntityCollection.Entities.FirstOrDefault();

            if (entity == null)
            {
                throw new Exception(String.Format("Unable to create an email from the {0} template.", template.Name));
            }

            return entity;
        }

        private string BuildFetchXml(Guid roleId)
        {
            const string fetchXml =
                    @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                      <entity name='systemuser'>
                        <attribute name='systemuserid' />
                            <filter type='and'>
                             <condition attribute='accessmode' operator='eq' value='0' />
                            </filter>
                        <link-entity name='systemuserroles' from='systemuserid' to='systemuserid' visible='false' intersect='true'>
                          <link-entity name='role' from='roleid' to='roleid' alias='aa'>
                            <filter type='and'>
                              <condition attribute='roleid' operator='eq' uitype='role' value='{0}' />
                            </filter>
                          </link-entity>
                        </link-entity>
                      </entity>
                    </fetch>";

            return string.Format(fetchXml, roleId);
        }
    }
}
