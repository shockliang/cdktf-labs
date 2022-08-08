using System;
using System.Collections.Generic;
using System.Linq;
using Constructs;
using HashiCorp.Cdktf;
using HashiCorp.Cdktf.Providers.Aws.Vpc;

namespace Cdktf.Dotnet.VpcModule.Aws
{
    public class VpcModule
    {
        public string Region { get; set; } = "us-east-1";
        public string CidrBlock { get; set; } = "10.10.0.0/16";
        public List<Subnet> PublicSubnets { get; }

        public string VpcId => _vpc.Id;

        private readonly Vpc _vpc;

        public VpcModule(Construct scope, string id)
        {
            _vpc = new Vpc(scope, id, new VpcConfig
            {
                CidrBlock = CidrBlock,
                Tags = new Dictionary<string, string>
                {
                    ["Name"] = "ckdtf-vpc",
                    ["Env"] = "dev"
                }
            });

            // var allowSshSecurityGroup = new SecurityGroup(scope, "cdktf-allow-ssh", new SecurityGroupConfig()
            // {
            //     VpcId = _vpc.Id,
            //     Name = "cdktf-allow-ssh",
            //     Description = "security group that allow ssh and all egress traffic",
            //     Egress = new[]
            //     {
            //         new SecurityGroupEgress
            //         {
            //             CidrBlocks = new[] { "0.0.0.0/0" },
            //             FromPort = 0,
            //             ToPort = 0,
            //             Protocol = "-1"
            //         }
            //     },
            //
            //     Ingress = new[]
            //     {
            //         new SecurityGroupIngress
            //         {
            //             CidrBlocks = new[] { "0.0.0.0/0" },
            //             FromPort = 22,
            //             ToPort = 22,
            //             Protocol = "tcp"
            //         }
            //     },
            //
            //     Tags = new Dictionary<string, string>
            //     {
            //         ["Name"] = "cdktf-allow-ssh",
            //         ["Env"] = "dev"
            //     }
            // });

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