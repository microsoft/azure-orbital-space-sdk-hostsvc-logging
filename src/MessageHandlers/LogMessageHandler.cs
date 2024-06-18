namespace Microsoft.Azure.SpaceFx.HostServices.Logging;

public partial class MessageHandler<T> {
    private void LogMessageHandler(MessageFormats.Common.LogMessage? message, MessageFormats.Common.DirectToApp fullMessage) {
        if (message == null) return;
        using (var scope = _serviceProvider.CreateScope()) {

            MessageFormats.Common.LogMessageResponse returnResponse = new() { };

            returnResponse = new() {
                ResponseHeader = new() {
                    TrackingId = message.RequestHeader.TrackingId,
                    CorrelationId = message.RequestHeader.CorrelationId,
                    Status = MessageFormats.Common.StatusCodes.Successful
                }
            };

            message.LogReceivedTime = Timestamp.FromDateTime(DateTime.UtcNow.ToUniversalTime());
            message.LogTimeUserReadable = DateTime.UtcNow.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            _logger.LogTrace($"Passing message '{message.GetType().Name}' and '{returnResponse.GetType().Name}' from '{fullMessage.SourceAppId}' to plugins (trackingId: '{message.RequestHeader.TrackingId}' / correlationId: '{message.RequestHeader.CorrelationId}')");

            (MessageFormats.Common.LogMessage? output_request, MessageFormats.Common.LogMessageResponse? output_response) =
                                            _pluginLoader.CallPlugins<MessageFormats.Common.LogMessage?, Plugins.PluginBase, MessageFormats.Common.LogMessageResponse>(
                                                orig_request: message, orig_response: returnResponse,
                                                pluginDelegate: _pluginDelegates.LogMessageReceived);

            _logger.LogTrace($"Plugins finished processing '{message.GetType().Name}' and '{returnResponse.GetType().Name}' from '{fullMessage.SourceAppId}' (trackingId: '{message.RequestHeader.TrackingId}' / correlationId: '{message.RequestHeader.CorrelationId}')");

            if (output_response == null || output_request == null) {
                _logger.LogTrace($"Plugins nullified '{message.GetType().Name}' or '{returnResponse.GetType().Name}' from '{fullMessage.SourceAppId}' (trackingId: '{message.RequestHeader.TrackingId}' / correlationId: '{message.RequestHeader.CorrelationId}')");
                return;
            }

            returnResponse = output_response;
            message = output_request;

            _serviceProvider.GetRequiredService<Services.LogWriterService>().QueueLogMessage(message);

            // Do not send a log message response if this was sent by the custom logger - otherwise a race condition will be presented
            if (!string.Equals(message.Category, "MSFTSpaceFxLoggingModule", StringComparison.InvariantCultureIgnoreCase))
                _client.DirectToApp(appId: fullMessage.SourceAppId, message: returnResponse).Wait();
        };
    }
}
