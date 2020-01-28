namespace Orleans.Sample.Api
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.OpenApi.Models;
    using Orleans.Configuration;
    using Orleans.Hosting;

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
    
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddOptions()
                .Configure<ConsulConfiguration>(Configuration.GetSection("Consul"))
                .Configure<OrleansConfiguration>(Configuration.GetSection("Orleans"));

            services.AddControllers();

            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Orleans Basic", Version = "v1" });
            });

            // Orleans registration
            services.AddSingleton<IClusterClient>(serviceProvider =>
            {
                var consulConfig = serviceProvider.GetService<IOptions<ConsulConfiguration>>().Value;
                var orleansConfig = serviceProvider.GetService<IOptions<OrleansConfiguration>>().Value;

                return new ClientBuilder()
                    .UseConsulClustering(options =>
                    {
                        options.Address = new Uri(consulConfig.Address);
                        options.AclClientToken = consulConfig.AclClientToken;
                        options.KvRootFolder = consulConfig.KvRootFolder;
                    })
                    // .UseLocalhostClustering()
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = orleansConfig.ClusterId;
                        options.ServiceId = orleansConfig.ServiceId;
                    })
                    .Configure<GatewayOptions>(options =>
                    {
                        //options.GatewayListRefreshPeriod = TimeSpan.FromSeconds(1);
                    })
                    .ConfigureLogging(logging => logging.AddDebug().AddConsole())
                    .Build();;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
                c.RoutePrefix = string.Empty;
            });

            // if (env.IsDevelopment())
            // {
                app.UseDeveloperExceptionPage();
            // }

            // app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            var oleransClient = app.ApplicationServices.GetService<IClusterClient>();

            oleransClient.Connect(
                async exception => 
                {
                    // log?.LogWarning("Exception while attempting to connect to Orleans cluster: {Exception}", exception);
                    await Task.Delay(TimeSpan.FromSeconds(2));
                    return true;
                }).GetAwaiter().GetResult();
        }
    }
}
