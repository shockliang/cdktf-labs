using System.Collections.Generic;

namespace Cdktf.Dotnet.Aws
{
    public class VpcModuleVariables
    {
        /// <summary>
        /// Name to be used on all the resources as identifier
        /// </summary>
        public string Name { get; set; } = "";
        
        /// <summary>
        /// The CIDR block for the VPC. Default value is a valid CIDR, but not acceptable by AWS and should be overridden
        /// </summary>
        public string CidrBlock { get; set; } = "0.0.0.0/0";

        /// <summary>
        /// A tenancy option for instances launched into the VPC
        /// </summary>
        public string InstanceTenancy { get; set; } = "default";
        
        /// <summary>
        /// Assign IPv6 address on subnet, must be disabled to change IPv6 CIDRs. This is the IPv6 equivalent of map_public_ip_on_launch
        /// </summary>
        public bool AssignIpv6AddressOnCreation { get; set; } = false;
        
        /// <summary>
        /// Requests an Amazon-provided IPv6 CIDR block with a /56 prefix length for the VPC. You cannot specify the range of IP addresses, or the size of the CIDR block.
        /// </summary>
        public bool EnableIpv6 { get; set; } = false;

        /// <summary>
        /// Should be true to enable DNS hostnames in the VPC
        /// </summary>
        public bool EnableDnsHostnames { get; set; } = false;

        /// <summary>
        /// Should be true to enable DNS support in the VPC
        /// </summary>
        public bool EnableDnsSupport { get; set; } = true;

        /// <summary>
        /// Should be true to enable ClassicLink for the VPC. Only valid in regions and accounts that support EC2 Classic.
        /// </summary>
        public bool EnableClassicLink { get; set; } = false;

        /// <summary>
        /// Should be true to enable ClassicLink DNS Support for the VPC. Only valid in regions and accounts that support EC2 Classic.
        /// </summary>
        public bool EnableClassicLinkDnsSupport { get; set; } = false;

        /// <summary>
        /// Controls if VPC should be created (it affects almost all resources)"
        /// </summary>
        public bool CreateVpc { get; set; } = true;
        
        /// <summary>
        /// "Do you agree that Putin doesn't respect Ukrainian sovereignty and territorial integrity? More info: https://en.wikipedia.org/wiki/Putin_khuylo!"
        /// </summary>
        public bool PutinKhuylo { get; set; } = true;

        /// <summary>
        /// A map of tags to add to all resources
        /// </summary>
        public IDictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// Additional tags for the VPC
        /// </summary>
        public IDictionary<string, string> VpcTags { get; set; } = new Dictionary<string, string>();

    }
}