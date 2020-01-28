namespace Orleans.Sample.GrainInterfaces
{
    using System.Threading.Tasks;
    
    public interface ISampleGrain : IGrainWithIntegerKey
    {
        Task<string> GetHost();
    }
}
