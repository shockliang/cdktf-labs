using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Constructs;
using HashiCorp.Cdktf;
using HashiCorp.Cdktf.Providers.Aws;
using HashiCorp.Cdktf.Providers.Aws.Ec2;
using HashiCorp.Cdktf.Providers.Aws.Vpc;


namespace MyCompany.MyApp
{
    class MyApp : TerraformStack
    {
        public MyApp(Construct scope, string id) : base(scope, id)
        {
            var region = "us-east-1";
            new AwsProvider(this, "Aws", new AwsProviderConfig
            {
                AccessKey = Environment.GetEnvironmentVariable("CDKTF_AWS_ACCESS_KEY"),
                SecretKey = Environment.GetEnvironmentVariable("CDKTF_AWS_SECRET"),
                Region = region
            });

            var vpc = new Vpc(this, "Vpc", new VpcConfig
            {
                CidrBlock = "10.10.0.0/16",
                Tags = new Dictionary<string, string>
                {
                    ["Name"] = "ckdtf-vpc",
                    ["Env"] = "dev"
                }
            });

            var securityGroup = new SecurityGroup(this, "cdktf-allow-ssh", new SecurityGroupConfig()
            {
                VpcId = vpc.Id,
                Name = "cdktf-allow-ssh",
                Description = "security group that allow ssh and all egress traffic",
                Egress = new []
                {
                    new SecurityGroupEgress
                    {
                        CidrBlocks = new[] { "0.0.0.0/0" },
                        FromPort = 0,
                        ToPort = 0,
                        Protocol = "-1"
                    }
                },

                Ingress = new []
                {
                    new SecurityGroupIngress
                    {
                        CidrBlocks = new[] { "0.0.0.0/0" },
                        FromPort = 22,
                        ToPort = 22,
                        Protocol = "tcp"
                    }
                },

                Tags = new Dictionary<string, string >
                {
                ["Name"] = "cdktf-allow-ssh",
                ["Env"] = "dev"
            }
            });

            var mykey = new KeyPair(this, "mykey", new KeyPairConfig
            {
                KeyName = "mykey",
                // PublicKey = Fn.File("./mykey.pub")
                PublicKey = File.ReadAllText("mykey.pub")
            });

            var azs = "a,b,c".Split(",").Select(x => $"{region}{x}").ToList();
            foreach (var az in azs)
            {
                Console.WriteLine(az);
            }

            var publicSubnets = new List<Subnet>();

            for (var i = 1; i <= 3; i++)
            {
                var subnet = new Subnet(this, $"public-subnet-{i}", new SubnetConfig
                {
                    VpcId = vpc.Id,
                    CidrBlock = $"10.10.{i}.0/24",
                    AvailabilityZone = azs[i - 1],

                    Tags = new Dictionary<string, string>
                    {
                        ["Name"] = $"ckdtf-public-subnet-{i}",
                        ["Env"] = "dev"
                    }
                });

                publicSubnets.Add(subnet);
            }

            var mainIgw = new InternetGateway(this, "main-igw", new InternetGatewayConfig
            {
                VpcId = vpc.Id,
                Tags = new Dictionary<string, string>
                {
                    ["Name"] = "ckdtf-main-igw"
                }
            });

            var mainRtb = new RouteTable(this, "main-rtb", new RouteTableConfig
            {
                VpcId = vpc.Id,
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

            for (var i = 0; i < publicSubnets.Count; i++)
            {
                new RouteTableAssociation(this, $"main-public-{i}", new RouteTableAssociationConfig
                {
                    SubnetId = publicSubnets[i].Id,
                    RouteTableId = mainRtb.Id
                });
            }

            new TerraformOutput(this, "vpc id", new TerraformOutputConfig()
            {
                Value = vpc.Id
            });

            foreach (var subnet in publicSubnets)
            {
                new TerraformOutput(this, $"{subnet} id", new TerraformOutputConfig()
                {
                    Value = subnet.Id
                });
            }

            foreach (var subnet in publicSubnets)
            {
                new TerraformOutput(this, $"{subnet} az", new TerraformOutputConfig()
                {
                    Value = subnet.AvailabilityZone
                });
            }
        }

        public static void Main(string[] args)
        {
            App app = new App();
            new MyApp(app, "cdktf-vpc-dotnet");
            app.Synth();
            Console.WriteLine("App synth complete");
        }
    }
}