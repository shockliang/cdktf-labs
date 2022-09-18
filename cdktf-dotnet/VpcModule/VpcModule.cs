using System;
using System.Collections.Generic;
using System.Linq;
using Constructs;
using HashiCorp.Cdktf;
using HashiCorp.Cdktf.Providers.Aws.Elasticache;
using HashiCorp.Cdktf.Providers.Aws.Rds;
using HashiCorp.Cdktf.Providers.Aws.Redshift;
using HashiCorp.Cdktf.Providers.Aws.Vpc;
using static Cdktf.Dotnet.Aws.Utils;

namespace Cdktf.Dotnet.Aws
{
    public class VpcModule
    {
        private readonly VpcModuleVariables _vars;
        public string Region { get; set; } = "us-east-1";
        public List<Subnet> PublicSubnets { get; }
        public List<Subnet> PrivateSubnets { get; }
        public List<Subnet> OutpostSubnets { get; }
        public List<Subnet> DatabaseSubnets { get; }
        public List<Subnet> RedshiftSubnets { get; }
        public List<Subnet> ElasticacheSubnets { get; }
        public List<Subnet> IntraSubnets { get; }

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

            #region Redshift routes

            var redshiftRoute = new RouteTable(scope, id, new RouteTableConfig
            {
                Count = _isCreateVpc && vars.CreateRedshiftSubnetRouteTable && vars.RedshiftSubnets.Count > 0 ? 1 : 0,
                VpcId = _vpc.Id,
                Tags = Merge(new Dictionary<string, string>
                    {
                        ["Name"] = $"{vars.Name}-{vars.RedshiftSubnetSuffix}"
                    },
                    vars.Tags,
                    vars.RedshiftRouteTableTags)
            });

            #endregion

            #region Elasticache routes

            var elasticacheRouteTable = new RouteTable(scope, id, new RouteTableConfig
            {
                Count = _isCreateVpc && vars.CreateElasticacheSubnetRouteTable && vars.ElasticacheSubnets.Count > 0
                    ? 1
                    : 0,
                VpcId = _vpc.Id,
                Tags = Merge(new Dictionary<string, string>
                    {
                        ["Name"] = $"{vars.Name}-{vars.ElastiCacheSubnetSuffix}"
                    },
                    vars.Tags,
                    vars.ElasticacheRouteTableTags)
            });

            #endregion

            #region Intra routes

            var intraRouteTable = new RouteTable(scope, id, new RouteTableConfig
            {
                Count = _isCreateVpc && vars.IntraSubnets.Count > 0 ? 1 : 0,
                VpcId = _vpc.Id,
                Tags = Merge(new Dictionary<string, string>
                    {
                        ["Name"] = $"{vars.Name}-{vars.IntraSubnetSuffix}"
                    },
                    vars.Tags,
                    vars.IntraRouteTableTags)
            });

            #endregion

            #region Public subnets

            var publicSubnetCount = _isCreateVpc
                                    && vars.PublicSubnets.Count > 0
                                    && (!vars.OneNatGatewayPerAz || vars.PublicSubnets.Count >= vars.Azs.Count)
                ? vars.PublicSubnets.Count
                : 0;

            PublicSubnets = new List<Subnet>();

            for (var i = 0; i < publicSubnetCount; i++)
            {
                var subnet = new Subnet(scope, $"public-subnet-{i}", new SubnetConfig
                {
                    Count = 1,
                    VpcId = _vpc.Id,
                    CidrBlock = vars.PublicSubnets[i],
                    AvailabilityZone = Fn.Regexall("^[a-z]{2}-", vars.Azs[i]).Length > 0 ? vars.Azs[i]: "",
                    AvailabilityZoneId = Fn.Regexall("^[a-z]{2}-", vars.Azs[i]).Length == 0 ? vars.Azs[i]: "",
                    MapPublicIpOnLaunch = vars.MapPublicIpOnLaunch,
                    AssignIpv6AddressOnCreation = vars.PublicSubnetAssignIpv6AddressOnCreation == false
                        ? vars.AssignIpv6AddressOnCreation
                        : vars.PublicSubnetAssignIpv6AddressOnCreation,

                    Ipv6CidrBlock = vars.EnableIpv6 && vars.PublicSubnetIpv6Prefixes.Count > 0
                        ? Fn.Cidrsubnet(_vpc.Ipv6CidrBlock, 8, double.Parse(vars.PublicSubnetIpv6Prefixes[i]))
                        : "",

                    Tags = Merge(new Dictionary<string, string>
                        {
                            ["Name"] = $"{vars.Name}-{vars.PublicSubnetSuffix}-{vars.Azs[i]}"
                        },
                        vars.Tags,
                        vars.PublicSubnetTags)
                });

                PublicSubnets.Add(subnet);
            }
            
