using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Text;

namespace CodecLabs.BPFTracking.Workflows.Helper
{
    public class CrmPluginStep
    {
        #region Data Members

        private int customizationLevel;
        private Boolean deleteAsyncOperationIfSuccessful;
        private Entity step = new Entity("sdkmessageprocessingstep");
        private int deployment;
        private string eventHandler;
        private string filteringAttributes;
        private Guid impersonatingUserId;
        private int invocationSource;
        private string message;
        private Guid messageEntityId;
        private Guid messageId;
        private int mode;
        private string name;
        private Guid pluginId;
        private string primaryEntity;
        private int rank;
        private string secureConfiguration;
        private Guid secureConfigurationId;
        private int stage;
        private Guid stepId;
        private string unsecureConfiguration;
        private string pluginAssemblyName;
        public ITracingService tracer;

        #endregion Data Members

        #region Properties

        public int CustomizationLevel
        {
            get { return customizationLevel; }
            set { customizationLevel = value; }
        }
        public Boolean DeleteAsyncOperationIfSuccessful
        {
            get { return deleteAsyncOperationIfSuccessful; }
            set { deleteAsyncOperationIfSuccessful = value; }
        }
        public int Deployment
        {
            get { return deployment; }
            set { deployment = value; }
        }
        public string EventHandler
        {
            get { return eventHandler; }
            set { eventHandler = value; }
        }
        public string FilteringAttributes
        {
            get { return filteringAttributes; }
            set { filteringAttributes = value; }
        }
        public Guid ImpersonatingUserId
        {
            get { return impersonatingUserId; }
            set { impersonatingUserId = value; }
        }
        public int InvocationSource
        {
            get { return invocationSource; }
            set { invocationSource = value; }
        }
        public string Message
        {
            get { return message; }
            set { message = value; }
        }
        public Guid MessageEntityId
        {
            get { return messageEntityId; }
            set { messageEntityId = value; }
        }
        public Guid MessageId
        {
            get { return messageId; }
            set { messageId = value; }
        }
        public int Mode
        {
            get { return mode; }
            set { mode = value; }
        }
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        public Guid PluginId
        {
            get { return pluginId; }
            set { pluginId = value; }
        }
        public string PrimaryEntity
        {
            get { return primaryEntity; }
            set { primaryEntity = value; }
        }
        public int Rank
        {
            get { return rank; }
            set { rank = value; }
        }
        public string SecureConfiguration
        {
            get { return secureConfiguration; }
            set { secureConfiguration = value; }
        }
        public Guid SecureConfigurationId
        {
            get { return secureConfigurationId; }
            set { secureConfigurationId = value; }
        }
        public int Stage
        {
            get { return stage; }
            set { stage = value; }
        }
        public Guid StepId
        {
            get { return stepId; }
            set { stepId = value; }
        }
        public string UnsecureConfiguration
        {
            get { return unsecureConfiguration; }
            set { unsecureConfiguration = value; }
        }
        public string PluginAssemblyName
        {
            get { return pluginAssemblyName; }
            set { pluginAssemblyName = value; }
        }

        #endregion Properties

        #region Methods

        public CrmPluginStep()
        {
        }

        public Guid RegisterPluginStep(ref IOrganizationService service)
        {
            tracer.Trace("--- SetMessageId ---");
            SetMessageId(ref service);

            tracer.Trace("--- SetMessageEntityId ---");
            SetMessageEntityId(ref service);

            tracer.Trace("--- SetPluginId ---");
            SetPluginId(ref service);

            step["sdkmessageid"] = new EntityReference("sdkmessage", this.MessageId);
            tracer.Trace("sdkmessageid: " + this.MessageId);

            step["sdkmessagefilterid"] = new EntityReference("sdkmessage", this.MessageEntityId);
            tracer.Trace("sdkmessagefilterid: " + this.MessageEntityId);

            step["eventhandler"] = new EntityReference("plugintype", this.PluginId);
            tracer.Trace("sdkmessagefilterid: " + this.MessageEntityId);

            step["name"] = this.Name;
            tracer.Trace("name: " + this.Name);

            step["mode"] = new OptionSetValue(this.Mode);
            tracer.Trace("mode: " + this.Mode);

            step["rank"] = this.Rank;
            tracer.Trace("rank: " + this.Rank);

            step["invocationsource"] = new OptionSetValue(this.InvocationSource);
            tracer.Trace("invocationsource: " + this.InvocationSource);

            step["stage"] = new OptionSetValue(this.Stage);
            tracer.Trace("stage: " + this.Stage);

            step["supporteddeployment"] = new OptionSetValue((int)this.Deployment);
            tracer.Trace("supporteddeployment: " + this.Deployment);

            if (string.IsNullOrEmpty(this.FilteringAttributes))
            {
                step["filteringattributes"] = string.Empty;
            }
            else
            {
                step["filteringattributes"] = this.FilteringAttributes;
            }

            tracer.Trace("filteringattributes: " + (string)step["filteringattributes"]);

            tracer.Trace("--- Create Step ---");
            return service.Create(step);
        }

        public void UpdatePluginStep(ref IOrganizationService service)
        {
            step.Id = this.StepId;
            step["configuration"] = this.UnsecureConfiguration;

            service.Update(step);
        }

        public void UnregisterPluginStep(ref IOrganizationService service, Guid stepId)
        {
            service.Delete("sdkmessageprocessingstep", stepId);
        }

        public void EnablePluginStep(ref IOrganizationService service, Guid stepId)
        {
            TogglePluginStepState(service, true, stepId);
        }

        public void DisablePluginStep(ref IOrganizationService service, Guid stepId)
        {
            TogglePluginStepState(service, false, stepId);
        }

        public void DeletePlugin(ref IOrganizationService service, Guid pluginId)
        {
            service.Delete("plugintype", pluginId);
        }

