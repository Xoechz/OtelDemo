using System.Diagnostics.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Demo.OpenTelemetry;

/// <summary>
/// Collection of Metric Instruments used in the application.
/// </summary>
public sealed class MetricInstrumentCollection
{
    public MetricInstrumentCollection(IMeterFactory meterFactory, IConfiguration configuration, IHostEnvironment environment)
    {
        var serviceName = configuration["SERVICE_NAME"] ?? environment.ApplicationName;

        var meter = meterFactory.Create(serviceName);
        ItemsProcessedCounter = meter.CreateCounter<int>("items_processed_total", "items", "Total number of items processed");
        ItemsProcessedHistogram = meter.CreateHistogram<int>("items_processed_histogram", "items", "Histogram of items processed per request");
    }

    /// <summary>
    /// Counter for total number of items processed.
    /// </summary>
    public Counter<int> ItemsProcessedCounter { get; }

    /// <summary>
    /// Histogram for items processed per request.
    /// </summary>
    public Histogram<int> ItemsProcessedHistogram { get; }
}