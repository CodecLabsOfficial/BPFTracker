using Microsoft.Xrm.Sdk;
using System;

namespace CodecLabs.BPFTracking.Workflows.Extensions
{
    public static class EnumExtensions
    {
        public static OptionSetValue ToOptionSetValue(this Enum _enum)
        {
            return _enum != null ? new OptionSetValue(_enum.GetHashCode()) : null;
        }
    }
}
