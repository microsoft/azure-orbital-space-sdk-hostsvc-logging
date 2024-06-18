namespace PayloadApp.DebugClient;

public class MessageSender : BackgroundService {
    private readonly ILogger<MessageSender> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Microsoft.Azure.SpaceFx.Core.Client _client;
    private readonly string _appId;
    private readonly string _hostSvcAppId;

    public MessageSender(ILogger<MessageSender> logger, IServiceProvider serviceProvider) {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _client = _serviceProvider.GetService<Microsoft.Azure.SpaceFx.Core.Client>() ?? throw new NullReferenceException($"{nameof(Microsoft.Azure.SpaceFx.Core.Client)} is null");
        _appId = _client.GetAppID().Result;
        _hostSvcAppId = _appId.Replace("-client", "");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {

        using (var scope = _serviceProvider.CreateScope()) {
            _logger.LogInformation("MessageSender running at: {time}", DateTimeOffset.Now);
            await PluginConfigurationRequest();
            await LogRequest();
            await LogTelemetryMessage();
        }
    }


    private async Task LogTelemetryMessage() {
        var trackingId = Guid.NewGuid().ToString();
        var telemetryLogMessage = new TelemetryMetric() {
            RequestHeader = new() {
                TrackingId = trackingId,
            },
            MetricName = "Testing",
            MetricValue = 37
        };
        await _client.DirectToApp(appId: _hostSvcAppId, message: telemetryLogMessage);
    }

    private async Task LogRequest() {
        var trackingId = Guid.NewGuid().ToString();
        var logRequest = new LogMessage() {
            RequestHeader = new() {
                TrackingId = trackingId,
            },
            LogLevel = LogMessage.Types.LOG_LEVEL.Info,
            Message = "Message with datatypes",
            Priority = Priority.Medium,
            Category = "",
            SubCategory = ""
        };

        logRequest.DateTimeValues.Add(Timestamp.FromDateTime(DateTime.UtcNow));
        logRequest.DateTimeValues.Add(Timestamp.FromDateTime(DateTime.UtcNow.AddDays(7)));

        logRequest.StringValues.Add("String Value #1");
        logRequest.StringValues.Add("String Value #2");

        await _client.DirectToApp(appId: _hostSvcAppId, message: logRequest);
    }

    private async Task PluginConfigurationRequest() {
        PluginConfigurationRequest request = new() {
            RequestHeader = new() {
                TrackingId = Guid.NewGuid().ToString()
            }
        };

        Console.WriteLine("Sending Plugin Configuration Request");

        await _client.DirectToApp(appId: _hostSvcAppId, message: request);
    }



}
