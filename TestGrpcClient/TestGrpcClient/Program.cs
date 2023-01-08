// See https://aka.ms/new-console-template for more information
using Grpc.Core;

Console.WriteLine("Hello, World!");

main().Wait();

async Task main()
{
    
    while(true)
    {
        Console.WriteLine("Press 9 to close, any other keys to start computation");
        var key = Console.ReadLine();
        if (key == "9")
        {
            Console.WriteLine("TestClient is Closing");
            break;
        }
        else
        {
            await InvokeSampleBiDirService();
        }

        Thread.Sleep(1000); 
    }

    

}

async Task InvokeSampleBiDirService()
{
    Console.WriteLine("Eneter number of requests to send to KNative grpc service");
    int num_requests = 1;
    if (!Int32.TryParse(Console.ReadLine(), out num_requests))
    {
        Console.WriteLine("Please enter valid number and try.");
            return;
    }
    if(num_requests > 20)
    {
        Console.WriteLine("Max numb requests supported is 20.");
        return;

    }

    try
    {
        var channel = new Channel("127.0.0.1:40081", ChannelCredentials.Insecure);
        var client = new SampleClient.SampleClientService.SampleClientServiceClient(channel);
        var request = new SampleClient.Request();
        request.NumRequests = num_requests;
        var resp = client.Compute(request);
        Console.WriteLine($"Compuation completed and the result is {resp.Status}");
    }
    catch(Exception ex)
    {
        Console.WriteLine($"Exception throw {ex.Message}");
    }

}
