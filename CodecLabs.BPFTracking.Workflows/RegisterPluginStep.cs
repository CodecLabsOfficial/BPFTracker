using CodecLabs.BPFTracking.Workflows.Helper;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace CodecLabs.BPFTracking.Workflows
{
    public sealed class RegisterPluginStep : CodeActivity
    {
        [RequiredArgument]
        [Input("Primary Entity (Schema Name)")]
        [Default("i.e. account")]
        public InArgument<string> PrimaryEntity { get; set; }

        [RequiredArgument]
        [Input("Assembly Name")]
        [Default("i.e. AssemblyName.AssemblyName")]
        public InArgument<string> AssemblyName { get; set; }

        [RequiredArgument]
        [Input("Update - Filtering Attributes")]
        [Default("i.e. name,accountnumber")]
        public InArgument<string> FilteringAttributes { get; set; }

        [RequiredArgument]
        [Input("Event Handler")]
        [Default("i.e. AssemblyName.AssemblyName.EventHandler")]
        public InArgument<string> EventHandler { get; set; }

        [RequiredArgument]
        [Input("Mode (Synchronous = 0, Asynchronous = 1)")]
        [Default("0")]
        public InArgument<int> Mode { get; set; }

        [RequiredArgument]
        [Input("Rank")]
        [Default("100")]
        public InArgument<int> Rank { get; set; }

        [RequiredArgument]
        [Input("Invocation Source (Parent = 0, Child = 1)")]
        [Default("0")]
        public InArgument<int> InvocationSource { get; set; }

        [RequiredArgument]
        [Input("Stage (PreValidation = 10, PreOperation = 20, PostOperation = 40, PostOperationDeprecated = 50)")]
        [Default("20")]
        public InArgument<int> Stage { get; set; }

        [RequiredArgument]
        [Input("Deployment (ServerOnly = 0, OfflineOnly = 1, Both = 2)")]
        [Default("0")]
        public InArgument<int> Deployment { get; set; }

        [RequiredArgument]
        [Input("Message (Assign, Create, Delete, GrantAccess, ModifyAccess, Retrieve, RetrieveMultiple, RetrievePrincipalAccess, RetrieveSharedPrincipalsAndAccess, RevokeAccess, SetState, SetStateDynamicEntity, Update)")]
        [Default("i.e. Create")]
        public InArgument<string> Message { get; set; }

        [Output("Plugin Step Guid")]
        public OutArgument<string> PluginStepGuid { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            ITracingService tracer = executionContext.GetExtension<ITracingService>();

            tracer.Trace("Inicialize context");
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();

            //Create an Organization Service
            tracer.Trace("Inicialize serviceFactory");
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();

            tracer.Trace("Inicialize orgService");
            IOrganizationService orgService = serviceFactory.CreateOrganizationService(context.InitiatingUserId);

            //Registered Step Guid
            var pluginStepGuid = Guid.Empty;

            try
            {
                //Plugin Step object
                tracer.Trace("Inicialize pluginStep");
                CrmPluginStep pluginStep = new CrmPluginStep();

                pluginStep.PrimaryEntity = PrimaryEntity.Get<string>(executionContext);
                tracer.Trace("PrimaryEntity: " + pluginStep.PrimaryEntity);

                pluginStep.PluginAssemblyName = AssemblyName.Get<string>(executionContext);
                tracer.Trace("PluginAssemblyName: " + pluginStep.PluginAssemblyName);

                pluginStep.EventHandler = EventHandler.Get<string>(executionContext);
                tracer.Trace("EventHandler: " + pluginStep.EventHandler);

                pluginStep.Mode = Mode.Get<int>(executionContext);
                tracer.Trace("Mode: " + pluginStep.Mode);

                pluginStep.Rank = Rank.Get<int>(executionContext);
                tracer.Trace("Rank: " + pluginStep.Rank);

                pluginStep.FilteringAttributes = FilteringAttributes.Get<string>(executionContext);
                tracer.Trace("FilteringAttributes: " + pluginStep.FilteringAttributes);

                pluginStep.InvocationSource = InvocationSource.Get<int>(executionContext);
                tracer.Trace("InvocationSource: " + pluginStep.InvocationSource);

                pluginStep.Stage = Stage.Get<int>(executionContext);
                tracer.Trace("Stage: " + pluginStep.Stage);

                pluginStep.Deployment = Deployment.Get<int>(executionContext);
                tracer.Trace("Deployment: " + pluginStep.Deployment);

                pluginStep.Message = Message.Get<string>(executionContext);
                tracer.Trace("Message: " + pluginStep.Message);

                pluginStep.Name = pluginStep.EventHandler + ": " + pluginStep.Message + " of " + pluginStep.PrimaryEntity;
                tracer.Trace("Name: " + pluginStep.Name);

                pluginStep.tracer = tracer;

                tracer.Trace("--- Register Plugin Step --- ");
                //Register Step
                pluginStepGuid = pluginStep.RegisterPluginStep(ref orgService);

                tracer.Trace("pluginStepGuid: " + pluginStepGuid);
                PluginStepGuid.Set(executionContext, pluginStepGuid.ToString());
            }
            catch (Exception e)
            {
                throw new InvalidPluginExecutionException("(RegisterPluginStep) Error! " + e.Message);
            }
        }
    }
}
