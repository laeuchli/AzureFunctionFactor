using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace AzureFunctionFactor
{
    internal class Program
    {
        static void Main(string[] args)
        {
            FunctionsDebugger.Enable();

        

            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                    .ConfigureServices(services =>
                    {
                        string cosmosConnectionString = Environment.GetEnvironmentVariable("CosmosConnectionString");

                        services.AddSingleton(s => new CosmosClient(cosmosConnectionString));
                    })
                .Build();

           
            host.Run();
        }
    }
}
