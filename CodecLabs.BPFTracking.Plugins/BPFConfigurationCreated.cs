using CodecLabs.BPFTracking.Plugins.Helper;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CodecLabs.BPFTracking.Plugins
{
    public class BPFConfigurationCreated : IPlugin
    {
        public IOrganizationService _service;

        public void Execute(IServiceProvider serviceProvider)
        {
            //initialize context and service
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            _service = serviceFactory.CreateOrganizationService(context.UserId);

            if (!CheckContext(context)) return;

            Entity config = (Entity)context.InputParameters["Target"];

            if (!CheckAttributes(config)) return;

            try
            {
                //get config fields
                string entityLogicalName = config.GetAttributeValue<string>("clabs_mainentitylogicalname");
                EntityReference bpfRef = config.GetAttributeValue<EntityReference>("clabs_processid");
                string bPFLogicalName = DataHelper.RetrieveBPFUniqueName(_service, bpfRef);

                var createStep = CreateStep(_service, entityLogicalName, bPFLogicalName, true);
                var UpdateStep = CreateStep(_service, entityLogicalName, bPFLogicalName, false);
                var linkEntities = LinkEntities(_service, entityLogicalName, bPFLogicalName);

                ExecuteMultipleRequest multipleReq = null;

                if (linkEntities != null)
                    multipleReq = BuildMultipleRequest(createStep, UpdateStep, linkEntities);
                else
                    multipleReq = BuildMultipleRequest(createStep, UpdateStep);

                if (multipleReq != null)
                {
                    var responses = ExecuteMultipleRequest(multipleReq);
                    ThrowExecuteMultipleFirstError(responses);
                }
            }
            catch (Exception e)
            {
                var updateConfig = new Entity(config.LogicalName, config.Id);

                updateConfig["clabs_ispluginsuccessful"] = false;
                updateConfig["clabs_errormessage"] = e.Message.Length > 999 ? e.Message.Substring(0, 999) : e.Message;

                _service.Update(updateConfig);
            }

        }

        private OrganizationRequest CreateStep(IOrganizationService service, string entityLogicalName, string bPFLogicalName, bool isUpdate)
        {
            var lookupName = GetBPFLookup(service, bPFLogicalName, entityLogicalName);

            Dictionary<string, object> actionParams = new Dictionary<string, object>();
            actionParams.Add("UnsecureConfiguration", entityLogicalName);
            actionParams.Add("PrimaryEntity", bPFLogicalName);
            actionParams.Add("PluginAssemblyName", "CodecLabs.BPFTracking.Plugins");
            actionParams.Add("EventHandler", "CodecLabs.BPFTracking.Plugins.BPFStageChanged");
            actionParams.Add("Mode", 0);
            actionParams.Add("Rank", 100);
            actionParams.Add("InvocationSource", 0);
            actionParams.Add("Deployment", 0);
            actionParams.Add("ImageCustomizationLevel", 0);
            actionParams.Add("ImageAttributes", $"activestageid,activestagestartedon,{lookupName},processid");

            if (isUpdate)
            {
                actionParams.Add("Message", "Update");
                actionParams.Add("Stage", 20);
                actionParams.Add("ImageType", 0);
                actionParams.Add("ImageEntityAlias", "PreImage");
                actionParams.Add("ImageName", "PreImage");
                actionParams.Add("UpdateFields", "activestageid,statecode");
            }
            else
            {
                actionParams.Add("Message", "Create");
                actionParams.Add("Stage", 40);
                actionParams.Add("ImageType", 1);
                actionParams.Add("ImageEntityAlias", "PostImage");
                actionParams.Add("ImageName", "PostImage");
            }

            return SDKHelper.BuildCallAction(service, "clabs_CodecLabsRegisterPluginStep", actionParams);
        }

        private string GetBPFLookup(IOrganizationService service, string bPFLogicalName, string entityLogicalName)
        {
            return bPFLogicalName.Contains("_") ? $"bpf_{entityLogicalName}id" : $"{entityLogicalName}id";
            //var bpfMetadata = SDKHelper.GetEntityMetadata(service, bPFLogicalName);
            //return SDKHelper.AttributeExists(bpfMetadata.Attributes, $"bpf_{entityLogicalName}id") ? $"bpf_{entityLogicalName}id" : $"{entityLogicalName}id";
        }

        private bool CheckContext(IPluginExecutionContext context)
        {
            //do sanity checks
            if (context.Depth > 1)
            {
                return false;
            }

            if (context.PrimaryEntityName != "clabs_bpftrackconfiguration" || context.MessageName != "Create")
            {
                throw new Exception("Wrong plugin registration");
            }

            if (!context.InputParameters.Contains("Target"))
            {
                throw new Exception("Plugin target is missing");
            }

            return true;
        }

        private static bool CheckAttributes(Entity config)
        {
            if (!config.Attributes.Contains("clabs_mainentitylogicalname"))
            {
                throw new Exception("Entity logical name is missing");
            }

            if (!config.Attributes.Contains("clabs_processid"))
            {
                throw new Exception("BPF logical name is missing");
            }

            return true;
        }

        private CreateOneToManyRequest LinkEntities(IOrganizationService service, string entityLogicalName, string bPFLogicalName)
        {
            var finalEntityLogicalName = entityLogicalName.Contains("_") ? entityLogicalName : $"clabs_{entityLogicalName}";
            var finalBpfLogicalName = bPFLogicalName.Contains("_") ? bPFLogicalName : $"clabs_{bPFLogicalName}";

            bool entityFieldExist = CheckFieldExists(service, "clabs_bpfchangehistory", $"{finalEntityLogicalName}id");
            bool bpfFieldExist = CheckFieldExists(service, "clabs_bpfchangehistory", $"{finalBpfLogicalName}id");

            //if (entityFieldExist) { throw new Exception($"Field already exist: {entityLogicalName}id"); }
            //if (bpfFieldExist) { throw new Exception($"Field already exist: {bPFLogicalName}id"); }

            if (!entityFieldExist)
            {
                //Create lookup on history entity to tracked entity
                return BuildCreateLookup(service, entityLogicalName);
            }

            if (!bpfFieldExist)
            {
                //Create lookup on history entity to bpf entity
                return BuildCreateLookup(service, bPFLogicalName);
            }

            return null;
        }

        private ExecuteMultipleRequest BuildMultipleRequest(params OrganizationRequest[] requests)
        {
            var req = new ExecuteMultipleRequest()
            {
                Settings = new ExecuteMultipleSettings()
                {
                    ContinueOnError = false,
                    ReturnResponses = true
                },
                Requests = new OrganizationRequestCollection()
            };

            req.Requests.AddRange(requests);

            return req;
        }

        private ExecuteMultipleResponse ExecuteMultipleRequest(ExecuteMultipleRequest req)
        {
            return (ExecuteMultipleResponse)_service.Execute(req);
        }

        private void ThrowExecuteMultipleFirstError(ExecuteMultipleResponse responses)
        {
            if (responses.Responses.Any(x => x.Fault != null))
            {
                var firstError = responses.Responses.Where(x => x.Fault != null).FirstOrDefault().Fault.Message;
                throw new InvalidPluginExecutionException(firstError);
            }
        }

        private static OrganizationResponse CreateLookup(IOrganizationService service, string entityLogicalName)
        {
            var finalEntityLogicalName = entityLogicalName.Contains("_") ? entityLogicalName : $"clabs_{entityLogicalName}";

            CreateOneToManyRequest createTrackedEtntiyLookupRequest =
                        new CreateOneToManyRequest
                        {
                            OneToManyRelationship =
                                new OneToManyRelationshipMetadata
                                {
                                    ReferencedEntity = entityLogicalName, //populate tracked entity name
                                    ReferencingEntity = "clabs_bpfchangehistory",
                                    SchemaName = string.Format("{0}_clabs_bpfchangehistory_createdbyplugin", finalEntityLogicalName),
                                    AssociatedMenuConfiguration = new AssociatedMenuConfiguration
                                    {
                                        Behavior = AssociatedMenuBehavior.UseLabel,
                                        Group = AssociatedMenuGroup.Details,
                                        Label = new Label("History", 1033),
                                        Order = 10000
                                    },
                                    CascadeConfiguration = new CascadeConfiguration
                                    {
                                        Assign = CascadeType.NoCascade,
                                        Delete = CascadeType.RemoveLink,
                                        Merge = CascadeType.NoCascade,
                                        Reparent = CascadeType.NoCascade,
                                        Share = CascadeType.NoCascade,
                                        Unshare = CascadeType.NoCascade
                                    }
                                },
                            Lookup = new LookupAttributeMetadata
                            {
                                SchemaName = finalEntityLogicalName + "id",
                                DisplayName = new Label(entityLogicalName, 1033),
                                RequiredLevel = new AttributeRequiredLevelManagedProperty(AttributeRequiredLevel.None),
                                Description = new Label(entityLogicalName, 1033)
                            }
                        };

            return service.Execute(createTrackedEtntiyLookupRequest);
        }

        private static CreateOneToManyRequest BuildCreateLookup(IOrganizationService service, string entityLogicalName)
        {
            var finalEntityLogicalName = entityLogicalName.Contains("_") ? entityLogicalName : $"clabs_{entityLogicalName}";

            CreateOneToManyRequest createTrackedEtntiyLookupRequest =
                        new CreateOneToManyRequest
                        {
                            OneToManyRelationship =
                                new OneToManyRelationshipMetadata
                                {
                                    ReferencedEntity = entityLogicalName, //populate tracked entity name
                                    ReferencingEntity = "clabs_bpfchangehistory",
                                    SchemaName = string.Format("{0}_clabs_bpfchangehistory_createdbyplugin", finalEntityLogicalName),
                                    AssociatedMenuConfiguration = new AssociatedMenuConfiguration
                                    {
                                        Behavior = AssociatedMenuBehavior.UseLabel,
                                        Group = AssociatedMenuGroup.Details,
                                        Label = new Label("History", 1033),
                                        Order = 10000
                                    },
                                    CascadeConfiguration = new CascadeConfiguration
                                    {
                                        Assign = CascadeType.NoCascade,
                                        Delete = CascadeType.RemoveLink,
                                        Merge = CascadeType.NoCascade,
                                        Reparent = CascadeType.NoCascade,
                                        Share = CascadeType.NoCascade,
                                        Unshare = CascadeType.NoCascade
                                    }
                                },
                            Lookup = new LookupAttributeMetadata
                            {
                                SchemaName = finalEntityLogicalName + "id",
                                DisplayName = new Label(entityLogicalName, 1033),
                                RequiredLevel = new AttributeRequiredLevelManagedProperty(AttributeRequiredLevel.None),
                                Description = new Label(entityLogicalName, 1033)
                            }
                        };

            return createTrackedEtntiyLookupRequest;
        }

        private bool CheckFieldExists(IOrganizationService service, string entityName, string fieldName)
        {
            var metadata = SDKHelper.GetEntityMetadata(service, entityName);
            return SDKHelper.AttributeExists(metadata.Attributes, fieldName);
        }

        private static void CreateStep(IOrganizationService service)
        {
            Guid messageId = new Guid("9EBDBB1B-EA3E-DB11-86A7-000A3A5473E8");

            Guid messageFitlerId = new Guid("C2C5BB1B-EA3E-DB11-86A7-000A3A5473E8");

            Entity step = new Entity("sdkmessageprocessingstep");
            step["name"] = "Sdk Message track history test";
            step["configuration"] = "BPF history tracking";

            step["invocationsource"] = new OptionSetValue(0);
            step["sdkmessageid"] = new EntityReference("sdkmessage", messageId);

            step["supporteddeployment"] = new OptionSetValue(0);
            step["plugintypeid"] = new EntityReference("plugintype", new Guid("f41913ff-fa4d-43b0-afd7-098ca99427b5"));

            step["mode"] = new OptionSetValue(0);
            step["rank"] = 1;
            step["stage"] = new OptionSetValue(20);

            step["sdkmessagefilterid"] = new EntityReference("sdkmessagefilter", messageFitlerId);
            Guid stepId = service.Create(step);
        }
    }
}