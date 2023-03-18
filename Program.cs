using Grpc.Net.Client;
using Microsoft.Extensions.Hosting;
using System.Net;

internal class Program
{
    private static void Main(string[] args)
    {
        var host = new HostBuilder()
            .ConfigureFunctionsWorkerDefaults()
            .Build();

        HttpClient.DefaultProxy = new WebProxy();

        using var channel = GrpcChannel.ForAddress(@"http://localhost:7007");
        //var client = new AlgorithmRunner.AlgorithmRunnerClient(channel);
        //using var call = client.RunAlgorithm(new RunAlgorithmRequest());

        host.Run();
    }
}