            #endregion

            #region Private subnets

            var privateSubnetCount = _isCreateVpc && vars.PrivateSubnets.Count > 0
                ? vars.PrivateSubnets.Count
                : 0;

            PrivateSubnets = new List<Subnet>();

            for (var i = 0; i < privateSubnetCount; i++)
            {
                var subnet = new Subnet(scope, $"private-subnet-{i}", new SubnetConfig
                {
                    Count = 1,
                    VpcId = _vpc.Id,
                    CidrBlock = vars.PrivateSubnets[i],
                    AvailabilityZone = Fn.Regexall("^[a-z]{2}-", vars.Azs[i]).Length > 0 ? vars.Azs[i]: "",
                    AvailabilityZoneId = Fn.Regexall("^[a-z]{2}-", vars.Azs[i]).Length == 0 ? vars.Azs[i]: "",
                    AssignIpv6AddressOnCreation = vars.PrivateSubnetAssignIpv6AddressOnCreation == false
                        ? vars.AssignIpv6AddressOnCreation
                        : vars.PrivateSubnetAssignIpv6AddressOnCreation,

                    Ipv6CidrBlock = vars.EnableIpv6 && vars.PrivateSubnetIpv6Prefixes.Count > 0
                        ? Fn.Cidrsubnet(_vpc.Ipv6CidrBlock, 8, double.Parse(vars.PrivateSubnetIpv6Prefixes[i]))
                        : "",

                    Tags = Merge(new Dictionary<string, string>
                        {
                            ["Name"] = $"{vars.Name}-{vars.PrivateSubnetSuffix}-{vars.Azs[i]}"
                        },
                        vars.Tags,
                        vars.PrivateSubnetTags)
                });

                PrivateSubnets.Add(subnet);
            }

            #endregion
            
            #region Outpost subnet

            var outpostSubnetCount = _isCreateVpc && vars.OutpostSubnets.Count > 0
                ? vars.OutpostSubnets.Count
                : 0;

            OutpostSubnets = new List<Subnet>();
            
            for (var i = 0; i < outpostSubnetCount; i++)
            {
                var subnet = new Subnet(scope, $"outpost-subnet-{i}", new SubnetConfig
                {
                    Count = 1,
                    VpcId = _vpc.Id,
                    CidrBlock = vars.OutpostSubnets[i],
                    AvailabilityZone = vars.OutpostAz,
                    AssignIpv6AddressOnCreation = vars.OutpostSubnetAssignIpv6AddressOnCreation == false
                        ? vars.AssignIpv6AddressOnCreation
                        : vars.OutpostSubnetAssignIpv6AddressOnCreation,

                    Ipv6CidrBlock = vars.EnableIpv6 && vars.OutpostSubnetIpv6Prefixes.Count > 0
                        ? Fn.Cidrsubnet(_vpc.Ipv6CidrBlock, 8, double.Parse(vars.OutpostSubnetIpv6Prefixes[i]))
                        : "",
                    OutpostArn = vars.OutpostArn,

                    Tags = Merge(new Dictionary<string, string>
                        {
                            ["Name"] = $"{vars.Name}-{vars.OutpostSubnetSuffix}-{vars.OutpostAz}"
                        },
                        vars.Tags,
                        vars.OutpostSubnetTags)
                });

                OutpostSubnets.Add(subnet);
            }

            #endregion

            #region Database subnet

            var databaseSubnetCount = _isCreateVpc && vars.DatabaseSubnets.Count > 0
                ? vars.DatabaseSubnets.Count
                : 0;

            DatabaseSubnets = new List<Subnet>();
            
