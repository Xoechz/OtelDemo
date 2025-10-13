using System.Diagnostics;

namespace Demo.Dito.Extensions;

/// <summary>
/// Extension methods for configuring OpenTelemetry in the service collection.
/// </summary>
public static class ActivitySourceExtensions
{
    #region Private Fields

    private const string DESTINATION = "dito.destination";
    private const string ENTITY_KEY = "dito.key";
    private const string ENTITY_TYPE = "dito.entity_type";
    private const string JOB_ID = "dito.job_id";
    private const string JOB_SPAN_ID = "dito.job_span_id";
    private const string SOURCE = "dito.source";

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Starts an activity for a specific entity, propagating the job span ID if available.
    /// </summary>
    /// <param name="activitySource">The activity source, which creates the activity.</param>
    /// <param name="name">The activity name.</param>
    /// <param name="entityKey">The entity key to group by with dito.</param>
    /// <returns><see cref="IDisposable"/> <see cref="Activity"/></returns>
    public static Activity? StartEntityActivity(this ActivitySource activitySource, string name, string entityKey)
    {
        var activity = activitySource.StartActivity(name);

        if (activity != null)
        {
            activity.SetTag(ENTITY_KEY, entityKey);
            activity.DisplayName = $"{name} - {entityKey}";
            var baggageValue = activity.GetBaggageItem(JOB_SPAN_ID);

            if (baggageValue != null)
            {
                activity.AddTag(JOB_SPAN_ID, baggageValue);
            }
        }

        return activity;
    }

    /// <summary>
    /// Starts an activity for a job. Propagates the job span ID as baggage.
    /// </summary>
    /// <param name="activitySource">The activity source, which creates the activity.</param>
    /// <param name="name">The activity name.</param>
    /// <param name="jobId">The job identifier.</param>
    /// <returns><see cref="IDisposable"/> <see cref="Activity"/></returns>
    public static Activity? StartJobActivity(this ActivitySource activitySource, string name, string jobId,
        string? source = null, string? destination = null, string? entityType = null)
    {
        var activity = activitySource.StartActivity(name);

        if (activity != null)
        {
            activity.DisplayName = $"{name} - {jobId}";
            activity.SetTag(JOB_ID, jobId);
            activity.AddBaggage(JOB_SPAN_ID, activity.SpanId.ToHexString());

            if (source != null)
            {
                activity.SetTag(SOURCE, source);
            }

            if (destination != null)
            {
                activity.SetTag(DESTINATION, destination);
            }

            if (entityType != null)
            {
                activity.SetTag(ENTITY_TYPE, entityType);
            }
        }

        return activity;
    }

    #endregion Public Methods
}