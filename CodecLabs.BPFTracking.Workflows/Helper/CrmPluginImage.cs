using CodecLabs.BPFTracking.Workflows.Enums;
using CodecLabs.BPFTracking.Workflows.Extensions;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodecLabs.BPFTracking.Workflows.Helper
{
    public class CrmPluginImage
    {
        #region Data Members

        private Entity stepImage = new Entity("sdkmessageprocessingstepimage");
        private Guid assemblyId;
        private Guid pluginId;
        private Guid stepId;
        private Guid imageId;
        private string attributes;
        private CrmPluginImageType imageType;
        private string entityAlias;
        private string messagePropertyName;
        private int customizationLevel;
        private string name;

        #endregion Data Members

        #region Properties

        public Guid AssemblyId
        {
            get { return assemblyId; }
            set { assemblyId = value; }
        }

        public Guid PluginId
        {
            get { return pluginId; }
            set { pluginId = value; }
        }

        public Guid StepId
        {
            get { return stepId; }
            set { stepId = value; }
        }

        public Guid ImageId
        {
            get { return imageId; }
            set { imageId = value; }
        }

        public string Attributes
        {
            get { return attributes; }
            set { attributes = value; }
        }

        public CrmPluginImageType ImageType
        {
            get { return imageType; }
            set { imageType = value; }
        }

        public string EntityAlias
        {
            get { return entityAlias; }
            set { entityAlias = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public string MessagePropertyName
        {
            get { return messagePropertyName; }
            set { messagePropertyName = value; }
        }

        public int CustomizationLevel
        {
            get { return customizationLevel; }
            set { customizationLevel = value; }
        }

        #endregion

        #region Methods

        public CrmPluginImage()
        {
        }

        public Guid CreatePluginImage(ref IOrganizationService Service)
        {
            stepImage["sdkmessageprocessingstepid"] = new EntityReference("sdkmessageprocessingstep", this.StepId);
            stepImage["name"] = this.Name;
            stepImage["attributes"] = this.Attributes;
            stepImage["imagetype"] = this.ImageType.ToOptionSetValue();
            stepImage["messagepropertyname"] = this.MessagePropertyName;
            stepImage["entityalias"] = this.EntityAlias;
            stepImage["customizationlevel"] = this.CustomizationLevel;

            return Service.Create(stepImage);
        }

        #endregion
    }
}
