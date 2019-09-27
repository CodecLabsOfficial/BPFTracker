using CodecLabs.BPFTracking.Workflows.Enums;
using CodecLabs.BPFTracking.Workflows.Helper;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace CodecLabs.BPFTracking.Workflows
{
    public sealed class RegisterPluginImage : CodeActivity
    {
        [RequiredArgument]
        [Input("Plugin Step Id")]
        public InArgument<string> PluginStepId { get; set; }

        [RequiredArgument]
        [Input("Attributes")]
        [Default("i.e. name,accountnumber,contactid")]
        public InArgument<string> Attributes { get; set; }

        [RequiredArgument]
        [Input("Image Type")]
        public InArgument<int> ImageType { get; set; }

        [RequiredArgument]
        [Input("Entity Alias")]
        [Default("i.e. account")]
        public InArgument<string> EntityAlias { get; set; }

        [RequiredArgument]
        [Input("Name")]
        [Default("i.e. PreImage")]
        public InArgument<string> Name { get; set; }

        [RequiredArgument]
        [Input("Message")]
        [Default("i.e. Update")]
        public InArgument<string> Message { get; set; }

        [RequiredArgument]
        [Input("Customization Level")]
        public InArgument<int> CustomizationLevel { get; set; }

        [Output("ImageId")]
        public OutArgument<string> ImageId { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();

            //Create an Organization Service
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService orgService = serviceFactory.CreateOrganizationService(context.InitiatingUserId);

            //Registered Step Guid
            var pluginStepGuid = Guid.Empty;

            var message = Message.Get(executionContext);

            //Plugin Image object
            CrmPluginImage pluginImage = new CrmPluginImage();
            //pluginImage.AssemblyId = new Guid("");
            //pluginImage.PluginId = new Guid("");
            pluginImage.StepId = new Guid(PluginStepId.Get<string>(executionContext));
            pluginImage.Attributes = Attributes.Get<string>(executionContext);
            pluginImage.ImageType = (CrmPluginImageType)ImageType.Get<int>(executionContext);
            pluginImage.EntityAlias = EntityAlias.Get<string>(executionContext);
            pluginImage.MessagePropertyName = message.Equals("Create") ? "Id" : "Target";
            pluginImage.Name = Name.Get<string>(executionContext);
            pluginImage.CustomizationLevel = CustomizationLevel.Get<int>(executionContext);

            var imageId = pluginImage.CreatePluginImage(ref orgService);

            ImageId.Set(executionContext, imageId.ToString());
        }
    }
}
