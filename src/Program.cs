using Google.Api;

namespace Microsoft.Azure.SpaceFx.HostServices.Logging;

public class Program {
    public static void Main(string[] args) {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddJsonFile("/workspaces/hostsvc-logging-config/appsettings.json", optional: true, reloadOnChange: true)
                             .AddJsonFile("/workspaces/hostsvc-logging/src/appsettings.json", optional: true, reloadOnChange: true)
                             .AddJsonFile("/workspaces/hostsvc-logging/src/appsettings.{env:DOTNET_ENVIRONMENT}.json", optional: true, reloadOnChange: true)
                             .Build();

        builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(50051, o => o.Protocols = HttpProtocols.Http2))
        .ConfigureServices((services) => {
            services.AddAzureOrbitalFramework();
            services.AddSingleton<Core.IMessageHandler<MessageFormats.Common.TelemetryMetric>, MessageHandler<MessageFormats.Common.TelemetryMetric>>();
            services.AddSingleton<Core.IMessageHandler<MessageFormats.Common.TelemetryMultiMetric>, MessageHandler<MessageFormats.Common.TelemetryMultiMetric>>();
            services.AddSingleton<Core.IMessageHandler<MessageFormats.Common.LogMessage>, MessageHandler<MessageFormats.Common.LogMessage>>();

            services.AddSingleton<Utils.PluginDelegates>();

            services.AddSingleton<Services.LogWriterService>();
            services.AddHostedService<Services.LogWriterService>(p => p.GetRequiredService<Services.LogWriterService>());

        }).ConfigureLogging((logging) => {
            logging.AddProvider(new Microsoft.Extensions.Logging.SpaceFX.Logger.HostSvcLoggerProvider());
            logging.AddConsole();
        });

        var app = builder.Build();

        app.UseRouting();
        app.UseEndpoints(endpoints => {
            endpoints.MapGrpcService<Microsoft.Azure.SpaceFx.Core.Services.MessageReceiver>();
            endpoints.MapGet("/", async context => {
                await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
            });
        });
        app.Run();
    }
}