        public Guid RetrieveStepId(ref IOrganizationService service, string stepName)
        {
            Entity step = RetrieveEntity(ref service, "sdkmessageprocessingstep", new string[] { "name" }, new string[] { stepName }, new ColumnSet(true), ConditionOperator.Equal);
            if (step != null)
            {
                return step.Id;
            }
            else
            {
                return Guid.Empty;
            }
        }

        public EntityCollection RetrieveStepsByPluginId(ref IOrganizationService service, Guid pluginId)
        {
            EntityCollection steps = RetrieveEntityCollection(ref service, "sdkmessageprocessingstep", new string[] { "plugintypeid" }, new string[] { pluginId.ToString() }, new ColumnSet(true), ConditionOperator.Equal);

            if (steps != null)
            {
                return steps;
            }
            else
            {
                return null;
            }
        }

        public Guid RetrievePluginId(ref IOrganizationService service, string pluginName)
        {
            Entity plugin = RetrieveEntity(ref service, "plugintype", new string[] { "name" }, new string[] { pluginName }, new ColumnSet(true), ConditionOperator.Equal);

            if (plugin != null)
            {
                return plugin.Id;
            }
            else
            {
                return Guid.Empty;
            }
        }

        private Entity RetrieveEntity(ref IOrganizationService service, string entityName, string[] entitySearchField, object[] entitySearchFieldValue, ColumnSet columnSet, ConditionOperator op)
        {
            Entity entity = null;

            QueryExpression query = new QueryExpression();
            query.EntityName = entityName;
            FilterExpression filter = new FilterExpression();

            for (int i = 0; i < entitySearchField.Length; i++)
            {
                ConditionExpression condition = new ConditionExpression();
                condition.AttributeName = entitySearchField[i];
                condition.Operator = op;
                condition.Values.Add(entitySearchFieldValue[i]);
                filter.FilterOperator = LogicalOperator.And;
                filter.AddCondition(condition);
            }

            query.ColumnSet = columnSet;
            query.Criteria = filter;

            EntityCollection collection;

            try
            {
                collection = service.RetrieveMultiple(query);
            }
            catch (Exception ex)
            {
                throw new Exception("Step Registration Error - " + ex.Message);
            }

            if (collection.Entities.Count == 1)
            {
                entity = (Entity)collection.Entities[0];
            }

            return entity;
        }

        private EntityCollection RetrieveEntityCollection(ref IOrganizationService service, string entityName, string[] entitySearchField, object[] entitySearchFieldValue, ColumnSet columnSet, ConditionOperator op)
        {
            EntityCollection entity = null;

            QueryExpression query = new QueryExpression();
            query.EntityName = entityName;

            FilterExpression filter = new FilterExpression();

            for (int i = 0; i < entitySearchField.Length; i++)
            {
                ConditionExpression condition = new ConditionExpression();
                condition.AttributeName = entitySearchField[i];
                condition.Operator = op;
                condition.Values.Add(entitySearchFieldValue[i]);
                filter.FilterOperator = LogicalOperator.And;
                filter.AddCondition(condition);
            }

            query.ColumnSet = columnSet;
            query.Criteria = filter;

            EntityCollection collection;

            try
            {
                collection = service.RetrieveMultiple(query);
            }
            catch (Exception ex)
            {
                throw new Exception("Step Registration Error - " + ex.Message);
            }

            if (collection.Entities.Count > 0)
            {
                entity = collection;
            }

            return entity;
        }

        private void TogglePluginStepState(IOrganizationService orgService, bool enable, Guid stepId)
        {
            var qe = new QueryExpression("sdkmessageprocessingstep");
            qe.ColumnSet.AddColumns("sdkmessageprocessingstepid", "name");

            int pluginStateCode = enable ? 0 : 1;
            int pluginStatusCode = enable ? 1 : 2;

            orgService.Execute(new SetStateRequest
            {
                EntityMoniker = new EntityReference("sdkmessageprocessingstep", stepId),
                State = new OptionSetValue(pluginStateCode),
                Status = new OptionSetValue(pluginStatusCode)
            });
        }

        private void SetMessageEntityId(ref IOrganizationService service)
        {
            Entity message = RetrieveEntity(ref service, "sdkmessagefilter", new string[] { "sdkmessageid", "primaryobjecttypecode" }, new string[] { MessageId.ToString(), PrimaryEntity.ToLower() }, new ColumnSet(true), ConditionOperator.Equal);

            if (message != null)
            {
                MessageEntityId = message.Id;
            }
        }

        private void SetMessageId(ref IOrganizationService service)
        {
            Entity message = RetrieveEntity(ref service, "sdkmessage", new string[] { "name" }, new string[] { Message.ToLower() }, new ColumnSet(true), ConditionOperator.Equal);

            if (message != null)
            {
                MessageId = message.Id;
            }
        }

        private void SetPluginId(ref IOrganizationService service)
        {
            try
            {
                Entity pluginAssembly = RetrieveEntity(ref service, "pluginassembly", new string[] { "name" }, new string[] { PluginAssemblyName }, new ColumnSet(true), ConditionOperator.Equal);

                EntityCollection pluginTypes = RetrieveEntityCollection(ref service, "plugintype", new string[] { "typename" }, new string[] { EventHandler }, new ColumnSet(true), ConditionOperator.Equal);

                foreach (var entity in pluginTypes.Entities)
                {
                    if (((EntityReference)entity["pluginassemblyid"]).Id == pluginAssembly.Id)
                    {
                        PluginId = entity.Id;
                    }
                }
            }
            catch (Exception e)
            {
                throw new InvalidPluginExecutionException("(SetPluginId) Error: " + e.Message);
            }
        }

        #endregion Methods
    }
}
