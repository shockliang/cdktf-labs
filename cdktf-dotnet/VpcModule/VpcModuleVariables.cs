using System.Collections.Generic;
using HashiCorp.Cdktf.Providers.Aws.Vpc;

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
        /// Name to be used on the default security group
        /// </summary>
        public string DefaultSecurityGroupName { get; set; } = "";
        
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
        /// Should be true if you want to specify a DHCP options set with a custom domain name, DNS servers, NTP servers, netbios servers, and/or netbios server type
        /// </summary>
        public bool EnableDhcpOptions { get; set; } = false;

        /// <summary>
        /// Specifies DNS name for DHCP options set (requires enable_dhcp_options set to true)
        /// </summary>
        public string DhcpOptionsDomainName { get; set; } = "";
        
        /// <summary>
        /// Specify a list of DNS server addresses for DHCP options set, default to AWS provided (requires enable_dhcp_options set to true)
        /// </summary>
        public IList<string> DhcpOptionsDomainNameServers { get; set; } = new List<string> { "AmazonProvidedDNS" };

        /// <summary>
        /// Specify a list of NTP servers for DHCP options set (requires enable_dhcp_options set to true)
        /// </summary>
        public IList<string> DhcpOptionsNtpServers { get; set; } = new List<string>();

        /// <summary>
        /// Specify a list of netbios servers for DHCP options set (requires enable_dhcp_options set to true)
        /// </summary>
        public IList<string> DhcpOptionsNetbiosNameServers { get; set; } = new List<string>();

        /// <summary>
        /// Specify netbios node_type for DHCP options set (requires enable_dhcp_options set to true)
        /// </summary>
        public string DhcpOptionsNetbiosNodeType { get; set; } = "";

        /// <summary>
        /// Additional tags for the DHCP option set (requires enable_dhcp_options set to true)
        /// </summary>
        public IDictionary<string, string> DhcpOptionsTags { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Controls if an Internet Gateway is created for public subnets and the related routes that connect them.
        /// </summary>
        public bool CreateIgw { get; set; } = true;
        
        /// <summary>
        /// Controls if an Egress Only Internet Gateway is created and its related routes.
        /// </summary>
        public bool CreateEgressOnlyIgw { get; set; } = true;

        /// <summary>
        /// Additional tags for the internet gateway
        /// </summary>
        public IDictionary<string, string> IgwTags { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Should be true to manage default route table
        /// </summary>
        public bool ManageDefaultRouteTable { get; set; } = false;
        
        /// <summary>
        /// List of virtual gateways for propagation
        /// </summary>
        public IList<string> DefaultRouteTablePropagatingVgws { get; set; } = new List<string>();

        /// <summary>
        /// Name to be used on the default route table
        /// </summary>
        public string DefaultRouteTableName { get; set; } = "";

        /// <summary>
        /// Additional tags for the default route table
        /// </summary>
        public IDictionary<string, string> DefaultRouteTableTags { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Configuration block of routes. See https://registry.terraform.io/providers/hashicorp/aws/latest/docs/resources/default_route_table#route
        /// </summary>
        public IList<DefaultRouteTableRoute> DefaultRouteTableRoutes { get; set; } = new List<DefaultRouteTableRoute>();

        /// <summary>
        /// Suffix to append to public subnets name
        /// </summary>
        public string PublicSubnetSuffix { get; set; } = "public";

        public IDictionary<string, string> PublicRouteTableTags = new Dictionary<string, string>();

        /// <summary>
        /// Should be true to adopt and manage default security group
        /// </summary>
        public bool ManageDefaultSecurityGroup { get; set; } = false;

        /// <summary>
        /// Should be true if you want to provision a single shared NAT Gateway across all of your private networks
        /// </summary>
        public bool SingleNatGateway { get; set; } = false;

        /// <summary>
        /// Should be true if you want only one NAT Gateway per availability zone. Requires `var.azs` to be set, and the number of `public_subnets` created to be greater than or equal to the number of availability zones specified in `var.azs`.
        /// </summary>
        public bool OneNatGatewayPerAz { get; set; } = false;

        /// <summary>
        /// Suffix to append to private subnets name
        /// </summary>
        public string PrivateSubnetSuffix { get; set; } = "private";
        
        /// <summary>
        /// Additional tags for the private route tables
        /// </summary>
        public IDictionary<string, string> PrivateRouteTableTags = new Dictionary<string, string>();

        /// <summary>
        /// Controls if separate route table for database should be created
        /// </summary>
        public bool CreateDatabaseSubnetRouteTable { get; set; } = false;

        /// <summary>
        /// Controls if an internet gateway route for public database access should be created
        /// </summary>
        public bool CreateDatabaseInternetGatewayRoute { get; set; } = false;

        /// <summary>
        /// Controls if a nat gateway route should be created to give internet access to the database subnets
        /// </summary>
        public bool CreateDatabaseNatGatewayRoute { get; set; } = false;

        /// <summary>
        /// Suffix to append to database subnets name
        /// </summary>
        public string DatabaseSubnetSuffix { get; set; } = "db";

        /// <summary>
        /// Additional tags for the database route tables
        /// </summary>
        public IDictionary<string, string> DatabaseRouteTableTags = new Dictionary<string, string>();

        /// <summary>
        /// Controls if separate route table for redshift should be created
        /// </summary>
        public bool CreateRedshiftSubnetRouteTable { get; set; } = false;

        /// <summary>
        /// Suffix to append to redshift subnets name
        /// </summary>
        public string RedshiftSubnetSuffix { get; set; } = "redshift";

        /// <summary>
        /// Additional tags for the redshift route tables
        /// </summary>
        public IDictionary<string, string> RedshiftRouteTableTags = new Dictionary<string, string>();

        /// <summary>
        /// Controls if separate route table for elasticache should be created
        /// </summary>
        public bool CreateElasticacheSubnetRouteTable { get; set; } = false;

        /// <summary>
        /// Suffix to append to elasticache subnets name
        /// </summary>
        public string ElasticacheSubnetSuffix { get; set; } = "elasticache";

        /// <summary>
        /// Additional tags for the elasticache route tables
        /// </summary>
        public IDictionary<string, string> ElasticacheRouteTableTags { get; set; } =
            new Dictionary<string, string>();

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

        /// <summary>
        /// Additional tags for the default security group
        /// </summary>
        public IDictionary<string, string> DefaultSecurityGroupTags = new Dictionary<string, string>();

        /// <summary>
        /// A list of public subnets inside the VPC
        /// </summary>
        public IList<string> PublicSubnets { get; set; } = new List<string>();
        
        /// <summary>
        /// A list of private subnets inside the VPC
        /// </summary>
        public IList<string> PrivateSubnets { get; set; } = new List<string>();
        
        /// <summary>
        /// A list of elasticache subnets
        /// </summary>
        public IList<string> ElasticacheSubnets { get; set; } = new List<string>();

        /// <summary>
        /// A list of outpost subnets inside the VPC
        /// </summary>
        public IList<string> OutpostSubnets { get; set; } = new List<string>();

        /// <summary>
        /// A list of database subnets
        /// </summary>
        public IList<string> DatabaseSubnets { get; set; } = new List<string>();

        /// <summary>
        /// A list of redshift subnets
        /// </summary>
        public IList<string> RedshiftSubnets { get; set; } = new List<string>();

        /// <summary>
        /// A list of intra subnets
        /// </summary>
        public IList<string> IntraSubnets { get; set; } = new List<string>();

        /// <summary>
        /// Suffix to append to intra subnets name
        /// </summary>
        public string IntraSubnetSuffix { get; set; } = "intra";

        /// <summary>
        /// Additional tags for the intra route tables
        /// </summary>
        public IDictionary<string, string> IntraRouteTableTags = new Dictionary<string, string>();

        /// <summary>
        /// A list of availability zones names or ids in the region
        /// </summary>
        public IList<string> Azs { get; set; } = new List<string>();

        /// <summary>
        /// List of secondary CIDR blocks to associate with the VPC to extend the IP Address pool
        /// </summary>
        public IList<string> SecondaryCidrBlocks { get; set; } = new List<string>();

        /// <summary>
        /// List of maps of ingress rules to set on the default security group
        /// </summary>
        public IList<DefaultSecurityGroupIngress> DefaultSecurityGroupIngresses { get; set; } =
            new List<DefaultSecurityGroupIngress>();

        /// <summary>
        /// List of maps of egress rules to set on the default security group
        /// </summary>
        public IList<DefaultSecurityGroupEgress> DefaultSecurityGroupEgresses { get; set; } =
            new List<DefaultSecurityGroupEgress>();

    }
}