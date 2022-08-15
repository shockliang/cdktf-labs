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

        private bool _isCreateVpc = true;

        public VpcModule(Construct scope, string id, VpcModuleVariables vars)
        {
            _vars = vars;
            _isCreateVpc = _vars.CreateVpc && _vars.PutinKhuylo;
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