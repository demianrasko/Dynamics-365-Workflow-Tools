using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Workflow;
using msdyncrmWorkflowTools;
using System;
using System.Activities;
using System.Collections.Generic;

namespace msdyncrmWorkflowTools_Tests
{
    [TestClass]
    public class AIActivities_Tests
    {
        [TestMethod]
        public void AITranslateText_ReturnsTranslatedText()
        {
            var service = new FakeOrganizationService();
            service.Handler = request =>
            {
                Assert.AreEqual("AITranslate", request.RequestName);
                Assert.AreEqual("fr", request["TargetLanguage"]);
                var response = new OrganizationResponse();
                response.Results["TranslatedText"] = "Bonjour";
                return response;
            };

            var outputs = InvokeActivity(
                new AITranslateText(),
                new Dictionary<string, object>
                {
                    {"TextToTranslate", "Hello"},
                    {"TargetLanguage", "fr"}
                },
                service);

            Assert.AreEqual("Bonjour", outputs["TranslatedText"]);
            Assert.IsFalse((bool)outputs["Failed"]);
            Assert.AreEqual(string.Empty, outputs["FailureMessage"]);
            Assert.AreEqual(1, service.NonMetadataExecuteCount);
        }

        [TestMethod]
        public void AITranslateText_WithEmptyTextSetsFailure()
        {
            var service = new FakeOrganizationService();

            var outputs = InvokeActivity(
                new AITranslateText(),
                new Dictionary<string, object>
                {
                    {"TextToTranslate", string.Empty}
                },
                service);

            Assert.IsTrue((bool)outputs["Failed"]);
            Assert.AreEqual("Text is empty.", outputs["FailureMessage"]);
            Assert.AreEqual(0, service.NonMetadataExecuteCount);
        }

        [TestMethod]
        public void AIClassifyText_UsesDistinctCategories()
        {
            var service = new FakeOrganizationService();
            service.Handler = request =>
            {
                CollectionAssert.AreEqual(new[] { "Sales", "Support" }, (string[])request["Categories"]);
                var response = new OrganizationResponse();
                response.Results["Classification"] = "Sales";
                return response;
            };

            var outputs = InvokeActivity(
                new AIClassifyText(),
                new Dictionary<string, object>
                {
                    {"TextToClassify", "Need help closing deals"},
                    {"CategoriesCsv", "Sales,Support, sales"}
                },
                service);

            Assert.AreEqual("Sales", outputs["TopCategory"]);
            Assert.IsFalse((bool)outputs["Failed"]);
            Assert.AreEqual(string.Empty, outputs["FailureMessage"]);
        }

        [TestMethod]
        public void AIDraftReply_ReturnsPreparedResponse()
        {
            var service = new FakeOrganizationService();
            service.Handler = request =>
            {
                Assert.AreEqual("AIReply", request.RequestName);
                Assert.AreEqual("Thanks for reaching out", request["Text"]);
                var response = new OrganizationResponse();
                response.Results["PreparedResponse"] = "Automated reply";
                return response;
            };

            var outputs = InvokeActivity(
                new AIDraftReply(),
                new Dictionary<string, object>
                {
                    {"TextToReplyTo", "Thanks for reaching out"}
                },
                service);

            Assert.AreEqual("Automated reply", outputs["ReplyText"]);
            Assert.IsFalse((bool)outputs["Failed"]);
        }

        [TestMethod]
        public void AISentimentDetect_ReturnsSentiment()
        {
            var service = new FakeOrganizationService();
            service.Handler = request =>
            {
                var response = new OrganizationResponse();
                response.Results["AnalyzedSentiment"] = "positive";
                return response;
            };

            var outputs = InvokeActivity(
                new AISentimentDetect(),
                new Dictionary<string, object>
                {
                    {"TextToAnalyzeSentiment", "This product is fantastic"}
                },
                service);

            Assert.AreEqual("positive", outputs["Sentiment"]);
            Assert.IsFalse((bool)outputs["Failed"]);
        }

        [TestMethod]
        public void AISummarizeText_ReturnsSummary()
        {
            var service = new FakeOrganizationService();
            service.Handler = request =>
            {
                var response = new OrganizationResponse();
                response.Results["SummarizedText"] = "Brief summary";
                return response;
            };

            var outputs = InvokeActivity(
                new AISummarizeText(),
                new Dictionary<string, object>
                {
                    {"TextToSummarize", "Long content"}
                },
                service);

            Assert.AreEqual("Brief summary", outputs["SummaryText"]);
            Assert.IsFalse((bool)outputs["Failed"]);
        }

        [TestMethod]
        public void AISummarizeRecord_ParsesUrlAndCallsService()
        {
            var recordId = Guid.NewGuid();
            var service = new FakeOrganizationService();
            service.AddMetadataResponse(1, "account");
            service.Handler = request =>
            {
                Assert.AreEqual("AISummarizeRecord", request.RequestName);
                Assert.AreEqual("account", request["EntityLogicalName"]);
                Assert.AreEqual(recordId.ToString(), request["Id"]);
                Assert.IsTrue((bool)request["IsMergedCatchupAndSummary"]);
                Assert.AreEqual("{\"foo\":\"bar\"}", request["RecordContext"]);
                var response = new OrganizationResponse();
                response.Results["SummarizedText"] = "Record summary";
                return response;
            };

            var outputs = InvokeActivity(
                new AISummarizeRecord(),
                new Dictionary<string, object>
                {
                    {"RecordUrl", $"https://demo.crm.dynamics.com/main.aspx?etc=1&id={recordId}"},
                    {"IncludeCatchup", true},
                    {"RecordContextJson", "{\"foo\":\"bar\"}"}
                },
                service);

            Assert.AreEqual("Record summary", outputs["SummaryText"]);
            Assert.IsFalse((bool)outputs["Failed"]);
            Assert.AreEqual(string.Empty, outputs["FailureMessage"]);
            Assert.AreEqual(1, service.NonMetadataExecuteCount);
        }

