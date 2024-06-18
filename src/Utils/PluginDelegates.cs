using Microsoft.Azure.SpaceFx.MessageFormats.Common;

namespace Microsoft.Azure.SpaceFx.HostServices.Logging;
public class Utils {
    public class PluginDelegates {
        private readonly ILogger<PluginDelegates> _logger;
        private readonly List<Core.Models.PLUG_IN> _plugins;
        public PluginDelegates(ILogger<PluginDelegates> logger, IServiceProvider serviceProvider) {
            _logger = logger;
            _plugins = serviceProvider.GetService<List<Core.Models.PLUG_IN>>() ?? new List<Core.Models.PLUG_IN>();
        }

        internal (MessageFormats.Common.LogMessage? output_request, MessageFormats.Common.LogMessageResponse? output_response) LogMessageReceived((MessageFormats.Common.LogMessage? input_request, MessageFormats.Common.LogMessageResponse? input_response, Plugins.PluginBase plugin) input) {
            const string methodName = nameof(input.plugin.LogMessageReceived);
            if (input.input_request is null || input.input_request is default(MessageFormats.Common.LogMessage)) {
                _logger.LogDebug("Plugin {Plugin_Name} / {methodName}: Received empty input.  Returning empty results", input.plugin.ToString(), methodName);
                return (input.input_request, input.input_response);
            }
            _logger.LogDebug("Plugin {Plugin_Name} / {methodName}: START", input.plugin.ToString(), methodName);

            try {
                Task<(MessageFormats.Common.LogMessage? output_request, MessageFormats.Common.LogMessageResponse? output_response)> pluginTask = input.plugin.LogMessageReceived(input_request: input.input_request, input_response: input.input_response);
                pluginTask.Wait();

                input.input_request = pluginTask.Result.output_request;
                input.input_response = pluginTask.Result.output_response;
            } catch (Exception ex) {
                _logger.LogError("Error in plugin '{Plugin_Name}:{methodName}'.  Error: {errMsg}", input.plugin.ToString(), methodName, ex.Message);
            }

            _logger.LogDebug("Plugin {Plugin_Name} / {methodName}: END", input.plugin.ToString(), methodName);
            return (input.input_request, input.input_response);
        }

        internal (MessageFormats.Common.TelemetryMetric? output_request, MessageFormats.Common.TelemetryMetricResponse? output_response) TelemetryMetricResponse((MessageFormats.Common.TelemetryMetric? input_request, MessageFormats.Common.TelemetryMetricResponse? input_response, Plugins.PluginBase plugin) input) {
            const string methodName = nameof(input.plugin.LogMessageReceived);
            if (input.input_request is null || input.input_request is default(MessageFormats.Common.TelemetryMetric)) {
                _logger.LogDebug("Plugin {Plugin_Name} / {methodName}: Received empty input.  Returning empty results", input.plugin.ToString(), methodName);
                return (input.input_request, input.input_response);
            }
            _logger.LogDebug("Plugin {Plugin_Name} / {methodName}: START", input.plugin.ToString(), methodName);

            try {
                Task<(MessageFormats.Common.TelemetryMetric? output_request, MessageFormats.Common.TelemetryMetricResponse? output_response)> pluginTask = input.plugin.TelemetryMetricResponse(input_request: input.input_request, input_response: input.input_response);
                pluginTask.Wait();

                input.input_request = pluginTask.Result.output_request;
                input.input_response = pluginTask.Result.output_response;
            } catch (Exception ex) {
                _logger.LogError("Error in plugin '{Plugin_Name}:{methodName}'.  Error: {errMsg}", input.plugin.ToString(), methodName, ex.Message);
            }

            _logger.LogDebug("Plugin {Plugin_Name} / {methodName}: END", input.plugin.ToString(), methodName);
            return (input.input_request, input.input_response);
        }

