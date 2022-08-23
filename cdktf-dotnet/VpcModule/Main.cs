using System;
using System.Linq;
using Cdktf.Dotnet.Aws;
using Constructs;
using HashiCorp.Cdktf;
using HashiCorp.Cdktf.Providers.Aws;


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

            var vpcVars = new VpcModuleVariables
            {
                Name = "testing-dotnet-vpc-module",
                Azs = "a,b,c".Split(",").Select(x => $"{region}{x}").ToList()
            };
            var vpcModule = new VpcModule(this, "cdktf-vpc-module", vpcVars);
            
        }

        public static void Main(string[] args)
        {
            App app = new App();
            new MyApp(app, "VpcModule");
            app.Synth();
            Console.WriteLine("App synth complete");
        }
    }
}