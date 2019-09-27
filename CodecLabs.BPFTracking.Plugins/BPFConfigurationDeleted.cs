using CodecLabs.BPFTracking.Plugins.Helper;
using CodecLabs.BPFTracking.Workflows.Helper;
using Microsoft.Xrm.Sdk;
using System;
using System.Linq;

namespace CodecLabs.BPFTracking.Plugins
{
    public class BPFConfigurationDeleted : IPlugin
    {
        CrmPluginStep _pluginStepHelper;

        public void Execute(IServiceProvider serviceProvider)
        {
            //initialize context and service
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
            _pluginStepHelper = new CrmPluginStep();

            var preImage = (Entity)context.PreEntityImages["PreImage"];

            Guid pluginId = _pluginStepHelper.RetrievePluginId(ref service, "CodecLabs.BPFTracking.Plugins.BPFStageChanged");
            EntityCollection steps = _pluginStepHelper.RetrieveStepsByPluginId(ref service, pluginId);
            DisableSteps(service, preImage, steps);
        }

        private void DisableSteps(IOrganizationService service, Entity preImage, EntityCollection steps)
        {
            string mainEntityName = preImage.GetAttributeValue<string>("clabs_mainentitylogicalname");
            EntityReference bpfRef = preImage.GetAttributeValue<EntityReference>("clabs_processid");
            string bpfLogicalName = DataHelper.RetrieveBPFUniqueName(service, bpfRef);

            var mainEntitySteps = steps.Entities.Where(step => step.GetAttributeValue<string>("configuration").Equals(mainEntityName)
                && step.GetAttributeValue<string>("name").Contains(bpfLogicalName));

            var updateStep = mainEntitySteps.Where(step => step.FormattedValues["sdkmessageid"].Equals("Update")).FirstOrDefault();
            var createStep = mainEntitySteps.Where(step => step.FormattedValues["sdkmessageid"].Equals("Create")).FirstOrDefault();

            if (updateStep != null)
            {
                _pluginStepHelper.UnregisterPluginStep(ref service, updateStep.Id);
            }

            if (createStep != null)
            {
                _pluginStepHelper.UnregisterPluginStep(ref service, createStep.Id);
            }
        }
    }
}