            for (var i = 0; i < databaseSubnetCount; i++)
            {
                var subnet = new Subnet(scope, $"database-subnet-{i}", new SubnetConfig
                {
                    Count = 1,
                    VpcId = _vpc.Id,
                    CidrBlock = vars.DatabaseSubnets[i],
                    AvailabilityZone = Fn.Regexall("^[a-z]{2}-", vars.Azs[i]).Length > 0 ? vars.Azs[i]: "",
                    AvailabilityZoneId = Fn.Regexall("^[a-z]{2}-", vars.Azs[i]).Length == 0 ? vars.Azs[i]: "",
                    AssignIpv6AddressOnCreation = vars.DatabaseSubnetAssignIpv6AddressOnCreation == false
                        ? vars.AssignIpv6AddressOnCreation
                        : vars.DatabaseSubnetAssignIpv6AddressOnCreation,

                    Ipv6CidrBlock = vars.EnableIpv6 && vars.DatabaseSubnetIpv6Prefixes.Count > 0
                        ? Fn.Cidrsubnet(_vpc.Ipv6CidrBlock, 8, double.Parse(vars.DatabaseSubnetIpv6Prefixes[i]))
                        : "",

                    Tags = Merge(new Dictionary<string, string>
                        {
                            ["Name"] = $"{vars.Name}-{vars.DatabaseSubnetSuffix}-{vars.Azs[i]}"
                        },
                        vars.Tags,
                        vars.DatabaseSubnetTags)
                });

                DatabaseSubnets.Add(subnet);
            }

            var dbSubnetGroup = new DbSubnetGroup(scope, "db-subnet-group", new DbSubnetGroupConfig
            {
                Count = _isCreateVpc && vars.DatabaseSubnets.Count > 0 && vars.CreateDatabaseSubnetGroup ? 1 : 0,
                Name = Coalesce(vars.DatabaseSubnetGroupName, vars.Name).ToLower(),
                Description = $"Database subnet group for ${vars.Name}",
                SubnetIds = DatabaseSubnets.Select(x => x.Id).ToArray(),
                Tags = Merge(new Dictionary<string, string>
                    {
                        ["Name"] = Coalesce(vars.DatabaseSubnetGroupName, vars.Name).ToLower()
                    },
                    vars.Tags,
                    vars.DatabaseSubnetGroupTags)
            });
            
            #endregion

            #region Redshift subnet

            var redshiftSubnetCount = _isCreateVpc && vars.RedshiftSubnets.Count > 0
                ? vars.RedshiftSubnets.Count
                : 0;

            RedshiftSubnets = new List<Subnet>();
            
            for (var i = 0; i < redshiftSubnetCount; i++)
            {
                var subnet = new Subnet(scope, $"redshift-subnet-{i}", new SubnetConfig
                {
                    Count = 1,
                    VpcId = _vpc.Id,
                    CidrBlock = vars.RedshiftSubnets[i],
                    AvailabilityZone = Fn.Regexall("^[a-z]{2}-", vars.Azs[i]).Length > 0 ? vars.Azs[i]: "",
                    AvailabilityZoneId = Fn.Regexall("^[a-z]{2}-", vars.Azs[i]).Length == 0 ? vars.Azs[i]: "",
                    AssignIpv6AddressOnCreation = vars.RedshiftSubnetAssignIpv6AddressOnCreation == false
                        ? vars.AssignIpv6AddressOnCreation
                        : vars.RedshiftSubnetAssignIpv6AddressOnCreation,

                    Ipv6CidrBlock = vars.EnableIpv6 && vars.RedshiftSubnetIpv6Prefixes.Count > 0
                        ? Fn.Cidrsubnet(_vpc.Ipv6CidrBlock, 8, double.Parse(vars.RedshiftSubnetIpv6Prefixes[i]))
                        : "",

                    Tags = Merge(new Dictionary<string, string>
                        {
                            ["Name"] = $"{vars.Name}-{vars.RedshiftSubnetSuffix}-{vars.Azs[i]}"
                        },
                        vars.Tags,
                        vars.RedshiftSubnetTags)
                });

                RedshiftSubnets.Add(subnet);
            }

            var redshiftSubnetGroup = new RedshiftSubnetGroup(scope, "redshift-subnet-group", new RedshiftSubnetGroupConfig
            {
                Count = _isCreateVpc && vars.RedshiftSubnets.Count > 0 && vars.CreateRedshiftSubnetGroup ? 1 : 0,
                Name = Coalesce(vars.RedshiftSubnetGroupName, vars.Name).ToLower(),
                Description = $"Redshift subnet group for ${vars.Name}",
                SubnetIds = RedshiftSubnets.Select(x => x.Id).ToArray(),
                Tags = Merge(new Dictionary<string, string>
                    {
                        ["Name"] = Coalesce(vars.RedshiftSubnetGroupName, vars.Name).ToLower()
                    },
                    vars.Tags,
                    vars.RedshiftSubnetGroupTags)
            });

