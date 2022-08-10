using System;
using System.Collections.Generic;
using System.Linq;
using Constructs;
using HashiCorp.Cdktf;
using HashiCorp.Cdktf.Providers.Aws.Vpc;

namespace Cdktf.Dotnet.Aws
{
    public partial class VpcModule
    {
        public string Region { get; set; } = "us-east-1";
        public List<Subnet> PublicSubnets { get; }

        public string VpcId => _vpc.Id;

        private readonly Vpc _vpc;

        public VpcModule(Construct scope, string id)
        {
            _vpc = new Vpc(scope, id, new VpcConfig
            {
                CidrBlock = CidrBlock,
                InstanceTenancy = InstanceTenancy,
                EnableDnsHostnames = EnableDnsHostnames,
                EnableDnsSupport = EnableDnsSupport,
                EnableClassiclink = EnableClassicLink,
                EnableClassiclinkDnsSupport = EnableClassicLinkDnsSupport,
                AssignGeneratedIpv6CidrBlock = EnableIpv6,
                
                Tags = new Dictionary<string, string>
                {
                    ["Name"] = "ckdtf-vpc",
                    ["Env"] = "dev"
                }
            });

            var azs = "a,b,c".Split(",").Select(x => $"{Region}{x}").ToList();
            foreach (var az in azs)
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
                    AvailabilityZone = azs[i - 1],
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