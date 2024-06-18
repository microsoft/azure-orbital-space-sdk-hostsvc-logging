using Microsoft.Azure.SpaceFx.MessageFormats.Common;

namespace Microsoft.Azure.SpaceFx.HostServices.Logging;

public partial class MessageHandler<T> {
    private void TelemetryMetricHandler(MessageFormats.Common.TelemetryMetric? message, MessageFormats.Common.DirectToApp fullMessage) {
        if (message == null) return;
        using (var scope = _serviceProvider.CreateScope()) {
            MessageFormats.Common.TelemetryMetricResponse? returnResponse = ProcessTelemetryMessage(message: message, fullMessage: fullMessage);

            if (returnResponse == null) {
                _logger.LogDebug("Plugins nullified '{messageType}' from '{sourceApp}'.  Dropping message (trackingId: '{trackingId}' / correlationId: '{correlationId}')", message.GetType().Name, fullMessage.SourceAppId, message.RequestHeader.TrackingId, message.RequestHeader.CorrelationId);
                return;
            }

            (MessageFormats.Common.TelemetryMetric? output_request, MessageFormats.Common.TelemetryMetricResponse? output_response) =
                                           _pluginLoader.CallPlugins<MessageFormats.Common.TelemetryMetric?, Plugins.PluginBase, MessageFormats.Common.TelemetryMetricResponse>(
                                               orig_request: message, orig_response: returnResponse,
                                               pluginDelegate: _pluginDelegates.TelemetryMetricResponse);

            if (output_response == null || output_request == null) {
                _logger.LogTrace("Plugins nullified '{messageType}' or '{output_requestMessageType}' from '{sourceApp}'.  Dropping Message (trackingId: '{trackingId}' / correlationId: '{correlationId}')", returnResponse.GetType().Name, message.GetType().Name, fullMessage.SourceAppId, message.RequestHeader.TrackingId, message.RequestHeader.CorrelationId);
                return;
            }

            returnResponse = output_response;

            _logger.LogDebug("Sending response type '{messageType}' to '{appId}'  (trackingId: '{trackingId}' / correlationId: '{correlationId}')", returnResponse.GetType().Name, fullMessage.SourceAppId, returnResponse.ResponseHeader.TrackingId, returnResponse.ResponseHeader.CorrelationId);

            // Route the response back to the calling user
            _client.DirectToApp(appId: fullMessage.SourceAppId, message: returnResponse);
        };
    }

    private void TelemetryMultiMetricHandler(MessageFormats.Common.TelemetryMultiMetric? message, MessageFormats.Common.DirectToApp fullMessage) {
        // Check if the message is null, if so, return immediately
        if (message == null) return;

        // Create a new scope for the service provider
        using (var scope = _serviceProvider.CreateScope()) {
            // Create a new response from the request message
            MessageFormats.Common.TelemetryMultiMetricResponse returnResponse = Core.Utils.ResponseFromRequest(message, new MessageFormats.Common.TelemetryMultiMetricResponse());

            // Log the processing of the message
            _logger.LogDebug("Processing message type '{messageType}' from '{sourceApp}' (trackingId: '{trackingId}' / correlationId: '{correlationId}')", message.GetType().Name, fullMessage.SourceAppId, message.RequestHeader.TrackingId, message.RequestHeader.CorrelationId);

            // Loop through the telemetry messages and handle each one like a normal telemetry message
            foreach (MessageFormats.Common.TelemetryMetric telemetryMetric in message.TelemetryMetrics) {
                // Process each telemetry message
                MessageFormats.Common.TelemetryMetricResponse? metricResponse = ProcessTelemetryMessage(message: telemetryMetric, fullMessage: fullMessage);

                // If the response is null, log the event and continue to the next iteration
                if (metricResponse == null) {
                    _logger.LogDebug("Plugins nullified '{messageType}' from '{sourceApp}'.  Dropping message (trackingId: '{trackingId}' / correlationId: '{correlationId}')", message.GetType().Name, fullMessage.SourceAppId, message.RequestHeader.TrackingId, message.RequestHeader.CorrelationId);
                    continue;
                }

                // Call plugins and get the output request and response
                (MessageFormats.Common.TelemetryMetric? output_request, MessageFormats.Common.TelemetryMetricResponse? output_response) =
                                               _pluginLoader.CallPlugins<MessageFormats.Common.TelemetryMetric?, Plugins.PluginBase, MessageFormats.Common.TelemetryMetricResponse>(
                                                   orig_request: telemetryMetric, orig_response: metricResponse,
                                                   pluginDelegate: _pluginDelegates.TelemetryMetricResponse);

                // If the output response or request is null, log the event and continue to the next iteration
                if (output_response == null || output_request == null) {
                    _logger.LogTrace("Plugins nullified '{messageType}' or '{output_requestMessageType}' from '{sourceApp}'.  Dropping Message (trackingId: '{trackingId}' / correlationId: '{correlationId}')", returnResponse.GetType().Name, message.GetType().Name, fullMessage.SourceAppId, message.RequestHeader.TrackingId, message.RequestHeader.CorrelationId);
                    continue;
                }

                // Update the metric response with the output response
                metricResponse = output_response;
            }

            // Set the status of the response header to successful
            returnResponse.ResponseHeader.Status = MessageFormats.Common.StatusCodes.Successful;

            // Log the sending of the response
            _logger.LogDebug("Sending response type '{messageType}' to '{appId}'  (trackingId: '{trackingId}' / correlationId: '{correlationId}')", returnResponse.GetType().Name, fullMessage.SourceAppId, returnResponse.ResponseHeader.TrackingId, returnResponse.ResponseHeader.CorrelationId);

            // Send the response directly to the app
            _client.DirectToApp(appId: fullMessage.SourceAppId, message: returnResponse);
        };
    }

