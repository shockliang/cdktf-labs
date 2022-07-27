using System;
using System.Collections.Generic;
using System.Linq;
using Constructs;
using HashiCorp.Cdktf;
using HashiCorp.Cdktf.Providers.Aws;
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
                    AvailabilityZone = azs[i-1],
                
                    Tags = new Dictionary<string, string>
                    {
                        ["Name"] = $"ckdtf-public-subnet-{i}",
                        ["Env"] = "dev"
                    }
                });
                
                publicSubnets.Add(subnet);
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