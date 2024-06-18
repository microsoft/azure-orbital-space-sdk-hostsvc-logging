namespace Microsoft.Azure.SpaceFx.HostServices.Sensor.IntegrationTests.Tests;

[Collection(nameof(TestSharedContext))]
public class TelemetryResponseTests : IClassFixture<TestSharedContext> {
    readonly TestSharedContext _context;

    public TelemetryResponseTests(TestSharedContext context) {
        _context = context;
    }

    [Fact]
    public async Task TelemetryTests() {
        DateTime maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);
        MessageFormats.Common.TelemetryMetricResponse? response = null;
        MessageFormats.Common.TelemetryMetricResponse? write_logResponse = null;

        MessageFormats.Common.TelemetryMetric testMessage = new() {
            RequestHeader = new MessageFormats.Common.RequestHeader() {
                TrackingId = Guid.NewGuid().ToString(),
                CorrelationId = Guid.NewGuid().ToString()
            },
            MetricName = "MetricName",
            MetricValue = 1
        };

        // Register a callback event to catch the response
        void TelemetryMetricAvailableResponseEventHandler(object? _, MessageFormats.Common.TelemetryMetricResponse _response) {
            if (_response.ResponseHeader.CorrelationId != testMessage.RequestHeader.CorrelationId) return;  // This is no the log response you're looking for

            if (_response.ResponseHeader.TrackingId == testMessage.RequestHeader.TrackingId) {
                // Response from our initial request
                response = _response;
            }
        }

        MessageHandler<MessageFormats.Common.TelemetryMetricResponse>.MessageReceivedEvent += TelemetryMetricAvailableResponseEventHandler;

        Console.WriteLine($"Sending '{testMessage.GetType().Name}' (TrackingId: '{testMessage.RequestHeader.TrackingId}')");
        await TestSharedContext.SPACEFX_CLIENT.DirectToApp(TestSharedContext.TARGET_SVC_APP_ID, testMessage);

        Console.WriteLine($"Waiting for TelemetryResponse to trigger...");
        while (response == null && DateTime.Now <= maxTimeToWait) {
            Thread.Sleep(100);
        }

        if (response == null) throw new TimeoutException($"Failed to hear {nameof(response)} after {TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG}.  Please check that {TestSharedContext.TARGET_SVC_APP_ID} is deployed");

        Assert.NotNull(response);
    }
}
