
namespace Microsoft.Azure.SpaceFx.HostServices.Logging;

public partial class Services {
    public class LogWriterService : BackgroundService {
        private readonly ILogger<LogWriterService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly Microsoft.Azure.SpaceFx.Core.Services.PluginLoader _pluginLoader;
        private readonly Utils.PluginDelegates _pluginDelegates;
        private readonly BlockingCollection<MessageFormats.Common.LogMessage> _logMessageQueue;
        private DateTime _logFileDateTime;
        private readonly JsonSerializerOptions jsonOptions = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault, PropertyNameCaseInsensitive = true, MaxDepth = 100 };
        private readonly Models.APP_CONFIG _appConfig;
        private readonly Core.Client _client;
        private readonly string _outputDir;
        public LogWriterService(ILogger<LogWriterService> logger, IServiceProvider serviceProvider, Utils.PluginDelegates pluginDelegates, Core.Services.PluginLoader pluginLoader, Core.Client client) {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _pluginDelegates = pluginDelegates;
            _pluginLoader = pluginLoader;
            _appConfig = new Models.APP_CONFIG();
            _logFileDateTime = DateTime.UtcNow;
            _logMessageQueue = new();
            _client = client;
            _outputDir = Path.Combine(_client.GetXFerDirectories().Result.outbox_directory, "logs");
            System.IO.Directory.CreateDirectory(_outputDir);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            while (!stoppingToken.IsCancellationRequested) {
                using (var scope = _serviceProvider.CreateScope()) {
                    string logFileName = GetLogFileName();

                    while (_logMessageQueue.Count > 0) {
                        MessageFormats.Common.LogMessage logMessage = _logMessageQueue.Take();
                        try {
                            // Call the plugins before we write
                            (MessageFormats.Common.LogMessage? output_request, string? fileName) preFileWrite =
                                   _pluginLoader.CallPlugins<MessageFormats.Common.LogMessage?, Plugins.PluginBase, string>(
                                       orig_request: logMessage, orig_response: logFileName,
                                       pluginDelegate: _pluginDelegates.PreWriteToLog);

                            // Drop out of the call if our plugins removed the request
                            if (preFileWrite.output_request == null || preFileWrite.output_request == default(MessageFormats.Common.LogMessage)) {
                                return;
                            }

                            string jsonString = JsonSerializer.Serialize(logMessage, jsonOptions);

                            File.AppendAllLines(logFileName, new[] { jsonString });

                            // Call the plugins after we wrote
                            (MessageFormats.Common.LogMessage? output_request, string? fileName) postFileWrite =
                                _pluginLoader.CallPlugins<MessageFormats.Common.LogMessage?, Plugins.PluginBase, string>(
                                    orig_request: logMessage, orig_response: logFileName,
                                    pluginDelegate: _pluginDelegates.PostWriteToLog);

                        } catch (Exception ex) {
                            _logger.LogError("Failed to write log message.  Error: {error}", ex.Message);
                        }
                    }

                    try {
                        await DownlinkLogFiles();
                    } catch (Exception ex) {
                        _logger.LogError("Failed to downlink log files.  Error: {error}", ex.Message);
                    }


                    await Task.Delay(_appConfig.HEARTBEAT_PULSE_TIMING_MS, stoppingToken);
                }
            }
        }

        /// <summary>
        /// Queues a log message to be written to disk
        /// </summary>
        /// <returns>void</returns>
        protected internal void QueueLogMessage(MessageFormats.Common.LogMessage logMessage) {
            try {
                _logger.LogTrace("Adding LogMessage to queue. (trackingId: '{trackingId}' / correlationId: '{correlationId}')", logMessage.RequestHeader.TrackingId, logMessage.RequestHeader.CorrelationId);
                _logMessageQueue.Add(logMessage);
            } catch (Exception ex) {
                _logger.LogError("Failure storing LogMessage to queue (trackingId: '{trackingId}' / correlationId: '{correlationId}').  Error: {errorMessage}", logMessage.RequestHeader.TrackingId, logMessage.RequestHeader.CorrelationId, ex.Message);
            }
        }

        internal string GetLogFileName() {

            string currentFileName = string.Format($"msft-azure-orbital-{_logFileDateTime:dd-MM-yy-HH.mm.ss}.json");
            string returnLogFileName = currentFileName; // Assume we're not cutting a new log file

            // We've exceeded our max run time - cut a new log file
            if ((DateTime.UtcNow - _logFileDateTime).TotalMinutes > _appConfig.LOG_FILE_MAX_TTL.TotalMinutes) {
                _logFileDateTime = DateTime.UtcNow;
                returnLogFileName = string.Format($"msft-azure-orbital-{_logFileDateTime:dd-MM-yy-HH.mm.ss}.json");
            }

            // Log file will exceed the maximum size if we add another log message - cut a new log file
            if (File.Exists(Path.Combine(_outputDir, returnLogFileName)) && (new FileInfo(Path.Combine(_outputDir, returnLogFileName)).Length / 1024) > (_appConfig.LOG_FILE_MAX_SIZE_KB * .9)) {
                _logFileDateTime = DateTime.UtcNow;
                returnLogFileName = string.Format($"msft-azure-orbital-{_logFileDateTime:dd-MM-yy-HH.mm.ss}.json");
            }

            return Path.Combine(_outputDir, returnLogFileName);
        }

        internal Task DownlinkLogFiles() => Task.Run(async () => {
            string currentLogFileName = GetLogFileName();
            foreach (string file in Directory.GetFiles(_outputDir)) {
                if (file == currentLogFileName) continue; // Don't send the current log file
                _logger.LogDebug("Current log file: '{currentLogFileName}'", currentLogFileName);
                _logger.LogInformation("Downlinking '{currentFileName}'", file);

                // Need this deployment does not delete yaml
                MessageFormats.HostServices.Link.LinkRequest linkRequest = new() {
                    DestinationAppId = $"platform-{nameof(MessageFormats.Common.PlatformServices.Mts).ToLower()}",
                    ExpirationTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow.AddHours(12)),
                    Subdirectory = "logs",
                    FileName = System.IO.Path.GetFileName(file),
                    LeaveSourceFile = false,
                    LinkType = MessageFormats.HostServices.Link.LinkRequest.Types.LinkType.Downlink,
                    Priority = MessageFormats.Common.Priority.Medium,
                    RequestHeader = new() {
                        TrackingId = Guid.NewGuid().ToString(),
                        CorrelationId = Guid.NewGuid().ToString()
                    }
                };
                await _client.DirectToApp(appId: $"hostsvc-{nameof(MessageFormats.Common.HostServices.Link).ToLower()}", message: linkRequest);

                _logger.LogDebug("Downlink of '{currentFileName}' complete.", file);
            }
        });


    }
}
