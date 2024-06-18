namespace Microsoft.Azure.SpaceFx.HostServices.Sensor.IntegrationTests.Tests;

[Collection(nameof(TestSharedContext))]
public class LogResponseTests : IClassFixture<TestSharedContext> {
    readonly TestSharedContext _context;

    public LogResponseTests(TestSharedContext context) {
        _context = context;
    }

    [Fact]
    public async Task LogTests() {
        DateTime maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);
        MessageFormats.Common.LogMessageResponse? response = null;
        MessageFormats.Common.LogMessageResponse? write_logResponse = null;

        MessageFormats.Common.LogMessage testMessage = new() {
            RequestHeader = new MessageFormats.Common.RequestHeader() {
                TrackingId = Guid.NewGuid().ToString(),
                CorrelationId = Guid.NewGuid().ToString()
            },
            LogLevel = MessageFormats.Common.LogMessage.Types.LOG_LEVEL.Info,
            Message = "Sending an info message"
        };

        // Register a callback event to catch the response
        void LogMessageAvailableResponseEventHandler(object? _, MessageFormats.Common.LogMessageResponse _response) {
            if (_response.ResponseHeader.CorrelationId != testMessage.RequestHeader.CorrelationId) return;  // This is no the log response you're looking for

            if (_response.ResponseHeader.TrackingId == testMessage.RequestHeader.TrackingId) {
                // Response from our initial request
                response = _response;
            } else {
                // Response from the Log File write
                write_logResponse = _response;
            }
        }

        MessageHandler<MessageFormats.Common.LogMessageResponse>.MessageReceivedEvent += LogMessageAvailableResponseEventHandler;

        Console.WriteLine($"Sending '{testMessage.GetType().Name}' (TrackingId: '{testMessage.RequestHeader.TrackingId}')");
        await TestSharedContext.SPACEFX_CLIENT.DirectToApp(TestSharedContext.TARGET_SVC_APP_ID, testMessage);

        Console.WriteLine($"Waiting for LogResponse to trigger...");
        while (response == null && DateTime.Now <= maxTimeToWait) {
            Thread.Sleep(100);
        }

        if (response == null) throw new TimeoutException($"Failed to hear {nameof(response)} after {TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG}.  Please check that {TestSharedContext.TARGET_SVC_APP_ID} is deployed");

        Assert.NotNull(response);
        Console.WriteLine($"Waiting for LogWrite to trigger...");

        maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);

        while (write_logResponse == null && DateTime.Now <= maxTimeToWait) {
            Thread.Sleep(100);
        }

        if (write_logResponse == null) throw new TimeoutException($"Failed to hear {nameof(response)} after {TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG}.  Please check that {TestSharedContext.TARGET_SVC_APP_ID} is deployed");

        Assert.NotNull(write_logResponse);
    }
}
