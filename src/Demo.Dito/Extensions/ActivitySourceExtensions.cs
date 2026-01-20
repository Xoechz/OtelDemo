using System.Diagnostics;

namespace Demo.Dito.Extensions;

/// <summary>
/// Extension methods for configuring OpenTelemetry in the service collection.
/// </summary>
public static class ActivitySourceExtensions
{
    #region public methods

    extension(ActivitySource activitySource)
    {
        /// <summary>
        /// Starts an activity for a specific entity, propagating the job span ID if available.
        /// </summary>
        /// <param name="activitySource">The activity source, which creates the activity.</param>
        /// <param name="name">The activity name.</param>
        /// <param name="entityKey">The entity key to group by with dito.</param>
        /// <returns><see cref="IDisposable"/> <see cref="Activity"/></returns>
        public Activity? StartEntityActivity(string name, string entityKey)
        {
            var activity = activitySource.StartActivity(name);

            if (activity == null)
            {
                return null;
            }

            activity.SetTag(ENTITY_KEY, entityKey);
            activity.DisplayName = $"{name} - {entityKey}";

            var jobSpanId = activity.GetBaggageItem(JOB_SPAN_ID);
            var entityType = activity.GetBaggageItem(ENTITY_TYPE);

            activity.AddTag(JOB_SPAN_ID, jobSpanId);
            activity.AddTag(ENTITY_TYPE, entityType);

            return activity;
        }

        /// <summary>
        /// Starts an activity for a job. Propagates the job span ID as baggage.
        /// </summary>
        /// <param name="activitySource">The activity source, which creates the activity.</param>
        /// <param name="name">The activity name.</param>
        /// <param name="jobId">The job identifier.</param>
        /// <returns><see cref="IDisposable"/> <see cref="Activity"/></returns>
        public Activity? StartJobActivity(string name, string jobId,
            string? source = null, string? destination = null, string? entityType = null)
        {
            var activity = activitySource.StartActivity(name);

            if (activity == null)
            {
                return null;
            }

            activity.DisplayName = $"{name} - {jobId}";
            activity.SetTag(JOB_ID, jobId);
            activity.SetTag(SOURCE, source);
            activity.SetTag(DESTINATION, destination);
            activity.SetTag(ENTITY_TYPE, entityType);

            activity.AddBaggage(JOB_SPAN_ID, activity.SpanId.ToHexString());
            activity.AddBaggage(ENTITY_TYPE, entityType);

            return activity;
        }
    }

    #endregion 

    #region private constants
    private const string DESTINATION = "dito.destination";
    private const string ENTITY_KEY = "dito.key";
    private const string ENTITY_TYPE = "dito.entity_type";
    private const string JOB_ID = "dito.job_id";
    private const string JOB_SPAN_ID = "dito.job_span_id";
    private const string SOURCE = "dito.source";
    #endregion 
}
