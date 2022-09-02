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

        private int _natGatewayCount = 0;

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
            
            _natGatewayCount = vars.SingleNatGateway
                ? 1
                : vars.OneNatGatewayPerAz
                    ? vars.Azs.Count
                    : _maxSubnetLength;

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

            #region Publiс routes

            var publicRouteTable = new RouteTable(scope, id, new RouteTableConfig
            {
                Count = _isCreateVpc && vars.PublicSubnets.Count > 0 ? 1: 0,
                VpcId = _vpc.Id,
                Tags = Merge(new Dictionary<string, string>
                {
                    ["Name"] = $"{vars.Name}-{vars.PublicSubnetSuffix}"
                }, vars.Tags, vars.PublicRouteTableTags)
            });

            var publicInternetGateway = new Route(scope, id, new RouteConfig
            {
                Count = _isCreateVpc && vars.CreateIgw && vars.PublicSubnets.Count > 0 ? 1 : 0,
                RouteTableId = publicRouteTable.Id,
                DestinationCidrBlock = "0.0.0.0/0",
                GatewayId = internetGateway.Id,
                
                Timeouts = new RouteTimeouts
                {
                    Create = "5m"
                }
            });

            var publicInternetGatewayIpv6 = new Route(scope, id, new RouteConfig
            {
                Count = _isCreateVpc && vars.CreateIgw && vars.EnableIpv6 && vars.PublicSubnets.Count > 0 ? 1 : 0,
                RouteTableId = publicRouteTable.Id,
                DestinationIpv6CidrBlock = "::/0",
                GatewayId = internetGateway.Id
            });

            #endregion

            #region Private routes

            // There are as many routing tables as the number of NAT gateways

            var createPrivateRouteTableCount = _isCreateVpc && _maxSubnetLength > 0 ? _natGatewayCount : 0;
            for (var i = 0; i < createPrivateRouteTableCount; i++)
            {
                var privateRouteTable = new RouteTable(scope, id, new RouteTableConfig
                {
                    Count = 1,
                    VpcId = _vpc.Id,
                    Tags = Merge(new Dictionary<string, string>
                        {
                            ["Name"] = vars.SingleNatGateway 
                                ? $"{vars.Name}-{vars.PrivateSubnetSuffix}"
                                : $"{vars.Name}-{vars.PrivateSubnetSuffix}-{i}"
                        },
                        vars.Tags,
                        vars.PrivateRouteTableTags)
                });
            }
            
            #endregion

            #region Database routes

            var createDatabaseRouteTableCount = _isCreateVpc
                                                && vars.CreateDatabaseSubnetRouteTable
                                                && vars.DatabaseSubnets.Count > 0
                ? vars.SingleNatGateway || vars.CreateDatabaseInternetGatewayRoute
                    ? 1
                    : vars.DatabaseSubnets.Count
                : 0;

            var databaseRouteTables = new List<RouteTable>();
            for (var i = 0; i < createDatabaseRouteTableCount; i++)
            {
                databaseRouteTables.Add(new RouteTable(scope, id, new RouteTableConfig
                {
                    Count = 1,
                    VpcId = _vpc.Id,
                    Tags = Merge(new Dictionary<string, string>
                        {
                            ["Name"] = vars.SingleNatGateway || vars.CreateDatabaseInternetGatewayRoute
                                ? $"{vars.Name}-{vars.DatabaseSubnetSuffix}"
                                : $"{vars.Name}-{vars.DatabaseSubnetSuffix}-{i}"
                        },
                        vars.Tags,
                        vars.DatabaseRouteTableTags)
                }));
            }

            var databaseInternetGateway = new Route(scope, id, new RouteConfig
            {
                Count = _isCreateVpc
                        && vars.CreateIgw
                        && vars.CreateDatabaseSubnetRouteTable
                        && vars.DatabaseSubnets.Count > 0
                        && vars.CreateDatabaseInternetGatewayRoute
                        && vars.CreateDatabaseNatGatewayRoute == false
                    ? 1
                    : 0,
                RouteTableId = databaseRouteTables.FirstOrDefault().Id,
                DestinationCidrBlock = "0.0.0.0/0",
                GatewayId = internetGateway.Id,
                
                Timeouts = new RouteTimeouts
                {
                    Create = "5m"
                }
            });

            var databaseIpv6Egress = new Route(scope, id, new RouteConfig
            {
                Count = _isCreateVpc
                        && vars.CreateEgressOnlyIgw
                        && vars.EnableIpv6
                        && vars.CreateDatabaseSubnetRouteTable
                        && vars.DatabaseSubnets.Count > 0
                        && vars.CreateDatabaseInternetGatewayRoute
                    ? 1
                    : 0,
                RouteTableId = databaseRouteTables.FirstOrDefault().Id,
                DestinationIpv6CidrBlock = "::/0",
                EgressOnlyGatewayId = egressOnlyInternetGateway.Id,
                
                Timeouts = new RouteTimeouts
                {
                    Create = "5m"
                }
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