            #endregion

            #region Elasticache subnet

            var elasticacheSubnetCount = _isCreateVpc && vars.ElasticacheSubnets.Count > 0
                ? vars.ElasticacheSubnets.Count
                : 0;

            ElasticacheSubnets = new List<Subnet>();
            
            for (var i = 0; i < elasticacheSubnetCount; i++)
            {
                var subnet = new Subnet(scope, $"elasticache-subnet-{i}", new SubnetConfig
                {
                    Count = 1,
                    VpcId = _vpc.Id,
                    CidrBlock = vars.ElasticacheSubnets[i],
                    AvailabilityZone = Fn.Regexall("^[a-z]{2}-", vars.Azs[i]).Length > 0 ? vars.Azs[i]: "",
                    AvailabilityZoneId = Fn.Regexall("^[a-z]{2}-", vars.Azs[i]).Length == 0 ? vars.Azs[i]: "",
                    AssignIpv6AddressOnCreation = vars.ElasticacheSubnetAssignIpv6AddressOnCreation == false
                        ? vars.AssignIpv6AddressOnCreation
                        : vars.ElasticacheSubnetAssignIpv6AddressOnCreation,

                    Ipv6CidrBlock = vars.EnableIpv6 && vars.ElasticacheSubnetIpv6Prefixes.Count > 0
                        ? Fn.Cidrsubnet(_vpc.Ipv6CidrBlock, 8, double.Parse(vars.ElasticacheSubnetIpv6Prefixes[i]))
                        : "",

                    Tags = Merge(new Dictionary<string, string>
                        {
                            ["Name"] = $"{vars.Name}-{vars.ElastiCacheSubnetSuffix}-{vars.Azs[i]}"
                        },
                        vars.Tags,
                        vars.ElasticacheSubnetTags)
                });

                ElasticacheSubnets.Add(subnet);
            }

            var elasticacheSubnetGroup = new ElasticacheSubnetGroup(scope, "redshift-subnet-group", new ElasticacheSubnetGroupConfig
            {
                Count = _isCreateVpc && vars.ElasticacheSubnets.Count > 0 && vars.CreateElasticacheSubnetGroup ? 1 : 0,
                Name = Coalesce(vars.ElasticacheSubnetGroupName, vars.Name).ToLower(),
                Description = $"Elasticache subnet group for {vars.Name}",
                SubnetIds = ElasticacheSubnets.Select(x => x.Id).ToArray(),
                Tags = Merge(new Dictionary<string, string>
                    {
                        ["Name"] = Coalesce(vars.ElasticacheSubnetGroupName, vars.Name).ToLower()
                    },
                    vars.Tags,
                    vars.ElasticacheSubnetGroupTags)
            });

            #endregion

            #region Intra subnets - private subnet without NAT gateway

            var intraSubnetCount = _isCreateVpc && vars.IntraSubnets.Count > 0
                ? vars.IntraSubnets.Count
                : 0;

            IntraSubnets = new List<Subnet>();
            
            for (var i = 0; i < intraSubnetCount; i++)
            {
                var subnet = new Subnet(scope, $"intra-subnet-{i}", new SubnetConfig
                {
                    Count = 1,
                    VpcId = _vpc.Id,
                    CidrBlock = vars.IntraSubnets[i],
                    AvailabilityZone = Fn.Regexall("^[a-z]{2}-", vars.Azs[i]).Length > 0 ? vars.Azs[i]: "",
                    AvailabilityZoneId = Fn.Regexall("^[a-z]{2}-", vars.Azs[i]).Length == 0 ? vars.Azs[i]: "",
                    AssignIpv6AddressOnCreation = vars.IntraSubnetAssignIpv6AddressOnCreation == false
                        ? vars.AssignIpv6AddressOnCreation
                        : vars.IntraSubnetAssignIpv6AddressOnCreation,

                    Ipv6CidrBlock = vars.EnableIpv6 && vars.IntraSubnetIpv6Prefixes.Count > 0
                        ? Fn.Cidrsubnet(_vpc.Ipv6CidrBlock, 8, double.Parse(vars.IntraSubnetIpv6Prefixes[i]))
                        : "",

                    Tags = Merge(new Dictionary<string, string>
                        {
                            ["Name"] = $"{vars.Name}-{vars.IntraSubnetSuffix}-{vars.Azs[i]}"
                        },
                        vars.Tags,
                        vars.IntraSubnetTags)
                });

                IntraSubnets.Add(subnet);
            }

