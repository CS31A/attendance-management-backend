using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace attendance_monitoring.Services;

/// <summary>
/// Provides the in-process reliability telemetry instruments for tracked request groups.
/// </summary>
public sealed class RequestReliabilityTelemetry : IDisposable
{
    private readonly Meter _meter;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestReliabilityTelemetry"/> class.
    /// </summary>
    public RequestReliabilityTelemetry()
    {
        _meter = new Meter(MeterName);
        RequestDuration = _meter.CreateHistogram<double>(RequestDurationInstrumentName, unit: "ms");
        RequestErrors = _meter.CreateCounter<long>(RequestErrorsInstrumentName);
    }

    /// <summary>
    /// Gets the reliability meter name.
    /// </summary>
    public const string MeterName = "attendance_monitoring.reliability";

    /// <summary>
    /// Gets the request-duration histogram name.
    /// </summary>
    public const string RequestDurationInstrumentName = "request.duration.ms";

    /// <summary>
    /// Gets the request-error counter name.
    /// </summary>
    public const string RequestErrorsInstrumentName = "request.errors";

    /// <summary>
    /// Gets the request duration histogram.
    /// </summary>
    public Histogram<double> RequestDuration { get; }

    /// <summary>
    /// Gets the request error counter.
    /// </summary>
    public Counter<long> RequestErrors { get; }

    /// <summary>
    /// Records the request duration with the supplied dimensions.
    /// </summary>
    /// <param name="elapsedMilliseconds">The request duration in milliseconds.</param>
    /// <param name="endpointGroup">The endpoint family name.</param>
    /// <param name="method">The HTTP method.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    public void RecordRequest(double elapsedMilliseconds, string endpointGroup, string method, int statusCode)
    {
        var tags = CreateTags(endpointGroup, method, statusCode);
        RequestDuration.Record(elapsedMilliseconds, tags);
    }

    /// <summary>
    /// Records a request error with the supplied dimensions.
    /// </summary>
    /// <param name="endpointGroup">The endpoint family name.</param>
    /// <param name="method">The HTTP method.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    public void RecordError(string endpointGroup, string method, int statusCode)
    {
        var tags = CreateTags(endpointGroup, method, statusCode);
        RequestErrors.Add(1, tags);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _meter.Dispose();
    }

    private static TagList CreateTags(string endpointGroup, string method, int statusCode)
    {
        return new TagList
        {
            { "endpoint_group", endpointGroup },
            { "method", method },
            { "status_code", statusCode.ToString() }
        };
    }
}
