namespace Orleans.Sample.Api.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Orleans.Sample.GrainInterfaces;
    using Orleans.Runtime.Messaging;
    using Polly;

    [ApiController]
    [Route("[controller]")]
    public class GrainLocationController : ControllerBase
    {
        private readonly ILogger<GrainLocationController> logger;
        private readonly IClusterClient orleansClusterClient;

        public GrainLocationController(ILogger<GrainLocationController> logger, IClusterClient orleansClusterClient)
        {
            this.logger = logger;
            this.orleansClusterClient = orleansClusterClient;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody]int[] grainIds)
        {
            return this.Ok(
                await Task.WhenAll(
                    grainIds.Select(async grainId =>
                        await this.GetGrainLocation(grainId))));
        }

        [HttpGet]
        [Route("{grainId:int}")]
        public async Task<IActionResult> Get([FromRoute]int grainId)
        {
            return this.Ok(
                await this.GetGrainLocation(grainId));
        }

        private async Task<GrainLocation> GetGrainLocation(int grainId)
        {
            try
            {
                Console.WriteLine($"############# Loading grainId: {grainId}");
                var sampleGrain = this.orleansClusterClient.GetGrain<ISampleGrain>(grainId);

                Console.WriteLine($"############# GetHost for grainId: {grainId}");
                var grainHost = await sampleGrain.GetHost();
                Console.WriteLine($"############# Success grainId: {grainId}");

                return new GrainLocation
                {
                    ApiHost = System.Environment.MachineName,
                    GrainHost = grainHost,
                    GrainId = grainId
                };
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"############# Error for grainId: {grainId}; Ex: {ex.ToString()}");

                throw;
            }

            // return await Policy
            //     .Handle<ConnectionFailedException>()
            //     .WaitAndRetry(3, retryAttempt => TimeSpan.FromSeconds(2))
            //     .ExecuteAndCapture(async () =>
            //     {
            //         var sampleGrain = this.orleansClusterClient.GetGrain<ISampleGrain>(grainId);

            //         var grainHost = await sampleGrain.GetHost();

            //         return new GrainLocation
            //         {
            //             ApiHost = System.Environment.MachineName,
            //             GrainHost = grainHost,
            //             GrainId = grainId
            //         };
            //     }).Result;
        }
    }
}
