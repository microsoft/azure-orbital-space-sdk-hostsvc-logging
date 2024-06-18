namespace Microsoft.Azure.SpaceFx.HostServices.Logging;

public partial class MessageHandler<T> : Microsoft.Azure.SpaceFx.Core.IMessageHandler<T> where T : notnull {
    private readonly ILogger<MessageHandler<T>> _logger;
    private readonly Utils.PluginDelegates _pluginDelegates;
    private readonly Microsoft.Azure.SpaceFx.Core.Services.PluginLoader _pluginLoader;
    private readonly IServiceProvider _serviceProvider;
    private readonly Core.Client _client;
    private readonly Models.APP_CONFIG _appConfig;
    private readonly Services.LogWriterService _logWriterService;
    public MessageHandler(ILogger<MessageHandler<T>> logger, Utils.PluginDelegates pluginDelegates, Microsoft.Azure.SpaceFx.Core.Services.PluginLoader pluginLoader, IServiceProvider serviceProvider, Core.Client client) {
        _logger = logger;
        _pluginDelegates = pluginDelegates;
        _pluginLoader = pluginLoader;
        _serviceProvider = serviceProvider;
        _appConfig = new Models.APP_CONFIG();
        _client = client;
        _logWriterService = _serviceProvider.GetRequiredService<Services.LogWriterService>();
    }

    public void MessageReceived(T message, MessageFormats.Common.DirectToApp fullMessage) => Task.Run(() => {
        using (var scope = _serviceProvider.CreateScope()) {

            if (message == null || EqualityComparer<T>.Default.Equals(message, default)) {
                _logger.LogDebug("Received empty message '{messageType}' from '{appId}'.  Discarding message.", typeof(T).Name, fullMessage.SourceAppId);
                return;
            }

            switch (typeof(T).Name) {
                case string messageType when messageType.Equals(typeof(MessageFormats.Common.TelemetryMetric).Name, StringComparison.CurrentCultureIgnoreCase):
                    TelemetryMetricHandler(message: message as MessageFormats.Common.TelemetryMetric, fullMessage: fullMessage);
                    break;
                case string messageType when messageType.Equals(typeof(MessageFormats.Common.TelemetryMultiMetric).Name, StringComparison.CurrentCultureIgnoreCase):
                    TelemetryMultiMetricHandler(message: message as MessageFormats.Common.TelemetryMultiMetric, fullMessage: fullMessage);
                    break;
                case string messageType when messageType.Equals(typeof(MessageFormats.Common.LogMessage).Name, StringComparison.CurrentCultureIgnoreCase):
                    LogMessageHandler(message: message as MessageFormats.Common.LogMessage, fullMessage: fullMessage);
                    break;
            }
        }
    });
}