    private MessageFormats.Common.TelemetryMetricResponse? ProcessTelemetryMessage(MessageFormats.Common.TelemetryMetric? message, MessageFormats.Common.DirectToApp fullMessage) {
        // Check if the message is null, if so, return null
        if (message == null) return null;

        // Create a new response from the request message
        MessageFormats.Common.TelemetryMetricResponse returnResponse = Core.Utils.ResponseFromRequest(message, new MessageFormats.Common.TelemetryMetricResponse());

        // If the MetricTime in the message is null, set it to the current UTC time
        if (message.MetricTime == null) {
            message.MetricTime = Timestamp.FromDateTime(DateTime.UtcNow.ToUniversalTime());
        }

        // Log the processing of the message
        _logger.LogDebug("Processing message type '{messageType}' from '{sourceApp}' (trackingId: '{trackingId}' / correlationId: '{correlationId}')", message.GetType().Name, fullMessage.SourceAppId, message.RequestHeader.TrackingId, message.RequestHeader.CorrelationId);

        // Call plugins and get the result
        MessageFormats.Common.TelemetryMetric? pluginResult =
           _pluginLoader.CallPlugins<MessageFormats.Common.TelemetryMetric?, Plugins.PluginBase>(
               orig_request: message,
               pluginDelegate: _pluginDelegates.TelemetryLogMessageReceived);

        // Log that the plugins have finished processing
        _logger.LogTrace("Plugins finished processing '{messageType}' (trackingId: '{trackingId}' / correlationId: '{correlationId}')", message.GetType().Name, message.RequestHeader.TrackingId, message.RequestHeader.CorrelationId);

        // If the plugin result is null, log the event and return null
        if (pluginResult == null) {
            _logger.LogDebug("Plugins nullified '{messageType}' from '{sourceApp}'.  Dropping message (trackingId: '{trackingId}' / correlationId: '{correlationId}')", message.GetType().Name, fullMessage.SourceAppId, message.RequestHeader.TrackingId, message.RequestHeader.CorrelationId);
            return null;
        }

        // Set the status of the response header to successful
        returnResponse.ResponseHeader.Status = MessageFormats.Common.StatusCodes.Successful;

        // If the app config is set to write telemetry to log, log the telemetry
        if (_appConfig.WRITE_TELEMETRY_TO_LOG) {
            _logger.LogTrace("WRITE_TELEMETRY_TO_LOG = 'true'.  Converting '{messageType}' to '{LogMessageTytpe}' and adding to Log Writer Service queue (trackingId: '{trackingId}' / correlationId: '{correlationId}')", message.GetType().Name, typeof(LogMessage).Name, message.RequestHeader.TrackingId, message.RequestHeader.CorrelationId);

            // Create a new log message
            MessageFormats.Common.LogMessage logMessage = new() {
                Category = "Telemetry",
                Message = message.MetricValue.ToString(),
                RequestHeader = message.RequestHeader,
                LogLevel = MessageFormats.Common.LogMessage.Types.LOG_LEVEL.Telemetry,
                SubCategory = message.MetricName,
                LogReceivedTime = Timestamp.FromDateTime(message.MetricTime.ToDateTime()),
                LogTimeUserReadable = message.MetricTime.ToDateTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };

            // Queue the log message in the Log Writer Service
            _serviceProvider.GetRequiredService<Services.LogWriterService>().QueueLogMessage(logMessage);
        }

        // Call plugins and get the output request and response
        (MessageFormats.Common.TelemetryMetric? output_request, MessageFormats.Common.TelemetryMetricResponse? output_response) =
                                       _pluginLoader.CallPlugins<MessageFormats.Common.TelemetryMetric?, Plugins.PluginBase, MessageFormats.Common.TelemetryMetricResponse>(
                                           orig_request: message, orig_response: returnResponse,
                                           pluginDelegate: _pluginDelegates.TelemetryMetricResponse);

        // Return the response
        return returnResponse;
    }
}
