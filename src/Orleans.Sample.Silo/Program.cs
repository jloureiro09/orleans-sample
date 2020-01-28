namespace Orleans.Sample.Silo
{
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Orleans.Configuration;
    using Orleans.Sample.Grains;
    using Orleans.Hosting;
    using System;
    
    public class OrleansConfiguration
    {
        public string ClusterId { get; set; }
        public string ServiceId { get; set; }
    }

    public class ConsulConfiguration
    {
        public string Address { get; set; }
        public string AclClientToken { get; set; }
        public string KvRootFolder { get; set; }
    }
    class Program
    {
        public static Task Main(string[] args)
        {
            return new HostBuilder()
                .ConfigureHostConfiguration(configurationBuilder =>
                {
                    configurationBuilder.AddCommandLine(args);
                    configurationBuilder.AddEnvironmentVariables();
                })
                .ConfigureAppConfiguration((context, config) =>
                {
                    var env = context.HostingEnvironment;
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                    config.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices((hostBuilderContext, services) =>
                {
                    services
                        .AddOptions()
                        .Configure<ConsulConfiguration>(hostBuilderContext.Configuration.GetSection("Consul"))
                        .Configure<OrleansConfiguration>(hostBuilderContext.Configuration.GetSection("Orleans"))
                        .Configure<ConsoleLifetimeOptions>(options =>
                        {
                            options.SuppressStatusMessages = true;
                        });
                })
                .UseOrleans((hostBuilderContext, siloBuilder) =>
                {
                    var consulConfig = hostBuilderContext.Configuration.GetSection("Consul").Get<ConsulConfiguration>();
                    var orleansConfig = hostBuilderContext.Configuration.GetSection("Orleans").Get<OrleansConfiguration>();

                    siloBuilder
                        .UseConsulClustering(options =>
                        {
                            options.Address = new Uri(consulConfig.Address);
                            options.AclClientToken = consulConfig.AclClientToken;
                            options.KvRootFolder = consulConfig.KvRootFolder;
                        })
                        .Configure<ClusterOptions>(options =>
                        {
                            options.ClusterId = orleansConfig.ClusterId;
                            options.ServiceId = orleansConfig.ServiceId;
                        })
                        .Configure<ClusterMembershipOptions>(options =>
                        {
                            options.DefunctSiloCleanupPeriod = TimeSpan.FromHours(1);
                            options.DefunctSiloExpiration = TimeSpan.FromHours(1);
                            options.TableRefreshTimeout = TimeSpan.FromSeconds(5);
                        })
                        .Configure<GrainCollectionOptions>(options =>
                        {
                            options.CollectionQuantum = TimeSpan.FromSeconds(15);
                            options.CollectionAge = TimeSpan.FromSeconds(30);
                        })
                        .ConfigureEndpoints(siloPort: 11111, gatewayPort: 30000)
                        .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(SampleGrain).Assembly).WithReferences())
                        .UseDashboard(options => { options.Port = 1111; });
                })
                .ConfigureLogging(builder =>
                {
                    builder.AddConsole();
                })
                // .UseConsoleLifetime(x=>{
                //     x.
                // });
                .RunConsoleAsync();
        }
    }
}
