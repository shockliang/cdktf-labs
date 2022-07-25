using System;
using System.Collections.Generic;
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
            new AwsProvider(this, "Aws", new AwsProviderConfig()
            {
                AccessKey = Environment.GetEnvironmentVariable("CDKTF_AWS_ACCESS_KEY"),
                SecretKey = Environment.GetEnvironmentVariable("CDKTF_AWS_SECRET"),
                Region = "us-east-1"
            });

            new Vpc(this, "Vpc", new VpcConfig
            {
                CidrBlock = "10.10.0.0/16",
                Tags = new Dictionary<string, string>
                {
                    ["Name"] = "ckdtf-vpc",
                    ["Env"] = "dev"
                }
            });
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