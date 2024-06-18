namespace Microsoft.Azure.SpaceFx.HostServices.Sensor.Plugins;
public class IntegrationTestPlugin : Microsoft.Azure.SpaceFx.HostServices.Logging.Plugins.PluginBase {
    private LogMessage? CAPTURED_LOG_MESSAGE;
    private LogMessageResponse? CAPTURED_LOG_RESPONSE;

    public override ILogger Logger { get; set; }

    public IntegrationTestPlugin() {
        LoggerFactory loggerFactory = new();
        Logger = loggerFactory.CreateLogger<IntegrationTestPlugin>();
    }

    public override Task BackgroundTask() => Task.Run(async () => {
        Logger.LogInformation("Started background task!");
    });

    public override void ConfigureLogging(ILoggerFactory loggerFactory) => Logger = loggerFactory.CreateLogger<IntegrationTestPlugin>();

    public override Task<PluginHealthCheckResponse> PluginHealthCheckResponse() => Task.FromResult(new MessageFormats.Common.PluginHealthCheckResponse());

    public override Task<HeartBeatPulse?> HeartBeatPulse(HeartBeatPulse input_request) {
        throw new NotImplementedException();
    }
    public override Task<(LogMessage?, LogMessageResponse?)> LogMessageReceived(LogMessage? input_request, LogMessageResponse? input_response) => Task.Run(() => {
        if (input_request == null || input_response == null) return (input_request, input_response);

        if (input_request.RequestHeader.AppId == "hostsvc-logging-client" && input_request.Message == "Sending an info message") {
            Logger.LogInformation("Received log message from '{appId}'", "hostsvc-logging-client");
            CAPTURED_LOG_MESSAGE = input_request;
            CAPTURED_LOG_RESPONSE = input_response;
        }

        return (input_request, input_response);
    });

    public override Task<TelemetryMetric?> TelemetryMetricReceived(TelemetryMetric? input_request) => Task.Run(() => {
        return input_request;
    });


    public override Task<(LogMessage?, string?)> PreWriteToLog(LogMessage? input_request, string? input_response) => Task.Run(() => {
        return (input_request, input_response);
    });

    public override Task<(LogMessage?, string?)> PostWriteToLog(LogMessage? input_request, string? input_response) => Task.Run(() => {
        if (CAPTURED_LOG_RESPONSE == null) return (input_request, input_response);

        CAPTURED_LOG_RESPONSE.ResponseHeader.Message = $"Successfully wrote file to '{input_response}'";
        CAPTURED_LOG_RESPONSE.ResponseHeader.TrackingId = Guid.NewGuid().ToString(); // Change the tracking ID so the test can tell the difference
        CAPTURED_LOG_RESPONSE.ResponseHeader.Status = StatusCodes.Ready;
        Core.DirectToApp(appId: CAPTURED_LOG_MESSAGE!.RequestHeader.AppId, message: CAPTURED_LOG_RESPONSE);
        return (input_request, input_response);
    });

    public override Task<(TelemetryMetric?, TelemetryMetricResponse?)> TelemetryMetricResponse(TelemetryMetric? input_request, TelemetryMetricResponse? input_response) => Task.Run(() => {
        return (input_request, input_response);
    });
}
