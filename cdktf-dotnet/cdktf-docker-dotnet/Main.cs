using System;
using Constructs;
using HashiCorp.Cdktf;

using HashiCorp.Cdktf.Providers.Docker;


namespace Cdktf.Dotnet.DockerLab
{
    class MyApp : TerraformStack
    {
        public MyApp(Construct scope, string id) : base(scope, id)
        {

            new DockerProvider(this, "docker", new DockerProviderConfig { });

            var dockerImage = new Image(this, "nginxImage", new ImageConfig
            {
                Name = "nginx:latest",
                KeepLocally = false,
            });

            new Container(this, "nginxContainer", new ContainerConfig
            {
                Image = dockerImage.Latest,
                Name = "tutorial",
                Ports = new [] { new ContainerPorts
                {
                    Internal = 80,
                    External = 8000
                }}
            });
        }

        public static void Main(string[] args)
        {
            App app = new App();
            new MyApp(app, "cdktf-docker-dotnet");
            app.Synth();
            Console.WriteLine("App synth complete");
        }
    }
}