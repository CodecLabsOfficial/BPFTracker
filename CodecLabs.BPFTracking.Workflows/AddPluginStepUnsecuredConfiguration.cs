using CodecLabs.BPFTracking.Workflows.Helper;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace CodecLabs.BPFTracking.Workflows
{
    public sealed class AddPluginStepUnsecuredConfiguration : CodeActivity
    {
        [RequiredArgument]
        [Input("Plugin Step Id")]
        public InArgument<string> PluginStepId { get; set; }

        [RequiredArgument]
        [Input("Unsecure Configuration")]
        public InArgument<string> UnsecureConfiguration { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();

            //Create an Organization Service
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService orgService = serviceFactory.CreateOrganizationService(context.InitiatingUserId);

            //Registered Step Guid
            var pluginStepGuid = Guid.Empty;

            //Plugin Step object
            CrmPluginStep pluginStep = new CrmPluginStep();

            pluginStep.StepId = new Guid(PluginStepId.Get<string>(executionContext));
            pluginStep.UnsecureConfiguration = UnsecureConfiguration.Get<string>(executionContext);

            //Update Step
            pluginStep.UpdatePluginStep(ref orgService);
        }
    }
}
