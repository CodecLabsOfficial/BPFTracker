using CodecLabs.BPFTracking.Plugins.Helper;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace CodecLabs.BPFTracking.Plugins
{
    public class BPFStageChanged : IPlugin
    {
        public string _secureString { get; set; }
        public string _unsecureString { get; set; }

        ITracingService _tracingService;

        public BPFStageChanged(string unsecureString, string secureString)
        {
            this._secureString = secureString;
            this._unsecureString = unsecureString;
        }

        public void Execute(IServiceProvider serviceProvider)
        {
            //initialize context and service
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
            _tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            Entity target = (Entity)context.InputParameters["Target"];
            bool isCreate = context.MessageName.Equals("Create");

            var image = isCreate ? (Entity)context.PostEntityImages["PostImage"] : (Entity)context.PreEntityImages["PreImage"];

            // Check if cross-entity and not the current one
            if (!IsCorrectCrossEntity(isCreate, target, image))
            {
                return;
            }
            else
            {
                var historyTBC = GetHistoryRecord(service, isCreate, image, target);

                if (historyTBC != null)
                {
                    UpsertHistoryRecord(service, isCreate, historyTBC, target);
                }
            }
        }

        private bool IsCorrectCrossEntity(bool isCreate, Entity target, Entity image)
        {
            if (target.Contains("statecode") && !isCreate)
            {
                return true;
            }

            if (target.Attributes.Count > 1)
            {
                var hasTargetOOB = SDKHelper.AttributeExists(target.Attributes, $"{_unsecureString}id");
                var hasImageOOB = SDKHelper.AttributeExists(image.Attributes, $"{_unsecureString}id");
                var hasTargetCustom = SDKHelper.AttributeExists(target.Attributes, $"bpf_{_unsecureString}id");
                var hasImageCustom = SDKHelper.AttributeExists(image.Attributes, $"bpf_{_unsecureString}id");

                var oobExists = hasTargetOOB || hasImageOOB;

                var customExists = hasTargetCustom || hasImageCustom;

                if (ValidateDuplicateUpdate(isCreate, oobExists, customExists, hasTargetOOB, hasImageOOB, hasTargetCustom, hasImageCustom))
                {
                    return false;
                }

                return oobExists || customExists;
            }

            return true;
        }

        private bool ValidateDuplicateUpdate(bool isCreate, bool oobExists, bool customExists, bool hasTargetOOB, bool hasImageOOB, bool hasTargetCustom, bool hasImageCustom)
        {
            if (isCreate) { return false; }

            if ((oobExists && hasTargetOOB && !hasImageOOB) || (customExists && hasTargetCustom && !hasImageCustom))
            {
                return false;
            }

            return false;
        }

        private void UpsertHistoryRecord(IOrganizationService service, bool isCreate, Entity historyTBC, Entity target)
        {
            var latestHistoryId = GetLatestHistoryId(service, target.LogicalName, target.Id);
            var addedGuid = new Guid();

            if (!target.Contains("statecode") || isCreate)
            {
                addedGuid = SDKHelper.Create(service, historyTBC);
            }

            if (!isCreate && addedGuid != default(Guid))
            {
                if (latestHistoryId != default(Guid))
                {
                    UpdateLatestHistory(service, latestHistoryId);
                }
            }
        }

        private void UpdateLatestHistory(IOrganizationService service, Guid latestHistoryId)
        {
            Entity historyTBU = new Entity("clabs_bpfchangehistory", latestHistoryId);
            historyTBU.Attributes.Add("clabs_enddate", DateTime.Now);

            service.Update(historyTBU);
        }

        private Guid GetLatestHistoryId(IOrganizationService service, string targetName, Guid targetId)
        {
            var finalTargetName = targetName.Contains("_") ? targetName : $"clabs_{targetName}";

            var entityName = "clabs_bpfchangehistory";
            var searchFields = new string[] { $"{finalTargetName}id" };
            var searchValues = new object[] { targetId };
            var columns = new ColumnSet("clabs_bpfchangehistoryid", "createdon");
            var desc = "createdon";

            var result = SDKHelper.RetrieveEntityCollection(service, entityName, searchFields, searchValues, columns, ConditionOperator.Equal, desc);

            _tracingService.Trace($"Result Count: {result.Entities.Count}");

            if (result.Entities.Count > 0)
            {
                _tracingService.Trace($"Result Id: {result[0].Id.ToString()}");
                return result[0].Id;
            }

            return new Guid();
        }

        private Entity GetHistoryRecord(IOrganizationService service, bool isCreate, Entity image, Entity target)
        {
            var mainEntity = isCreate || target.Contains("statecode") ? GetStageFromReference(image.GetAttributeValue<EntityReference>("activestageid")) : RetrieveStage(service, target);
            if (_unsecureString != mainEntity.GetAttributeValue<string>("primaryentitytypecode")) { return null; }

            var mainLookup = _unsecureString.Contains("_") ? $"bpf_{_unsecureString}id" : $"{_unsecureString}id";
            var historyLookup = _unsecureString.Contains("_") ? $"{_unsecureString}id" : $"clabs_{_unsecureString}id";
            var targetFinalName = target.LogicalName.Contains("_") ? target.LogicalName : $"clabs_{target.LogicalName}";

            Entity ret = new Entity("clabs_bpfchangehistory");
            DateTime startedDate = target.GetAttributeValue<DateTime>("activestagestartedon");
            var process = image.GetAttributeValue<EntityReference>("processid");
            var activeStageName = mainEntity.GetAttributeValue<string>("stagename");
            EntityReference mainEntityLookup = GetMainEntityLookup(image, target, mainLookup);

            if (mainEntityLookup == null)
            {
                return null;
            }

            ret.Attributes.Add("clabs_startdate", startedDate);
            ret.Attributes.Add("clabs_name", $"{_unsecureString} - {activeStageName}");
            ret.Attributes.Add("clabs_stagename", activeStageName);
            ret.Attributes.Add("clabs_processname", process.Name);
            ret.Attributes.Add(historyLookup, mainEntityLookup);
            ret.Attributes.Add($"{targetFinalName}id", target.ToEntityReference());

            return ret;
        }

        private static EntityReference GetMainEntityLookup(Entity image, Entity target, string mainLookup)
        {
            return image.GetAttributeValue<EntityReference>(mainLookup)
                ?? target.GetAttributeValue<EntityReference>(mainLookup)
                ?? image.GetAttributeValue<EntityReference>($"bpf_{mainLookup}")
                ?? target.GetAttributeValue<EntityReference>($"bpf_{mainLookup}");
        }

        private Entity RetrieveStage(IOrganizationService service, Entity target)
        {
            if (target.GetAttributeValue<EntityReference>("activestageid") == null)
            {
                return null;
            }

            Entity entity = service.Retrieve("processstage", target.GetAttributeValue<EntityReference>("activestageid").Id, new ColumnSet("stagename", "primaryentitytypecode"));

            return entity;
        }

        private Entity GetStageFromReference(EntityReference entRef)
        {
            Entity ret = new Entity("processstage", entRef.Id);
            ret.Attributes.Add("stagename", entRef.Name);
            ret.Attributes.Add("primaryentitytypecode", _unsecureString);

            return ret;
        }
    }
}