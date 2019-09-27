using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodecLabs.BPFTracking.Plugins.Helper
{
    internal class DataHelper
    {
        internal static string RetrieveBPFUniqueName(IOrganizationService service, EntityReference bpfRef)
        {
            string logicalName = bpfRef.LogicalName;
            Guid processId = bpfRef.Id;
            ColumnSet columns = new ColumnSet("uniquename");

            Entity result = service.Retrieve(logicalName, processId, columns);
            return result.GetAttributeValue<string>("uniquename");
        }
    }
}
