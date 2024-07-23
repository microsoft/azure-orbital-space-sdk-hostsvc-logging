
namespace PayloadApp.DebugClient;

public class MessageHandler<T> : Microsoft.Azure.SpaceFx.Core.IMessageHandler<T> where T : notnull {
    private readonly ILogger<MessageHandler<T>> _logger;
    private readonly Microsoft.Azure.SpaceFx.Core.Services.PluginLoader _pluginLoader;
    private readonly IServiceProvider _serviceProvider;
    public MessageHandler(ILogger<MessageHandler<T>> logger, Microsoft.Azure.SpaceFx.Core.Services.PluginLoader pluginLoader, IServiceProvider serviceProvider) {
        _logger = logger;
        _pluginLoader = pluginLoader;
        _serviceProvider = serviceProvider;
    }

    public void MessageReceived(T message, Microsoft.Azure.SpaceFx.MessageFormats.Common.DirectToApp fullMessage) {
        using (var scope = _serviceProvider.CreateScope()) {
            _logger.LogInformation($"Found {typeof(T).Name}");
            switch (typeof(T).Name) {
                case string messageType when messageType.Equals(typeof(LogMessageResponse).Name, StringComparison.CurrentCultureIgnoreCase):
                    LogMessageResponseHandler(response: message as LogMessageResponse);
                    break;
                case string messageType when messageType.Equals(typeof(PluginConfigurationResponse).Name, StringComparison.CurrentCultureIgnoreCase):
                    PluginConfigurationResponseHandler(response: message as PluginConfigurationResponse);
                    break;
            }
        }
    }

    public void LogMessageResponseHandler(LogMessageResponse response) {
        _logger.LogInformation($"LogMessageResponse: {response.ResponseHeader.Status}. TrackingID: {response.ResponseHeader.TrackingId}");
    
    public void PluginConfigurationResponseHandler(PluginConfigurationResponse response) {
        _logger.LogInformation($"PluginConfigurationResponse: {response.ResponseHeader.Status}. TrackingID: {response.ResponseHeader.TrackingId}");
    }


}