using Microsoft.Azure.SpaceFx.MessageFormats.Common;

namespace Microsoft.Azure.SpaceFx.HostServices.Logging.Plugins;
public abstract class PluginBase : Core.IPluginBase, IPluginBase {
    public abstract ILogger Logger { get; set; }
    public abstract Task BackgroundTask();
    public abstract void ConfigureLogging(ILoggerFactory loggerFactory);
    public abstract Task<PluginHealthCheckResponse> PluginHealthCheckResponse();

    // Logging Service Stuff
    public abstract Task<(LogMessage?, LogMessageResponse?)> LogMessageReceived(LogMessage? input_request, LogMessageResponse? input_response);
    public abstract Task<TelemetryMetric?> TelemetryMetricReceived(TelemetryMetric input_request);
    public abstract Task<(TelemetryMetric?, TelemetryMetricResponse?)> TelemetryMetricResponse(TelemetryMetric? input_request, TelemetryMetricResponse? input_response);

    public abstract Task<(LogMessage?, string?)> PreWriteToLog(LogMessage? input_request, string? fileName);
    public abstract Task<(LogMessage?, string?)> PostWriteToLog(LogMessage? input_request, string? fileName);
}

public interface IPluginBase {
    ILogger Logger { get; set; }
    Task<(LogMessage?, LogMessageResponse?)> LogMessageReceived(LogMessage? input_request, LogMessageResponse? input_response);
    Task<TelemetryMetric?> TelemetryMetricReceived(TelemetryMetric input_request);
    Task<(TelemetryMetric?, TelemetryMetricResponse?)> TelemetryMetricResponse(TelemetryMetric? input_request, TelemetryMetricResponse? input_response);
    Task<(LogMessage?, string?)> PreWriteToLog(LogMessage? input_request, string? fileName);
    Task<(LogMessage?, string?)> PostWriteToLog(LogMessage? input_request, string? fileName);
}
