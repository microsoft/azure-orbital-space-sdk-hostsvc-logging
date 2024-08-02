
namespace Microsoft.Azure.SpaceFx.HostServices.Logging;

public partial class Services {
    public class LogWriterService : BackgroundService, Core.IMonitorableService {
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
        public bool IsHealthy() {
            var files = Directory.GetFiles(_outputDir);
            // Check to make sure there's at least one log file and no more than two.
            // One log file means there's a current log file, two means there's a current log file and a previous log file that hasn't been downlinked yet.
            // This is expected behavior and means the service is writing logs and downlinking them correctly.
            if (files.Length <= 2) {
                return true;
            }

            // There's either:
            //    no log files: meaning the service isn't writing logs like its supposed to,
            //    there's more than two log files: meaning the service isn't downlinking logs like its supposed to.
            // Both are failures and need a restart.
            return false;
        }
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
                using var scope = _serviceProvider.CreateScope();
                string logFileName = GetLogFileName();

                // Process log messages in the queue
                while (_logMessageQueue.Count > 0) {
                    var logMessage = _logMessageQueue.Take();
                    try {
                        // Call pre-write plugins
                        (MessageFormats.Common.LogMessage? output_request, string? fileName) preFileWrite = _pluginLoader.CallPlugins<MessageFormats.Common.LogMessage?, Plugins.PluginBase, string>(
                            orig_request: logMessage, orig_response: logFileName, pluginDelegate: _pluginDelegates.PreWriteToLog);

                        // Skip if the pre-write plugin nullifies the request
                        if (preFileWrite.output_request == null) {
                            continue;
                        }

                        // Serialize the log message to JSON
                        string jsonString = JsonSerializer.Serialize(logMessage, jsonOptions);

                        // Append the serialized log message to the log file
                        await File.AppendAllLinesAsync(logFileName, new string[] { jsonString }, stoppingToken);

                        // Call post-write plugins
                        _pluginLoader.CallPlugins<MessageFormats.Common.LogMessage?, Plugins.PluginBase, string>(
                            orig_request: logMessage, orig_response: logFileName, pluginDelegate: _pluginDelegates.PostWriteToLog);
                    } catch (Exception ex) {
                        _logger.LogError("Failed to process log message. Error: {error}", ex.Message);
                    }
                }

                try {
                    // Downlink log files
                    await DownlinkLogFiles();
                } catch (Exception ex) {
                    _logger.LogError("Failed to downlink log files. Error: {error}", ex.Message);
                }

                // Wait for the specified heartbeat pulse timing before the next iteration
                await Task.Delay(_appConfig.HEARTBEAT_PULSE_TIMING_MS, stoppingToken);
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

        /// <summary>
        /// Calculate the log file name based on the current datetime, the maximum allowed time-to-live (TTL), and the maximum allowed size
        /// </summary>
        /// <returns>Full path to the expected log filename</returns>
        internal string GetLogFileName() {
            // Generate the initial log file name based on the current date and time
            string currentFileName = string.Format($"msft-azure-orbital-{_logFileDateTime:dd-MM-yy-HH.mm.ss}.json");
            string returnLogFileName = currentFileName; // Assume we're not cutting a new log file

            // Check if the current log file has exceeded the maximum allowed time-to-live (TTL)
            if ((DateTime.UtcNow - _logFileDateTime).TotalMinutes > _appConfig.LOG_FILE_MAX_TTL.TotalMinutes) {
                // Update the log file date-time to the current time
                _logFileDateTime = DateTime.UtcNow;
                // Generate a new log file name based on the updated date and time
                returnLogFileName = string.Format($"msft-azure-orbital-{_logFileDateTime:dd-MM-yy-HH.mm.ss}.json");
            }

            // Check if the current log file exists and if its size exceeds 90% of the maximum allowed size
            if (File.Exists(Path.Combine(_outputDir, returnLogFileName)) &&
                (new FileInfo(Path.Combine(_outputDir, returnLogFileName)).Length / 1024) > (_appConfig.LOG_FILE_MAX_SIZE_KB * .9)) {
                // Update the log file date-time to the current time
                _logFileDateTime = DateTime.UtcNow;
                // Generate a new log file name based on the updated date and time
                returnLogFileName = string.Format($"msft-azure-orbital-{_logFileDateTime:dd-MM-yy-HH.mm.ss}.json");
            }

            // Return the full path of the log file
            return Path.Combine(_outputDir, returnLogFileName);
        }

        /// <summary>
        /// Downlink all but the current log file to Platform MTS
        /// </summary>
        internal Task DownlinkLogFiles() => Task.Run(async () => {
            string currentLogFileName = GetLogFileName();
            var filesToDownlink = Directory.GetFiles(_outputDir).Where(file => file != currentLogFileName);

            foreach (string file in filesToDownlink) {
                _logger.LogInformation("Downlinking '{file}'", file);

                var linkRequest = new MessageFormats.HostServices.Link.LinkRequest {
                    DestinationAppId = $"platform-{nameof(MessageFormats.Common.PlatformServices.Mts).ToLower()}",
                    ExpirationTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow.AddHours(12)),
                    Subdirectory = "logs",
                    FileName = Path.GetFileName(file),
                    LeaveSourceFile = false,
                    LinkType = MessageFormats.HostServices.Link.LinkRequest.Types.LinkType.Downlink,
                    Priority = MessageFormats.Common.Priority.Medium,
                    RequestHeader = new MessageFormats.Common.RequestHeader {
                        TrackingId = Guid.NewGuid().ToString()
                    }
                };

                linkRequest.RequestHeader.CorrelationId = linkRequest.RequestHeader.TrackingId;

                await _client.DirectToApp(appId: $"hostsvc-{nameof(MessageFormats.Common.HostServices.Link).ToLower()}", message: linkRequest);

                _logger.LogDebug("Downlink of '{file}' complete.", file);
            }
        });
    }
}
