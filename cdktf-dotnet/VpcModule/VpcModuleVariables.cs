namespace Cdktf.Dotnet.Aws
{
    public partial class VpcModule
    {
        /// <summary>
        /// Assign IPv6 address on subnet, must be disabled to change IPv6 CIDRs. This is the IPv6 equivalent of map_public_ip_on_launch
        /// </summary>
        public bool AssignIpv6AddressOnCreation { get; set; } = false;
        
        /// <summary>
        /// The CIDR block for the VPC. Default value is a valid CIDR, but not acceptable by AWS and should be overridden
        /// </summary>
        public string CidrBlock { get; set; } = "0.0.0.0/0";
    }
}