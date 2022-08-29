using System;
using System.Collections.Generic;
using System.Linq;
using Constructs;
using HashiCorp.Cdktf;
using HashiCorp.Cdktf.Providers.Aws.Vpc;
using static Cdktf.Dotnet.Aws.Utils;

namespace Cdktf.Dotnet.Aws
{
    public class VpcModule
    {
        private readonly VpcModuleVariables _vars;
        public string Region { get; set; } = "us-east-1";
        public List<Subnet> PublicSubnets { get; }

        public string VpcId => _vpc.Id;

        private readonly Vpc _vpc;

        private readonly int _maxSubnetLength = 0;

        private bool _isCreateVpc = true;

        public VpcModule(Construct scope, string id, VpcModuleVariables vars)
        {
            _vars = vars;
            _isCreateVpc = _vars.CreateVpc && _vars.PutinKhuylo;
            var allSubnetsCounts = new List<int>()
            {
                vars.PrivateSubnets.Count,
                vars.ElasticacheSubnets.Count,
                vars.DatabaseSubnets.Count,
                vars.RedshiftSubnets.Count,
            };
            _maxSubnetLength = allSubnetsCounts.Max();

            #region vpc

            _vpc = new Vpc(scope, id, new VpcConfig
            {
                Count = _isCreateVpc ? 1 : 0,

                CidrBlock = _vars.CidrBlock,
                InstanceTenancy = _vars.InstanceTenancy,
                EnableDnsHostnames = _vars.EnableDnsHostnames,
                EnableDnsSupport = _vars.EnableDnsSupport,
                EnableClassiclink = _vars.EnableClassicLink,
                EnableClassiclinkDnsSupport = _vars.EnableClassicLinkDnsSupport,
                AssignGeneratedIpv6CidrBlock = _vars.EnableIpv6,

                Tags = Merge(new Dictionary<string, string>
                {
                    ["Name"] = _vars.Name
                }, _vars.Tags, _vars.VpcTags)
            });

            var awsVpcIpv4CidrBlockAssociations = new List<VpcIpv4CidrBlockAssociation>();
            for (var i = 0; i < vars.SecondaryCidrBlocks.Count && _isCreateVpc; i++)
            {
                awsVpcIpv4CidrBlockAssociations.Add(new VpcIpv4CidrBlockAssociation(scope, id,
                    new VpcIpv4CidrBlockAssociationConfig
                    {
                        Count = 1,
                        VpcId = _vpc.Id,
                        CidrBlock = vars.SecondaryCidrBlocks[i]
                    }));
            }

            var defaultSecurityGroup = new DefaultSecurityGroup(scope, id, new DefaultSecurityGroupConfig
            {
                Count = _isCreateVpc && vars.ManageDefaultSecurityGroup ? 1 : 0,
                VpcId = _vpc.Id,
                Ingress = vars.DefaultSecurityGroupIngresses,
                Egress = vars.DefaultSecurityGroupEgresses,
                Tags = Merge(new Dictionary<string, string>
                    {
                        ["Name"] = Coalesce(vars.DefaultSecurityGroupName, vars.Name)
                    },
                    vars.Tags,
                    vars.DefaultSecurityGroupTags)
            });

            #endregion

            #region dhcp options set

            var vpcDhcpOptions = new VpcDhcpOptions(scope, id, new VpcDhcpOptionsConfig
            {
                Count = _isCreateVpc && vars.EnableDhcpOptions ? 1 : 0,
                DomainName = vars.DhcpOptionsDomainName,
                NtpServers = vars.DhcpOptionsNtpServers.ToArray(),
                NetbiosNameServers = vars.DhcpOptionsNetbiosNameServers.ToArray(),
                NetbiosNodeType = vars.DhcpOptionsNetbiosNodeType,

                Tags = Merge(new Dictionary<string, string>
                {
                    ["Name"] = vars.Name
                }, vars.Tags, vars.DhcpOptionsTags)
            });

            new VpcDhcpOptionsAssociation(scope, id, new VpcDhcpOptionsAssociationConfig
            {
                Count = _isCreateVpc && vars.EnableDhcpOptions ? 1 : 0,
                VpcId = _vpc.Id,
                DhcpOptionsId = vpcDhcpOptions.Id
            });

            #endregion

            #region Internet gateway

            var internetGateway = new InternetGateway(scope, id, new InternetGatewayConfig
            {
                Count = _isCreateVpc && vars.CreateIgw && vars.PublicSubnets.Count > 0 ? 1 : 0,
                VpcId = _vpc.Id,
                Tags = Merge(new Dictionary<string, string>
                {
                    ["Name"] = vars.Name
                }, vars.Tags, vars.IgwTags)
            });

            var egressOnlyInternetGateway = new EgressOnlyInternetGateway(scope, id, new EgressOnlyInternetGatewayConfig
            {
                Count = _isCreateVpc && vars.CreateEgressOnlyIgw && vars.EnableIpv6 && _maxSubnetLength > 0 ? 1 : 0,
                VpcId = _vpc.Id,
                Tags = Merge(new Dictionary<string, string>
                {
                    ["name"] = vars.Name
                }, vars.Tags, vars.IgwTags)
            });

            #endregion

            #region Default route

            var defaultRouteTable = new DefaultRouteTable(scope, id, new DefaultRouteTableConfig
            {
                Count = _isCreateVpc && vars.ManageDefaultRouteTable ? 1 : 0,
                DefaultRouteTableId = _vpc.DefaultRouteTableId,
                PropagatingVgws = vars.DefaultRouteTablePropagatingVgws.ToArray(),
                
                Route = vars.DefaultRouteTableRoutes.ToArray(),
                
                Timeouts = new DefaultRouteTableTimeouts()
                {
                    Create = "5m",
                    Update = "5m"
                },
                 
                Tags = Merge(new Dictionary<string, string>
                {
                    ["Name"] = Coalesce(vars.DefaultRouteTableName, vars.Name)
                }, vars.Tags, vars.DefaultRouteTableTags)
            });

            #endregion

            foreach (var az in vars.Azs)
            {
                Console.WriteLine(az);
            }

            PublicSubnets = new List<Subnet>();

            for (var i = 1; i <= 3; i++)
            {
                var subnet = new Subnet(scope, $"public-subnet-{i}", new SubnetConfig
                {
                    VpcId = _vpc.Id,
                    CidrBlock = $"10.10.{i}.0/24",
                    AvailabilityZone = vars.Azs[i - 1],
                    MapPublicIpOnLaunch = true,

                    Tags = new Dictionary<string, string>
                    {
                        ["Name"] = $"ckdtf-public-subnet-{i}",
                        ["Env"] = "dev"
                    }
                });

                PublicSubnets.Add(subnet);
            }

            var mainIgw = new InternetGateway(scope, "main-igw", new InternetGatewayConfig
            {
                VpcId = _vpc.Id,
                Tags = new Dictionary<string, string>
                {
                    ["Name"] = "ckdtf-main-igw"
                }
            });

            var mainRtb = new RouteTable(scope, "main-rtb", new RouteTableConfig
            {
                VpcId = _vpc.Id,
                Route = new
                {
                    CidrBlock = "0.0.0.0/0",
                    GatewayId = mainIgw.Id
                },
                Tags = new Dictionary<string, string>
                {
                    ["Name"] = "ckdtf-rtb"
                }
            });

            for (var i = 0; i < PublicSubnets.Count; i++)
            {
                new RouteTableAssociation(scope, $"main-public-{i}", new RouteTableAssociationConfig
                {
                    SubnetId = PublicSubnets[i].Id,
                    RouteTableId = mainRtb.Id
                });
            }

            // Output

            new TerraformOutput(scope, "vpc id", new TerraformOutputConfig()
            {
                Value = _vpc.Id
            });

            foreach (var subnet in PublicSubnets)
            {
                new TerraformOutput(scope, $"{subnet} id", new TerraformOutputConfig()
                {
                    Value = subnet.Id
                });
            }
        }
    }
}