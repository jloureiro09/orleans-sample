namespace Orleans.Sample.Grains
{
    using System.Threading.Tasks;
    using Orleans.Sample.GrainInterfaces;

    public class SampleGrain : Orleans.Grain, ISampleGrain
    {
        public Task<string> GetHost()
        {
            return Task.FromResult(System.Environment.MachineName);
        }
    }
}