            #endregion

            #region Default Network ACLs

            string[]? subnetIdsForDefaultNetworkAcl = null;
            
            var defaultNetworkAcl = new DefaultNetworkAcl(scope, "default-network-acl", new DefaultNetworkAclConfig
            {
                Count = _isCreateVpc && vars.ManageDefaultNetworkAcl ? 1 : 0,
                // subnet_ids is using lifecycle ignore_changes, so it is not necessary to list
                // any explicitly. See https://github.com/terraform-aws-modules/terraform-aws-vpc/issues/736.
                SubnetIds = subnetIdsForDefaultNetworkAcl,
                
                Ingress = vars.DefaultNetworkAclIngress,
                Egress = vars.DefaultNetworkAclEgress,

                Tags = Merge(new Dictionary<string, string>
                    {
                        ["Name"] = Coalesce(vars.DefaultNetworkAclName, vars.Name)
                    },
                    vars.Tags,
                    vars.DefaultNetworkAclTags),
                
                Lifecycle = new TerraformResourceLifecycle
                {
                    IgnoreChanges = new [] {subnetIdsForDefaultNetworkAcl}
                }
            });

            #endregion

            #region Public Network ACLs

            var publicNetworkAcl = new NetworkAcl(scope, "public-network-acl", new NetworkAclConfig
            {
                Count = _isCreateVpc && vars.PublicDedicatedNetworkAcl && vars.PublicSubnets.Count > 0 ? 1 : 0,
                VpcId = _vpc.Id,
                SubnetIds = PublicSubnets.Select(x => x.Id).ToArray(),
                Tags = Merge(new Dictionary<string, string>
                    {
                        ["Name"] = $"{vars.Name}-${vars.PublicSubnetSuffix}"
                    },
                    vars.Tags,
                    vars.PublicAclTags)
            });

            var publicInboundAclRuleCount = _isCreateVpc
                                            && vars.PublicDedicatedNetworkAcl
                                            && vars.PublicSubnets.Count > 0
                ? vars.PublicInboundAclRules.Count
                : 0;

            var publicInboundAclRules = new List<NetworkAclRule>();
            
            for (var i = 0; i < publicInboundAclRuleCount; i++)
            {
                var rule = new NetworkAclRule(scope, $"public-inbound-acl-rule-{i}", new NetworkAclRuleConfig
                {
                    Count = 1,
                    NetworkAclId = publicNetworkAcl.Id,
                    Egress = false,
                    RuleNumber = vars.PublicInboundAclRules[i].RuleNo.Value,
                    RuleAction = vars.PublicInboundAclRules[i].Action,
                    FromPort = vars.PublicInboundAclRules[i].FromPort,
                    ToPort = vars.PublicInboundAclRules[i].ToPort,
                    IcmpCode = vars.PublicInboundAclRules[i].IcmpCode,
                    IcmpType = vars.PublicInboundAclRules[i].IcmpType,
                    Protocol = vars.PublicInboundAclRules[i].Protocol,
                    CidrBlock = vars.PublicInboundAclRules[i].CidrBlock,
                    Ipv6CidrBlock = vars.PublicInboundAclRules[i].Ipv6CidrBlock
                });
                
                publicInboundAclRules.Add(rule);
            }
            
            var publicOutboundAclRuleCount = _isCreateVpc
                                            && vars.PublicDedicatedNetworkAcl
                                            && vars.PublicSubnets.Count > 0
                ? vars.PublicOutboundAclRules.Count
                : 0;

            var publicOutboundAclRules = new List<NetworkAclRule>();
            
