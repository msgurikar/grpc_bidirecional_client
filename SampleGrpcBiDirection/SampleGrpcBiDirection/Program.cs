// See https://aka.ms/new-console-template for more information
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SampleGrpcBiDirection;

int Port = 40081;

ProcessArgs(args);
MainAysnc().Wait();

/// <summary>
/// Starts the Processing Framework.
/// </summary>
/// <returns>Awaitable Task.</returns>
async Task MainAysnc()
{    

    ConfigureLogFile();
    var hostBuilder = new HostBuilder()
        .ConfigureLogging((hostingContext, logging) =>
        {
            logging.AddConsole().SetMinimumLevel(LogLevel.Information);

        }).ConfigureServices((hostContext, services) =>
        {
            Server server = new Server
            {
                Services = { SampleClient.SampleClientService.BindService(new SampleClientProviderImpl()) },
                Ports = { new ServerPort("0.0.0.0", Port, ServerCredentials.Insecure) }
            };
            services.AddSingleton<Server>(server);
            services.AddSingleton<IHostedService, GrpcHostedService>();

        });


    await hostBuilder.RunConsoleAsync();
}

/// <summary>
/// Process command line arguments.
/// </summary>
/// <param name="args">Command line arguments</param>
/// <returns>True if parsing successful, otherwise False.</returns>
bool ProcessArgs(string[] args)
{
    int port = 0;
    bool showHelp = false;

    return true;
}



/// <summary>
/// Configuring log file.
/// </summary>
static void ConfigureLogFile()
{
    var data = System.IO.Directory.GetCurrentDirectory();
    var logPath = data + "/Logs";
    Console.WriteLine($"Current director is {logPath}");
    if (!Directory.Exists(logPath))
    {
        Directory.CreateDirectory(logPath);
    }
    var config = new NLog.Config.LoggingConfiguration();
    var logfile = new NLog.Targets.FileTarget() { FileName = $"{logPath + "/Logfile_" + DateTime.Now.ToString("yyyy-dd-M-HH-mm-ss") + ".txt"}" };

    config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Off, logfile);
    NLog.LogManager.Configuration = config;
}



/// <summary>
/// Hostable gRPC Service.
/// </summary>
public class GrpcHostedService : IHostedService
{
    private Server _server;
    private readonly ILogger<GrpcHostedService> _logger;

    public GrpcHostedService(Server server, ILogger<GrpcHostedService> logger)
    {
        _server = server;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _server.Start();
        _logger.Log(LogLevel.Information, "server listening on port " + _server.Ports.FirstOrDefault().Port);
        Console.WriteLine($"server listening on port {_server.Ports.FirstOrDefault().Port}");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.Log(LogLevel.Information, "spark server is shutting down");
        _server.ShutdownAsync().Wait();
        return Task.CompletedTask;
    }
}