        internal (MessageFormats.Common.LogMessage? output_request, string? output_response) PreWriteToLog((MessageFormats.Common.LogMessage? input_request, string? input_response, Plugins.PluginBase plugin) input) {
            const string methodName = nameof(input.plugin.PreWriteToLog);
            if (input.input_request is null || input.input_request is default(MessageFormats.Common.LogMessage)) {
                _logger.LogDebug("Plugin {Plugin_Name} / {methodName}: Received empty input.  Returning empty results", input.plugin.ToString(), methodName);
                return (input.input_request, input.input_response);
            }
            _logger.LogDebug("Plugin {Plugin_Name} / {methodName}: START", input.plugin.ToString(), methodName);

            try {
                Task<(MessageFormats.Common.LogMessage? output_request, string? output_response)> pluginTask = input.plugin.PreWriteToLog(input_request: input.input_request, fileName: input.input_response);
                pluginTask.Wait();

                input.input_request = pluginTask.Result.output_request;
                input.input_response = pluginTask.Result.output_response;
            } catch (Exception ex) {
                _logger.LogError("Error in plugin '{Plugin_Name}:{methodName}'.  Error: {errMsg}", input.plugin.ToString(), methodName, ex.Message);
            }

            _logger.LogDebug("Plugin {Plugin_Name} / {methodName}: END", input.plugin.ToString(), methodName);
            return (input.input_request, input.input_response);
        }

        internal (MessageFormats.Common.LogMessage? output_request, string? output_response) PostWriteToLog((MessageFormats.Common.LogMessage? input_request, string? input_response, Plugins.PluginBase plugin) input) {
            const string methodName = nameof(input.plugin.PostWriteToLog);
            if (input.input_request is null || input.input_request is default(MessageFormats.Common.LogMessage)) {
                _logger.LogDebug("Plugin {Plugin_Name} / {methodName}: Received empty input.  Returning empty results", input.plugin.ToString(), methodName);
                return (input.input_request, input.input_response);
            }
            _logger.LogDebug("Plugin {Plugin_Name} / {methodName}: START", input.plugin.ToString(), methodName);

            try {
                Task<(MessageFormats.Common.LogMessage? output_request, string? output_response)> pluginTask = input.plugin.PostWriteToLog(input_request: input.input_request, fileName: input.input_response);
                pluginTask.Wait();

                input.input_request = pluginTask.Result.output_request;
                input.input_response = pluginTask.Result.output_response;
            } catch (Exception ex) {
                _logger.LogError("Error in plugin '{Plugin_Name}:{methodName}'.  Error: {errMsg}", input.plugin.ToString(), methodName, ex.Message);
            }

            _logger.LogDebug("Plugin {Plugin_Name} / {methodName}: END", input.plugin.ToString(), methodName);
            return (input.input_request, input.input_response);
        }

        internal MessageFormats.Common.TelemetryMetric? TelemetryLogMessageReceived((MessageFormats.Common.TelemetryMetric? input_request, Plugins.PluginBase plugin) input) {
            const string methodName = nameof(input.plugin.TelemetryMetricReceived);

            if (input.input_request is null || input.input_request is default(MessageFormats.Common.TelemetryMetric)) {
                _logger.LogDebug("Plugin {pluginName} / {pluginMethod}: Received empty input.  Returning empty results", input.plugin.ToString(), methodName);
                return input.input_request;
            }
            _logger.LogDebug("Plugin {pluginMethod}: START", methodName);

            try {
                Task<MessageFormats.Common.TelemetryMetric?> pluginTask = input.plugin.TelemetryMetricReceived(input_request: input.input_request);
                pluginTask.Wait();
                input.input_request = pluginTask.Result;
            } catch (Exception ex) {
                _logger.LogError("Plugin {pluginName} / {pluginMethod}: Error: {errorMessage}", input.plugin.ToString(), methodName, ex.Message);
            }

            _logger.LogDebug("Plugin {pluginName} / {pluginMethod}: END", input.plugin.ToString(), methodName);
            return input.input_request;
        }
    }
}