            for (var i = 0; i < publicOutboundAclRuleCount; i++)
            {
                var rule = new NetworkAclRule(scope, $"public-outbound-acl-rule-{i}", new NetworkAclRuleConfig
                {
                    Count = 1,
                    NetworkAclId = publicNetworkAcl.Id,
                    Egress = true,
                    RuleNumber = vars.PublicOutboundAclRules[i].RuleNo.Value,
                    RuleAction = vars.PublicOutboundAclRules[i].Action,
                    FromPort = vars.PublicOutboundAclRules[i].FromPort,
                    ToPort = vars.PublicOutboundAclRules[i].ToPort,
                    IcmpCode = vars.PublicOutboundAclRules[i].IcmpCode,
                    IcmpType = vars.PublicOutboundAclRules[i].IcmpType,
                    Protocol = vars.PublicOutboundAclRules[i].Protocol,
                    CidrBlock = vars.PublicOutboundAclRules[i].CidrBlock,
                    Ipv6CidrBlock = vars.PublicOutboundAclRules[i].Ipv6CidrBlock
                });
                
                publicOutboundAclRules.Add(rule);
            }
            #endregion

            #region Private Network ACLs
            
            var privateNetworkAcl = new NetworkAcl(scope, "private-network-acl", new NetworkAclConfig
            {
                Count = _isCreateVpc && vars.PrivateDedicatedNetworkAcl && vars.PrivateSubnets.Count > 0 ? 1 : 0,
                VpcId = _vpc.Id,
                SubnetIds = PrivateSubnets.Select(x => x.Id).ToArray(),
                Tags = Merge(new Dictionary<string, string>
                    {
                        ["Name"] = $"{vars.Name}-${vars.PrivateSubnetSuffix}"
                    },
                    vars.Tags,
                    vars.PrivateAclTags)
            });

            var privateInboundAclRuleCount = _isCreateVpc
                                            && vars.PrivateDedicatedNetworkAcl
                                            && vars.PrivateSubnets.Count > 0
                ? vars.PrivateInboundAclRules.Count
                : 0;

            var privateInboundAclRules = new List<NetworkAclRule>();
            
            for (var i = 0; i < privateInboundAclRuleCount; i++)
            {
                var rule = new NetworkAclRule(scope, $"private-inbound-acl-rule-{i}", new NetworkAclRuleConfig
                {
                    Count = 1,
                    NetworkAclId = privateNetworkAcl.Id,
                    Egress = false,
                    RuleNumber = vars.PublicInboundAclRules[i].RuleNo.Value,
                    RuleAction = vars.PublicInboundAclRules[i].Action,
                    FromPort = vars.PublicInboundAclRules[i].FromPort,
                    ToPort = vars.PublicInboundAclRules[i].ToPort,
                    IcmpCode = vars.PublicInboundAclRules[i].IcmpCode,
                    IcmpType = vars.PublicInboundAclRules[i].IcmpType,
                    Protocol = vars.PublicInboundAclRules[i].Protocol,
                    CidrBlock = vars.PublicInboundAclRules[i].CidrBlock,
                    Ipv6CidrBlock = vars.PublicInboundAclRules[i].Ipv6CidrBlock
                });
                
                privateInboundAclRules.Add(rule);
            }
            
            var privateOutboundAclRuleCount = _isCreateVpc
                                            && vars.PrivateDedicatedNetworkAcl
                                            && vars.PrivateSubnets.Count > 0
                ? vars.PrivateOutboundAclRules.Count
                : 0;

            var privateOutboundAclRules = new List<NetworkAclRule>();
            
            for (var i = 0; i < privateOutboundAclRuleCount; i++)
            {
                var rule = new NetworkAclRule(scope, $"public-outbound-acl-rule-{i}", new NetworkAclRuleConfig
                {
                    Count = 1,
                    NetworkAclId = publicNetworkAcl.Id,
                    Egress = true,
                    RuleNumber = vars.PrivateOutboundAclRules[i].RuleNo.Value,
                    RuleAction = vars.PrivateOutboundAclRules[i].Action,
                    FromPort = vars.PrivateOutboundAclRules[i].FromPort,
                    ToPort = vars.PrivateOutboundAclRules[i].ToPort,
                    IcmpCode = vars.PrivateOutboundAclRules[i].IcmpCode,
                    IcmpType = vars.PrivateOutboundAclRules[i].IcmpType,
                    Protocol = vars.PrivateOutboundAclRules[i].Protocol,
                    CidrBlock = vars.PrivateOutboundAclRules[i].CidrBlock,
                    Ipv6CidrBlock = vars.PrivateOutboundAclRules[i].Ipv6CidrBlock
                });
                
                privateOutboundAclRules.Add(rule);
            }

            #endregion
            
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