        private static IDictionary<string, object> InvokeActivity(CodeActivity activity, IDictionary<string, object> inputs, FakeOrganizationService service)
        {
            var tracing = new FakeTracingService();
            var workflowContext = new FakeWorkflowContext();
            var factory = new FakeOrganizationServiceFactory(service);

            var invoker = new WorkflowInvoker(activity);
            invoker.Extensions.Add(() => tracing);
            invoker.Extensions.Add(() => workflowContext);
            invoker.Extensions.Add(() => factory);

            return invoker.Invoke(inputs);
        }

        private class FakeTracingService : ITracingService
        {
            public List<string> Messages { get; } = new List<string>();

            public void Trace(string format, params object[] args)
            {
                Messages.Add(args == null || args.Length == 0 ? format : string.Format(format, args));
            }
        }

        private class FakeOrganizationServiceFactory : IOrganizationServiceFactory
        {
            private readonly IOrganizationService service;

            public FakeOrganizationServiceFactory(IOrganizationService service)
            {
                this.service = service;
            }

            public IOrganizationService CreateOrganizationService(Guid? userId)
            {
                return service;
            }
        }

        private class FakeOrganizationService : IOrganizationService
        {
            private readonly Dictionary<int, string> logicalNamesByTypeCode = new Dictionary<int, string>();

            public Func<OrganizationRequest, OrganizationResponse> Handler { get; set; }

            public int NonMetadataExecuteCount { get; private set; }

            public OrganizationRequest LastRequest { get; private set; }

            public void AddMetadataResponse(int typeCode, string logicalName)
            {
                logicalNamesByTypeCode[typeCode] = logicalName;
            }

            public OrganizationResponse Execute(OrganizationRequest request)
            {
                if (request is RetrieveMetadataChangesRequest)
                {
                    return BuildMetadataResponse();
                }

                NonMetadataExecuteCount++;
                LastRequest = request;
                if (Handler != null)
                {
                    return Handler(request);
                }

                return new OrganizationResponse();
            }

            private RetrieveMetadataChangesResponse BuildMetadataResponse()
            {
                var entityMetadataCollection = new EntityMetadataCollection();
                foreach (var kvp in logicalNamesByTypeCode)
                {
                    var metadata = new EntityMetadata
                    {
                        LogicalName = kvp.Value,
                        ObjectTypeCode = kvp.Key
                    };

                    entityMetadataCollection.Add(metadata);
                }

                var response = new RetrieveMetadataChangesResponse
                {
                    Results = { ["EntityMetadata"] = entityMetadataCollection }
                };

                return response;
            }

            #region Not Implemented Members
            public Guid Create(Entity entity) => throw new NotImplementedException();
            public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet) => throw new NotImplementedException();
            public void Update(Entity entity) => throw new NotImplementedException();
            public void Delete(string entityName, Guid id) => throw new NotImplementedException();
            public EntityCollection RetrieveMultiple(QueryBase query) => throw new NotImplementedException();
            public void Associate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities) => throw new NotImplementedException();
            public void Disassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities) => throw new NotImplementedException();
            #endregion
        }

        private class FakeWorkflowContext : IWorkflowContext
        {
            public Guid BusinessUnitId => Guid.NewGuid();
            public Guid CorrelationId => Guid.NewGuid();
            public int Depth => 1;
            public Guid InitiatingUserId => Guid.NewGuid();
            public ParameterCollection InputParameters { get; } = new ParameterCollection();
            public bool IsExecutingOffline => false;
            public bool IsOfflinePlayback => false;
            public bool IsInTransaction => false;
            public Guid MessageId => Guid.NewGuid();
            public string MessageName => "Test";
            public int Mode => 0;
            public Guid OperationId => Guid.NewGuid();
            public DateTime OperationCreatedOn => DateTime.UtcNow;
            public Guid OrganizationId => Guid.NewGuid();
            public string OrganizationName => "org";
            public Guid PrimaryEntityId => Guid.Empty;
            public string PrimaryEntityName => string.Empty;
            public EntityReference PrimaryEntityReference => new EntityReference(PrimaryEntityName, PrimaryEntityId);
            public ParameterCollection OutputParameters { get; } = new ParameterCollection();
            public ParameterCollection SharedVariables { get; } = new ParameterCollection();
            public string SecondaryEntityName => string.Empty;
            public Guid UserId => Guid.NewGuid();
            public EntityReference OwningExtension => null;
            public Guid OwningExtensionId => Guid.NewGuid();
            public string OwningExtensionLogicalName => string.Empty;
            public Guid ParentContextId => Guid.Empty;
            public Guid RequestId => Guid.NewGuid();
            public string StageName => string.Empty;
            public int WorkflowCategory => 0;
            public int WorkflowMode => 0;
            public Guid? OwningUserId => null;
            public Guid? OwningBusinessUnitId => null;
            public Guid? InitiatingBusinessUnitId => null;
        }
    }
}
