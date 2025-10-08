using Demo.Data.Models;
using Demo.Data.Repositories;
using Demo.Data.Utilities;
using Demo.JobService.Config;
using Demo.ServiceDefaults.Faker;
using System.Diagnostics;

namespace Demo.JobService.Jobs;

public class JobWorker(ActivitySource activitySource,
                       UserRepository userRepository,
                       JobConfig config,
                       EmailFaker emailFaker,
                       JobFaker jobFaker,
                       IHttpClientFactory httpClientFactory)
{
    #region Private Fields

    private readonly ActivitySource _activitySource = activitySource;
    private readonly JobConfig _config = config;
    private readonly UserRepository _userRepository = userRepository;
    private readonly EmailFaker _emailFaker = emailFaker;
    private readonly JobFaker _jobFaker = jobFaker;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    #endregion Private Fields

    #region Public Properties

    public string CronExpression => _config.CronExpression ?? "0 0 31 2 *";

    #endregion Public Properties

    #region Public Methods

    public async Task DoWork(CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity(_jobFaker.Generate(1)[0]);
        activity?.SetTag("dito.job_id", _config.ServiceName);
        activity?.SetTag("dito.source", $"Worker {_config.ServiceIndex}");
        activity?.SetTag("dito.entity_type", "User");
        var httpClient = _httpClientFactory.CreateClient("jobs");

        var randomIndex = new Random().Next(0, _config.TargetUrls.Count());

        if (randomIndex == _config.ServiceIndex)
        {
            randomIndex = (randomIndex + 1) % _config.TargetUrls.Count();
        }

        var targetUrl = _config.TargetUrls.ElementAt(randomIndex)
            ?? throw new InvalidOperationException("No target URL provided");

        activity?.SetTag("dito.destination", $"Worker {randomIndex}");
        activity?.SetTag("TargetUrl", targetUrl);

        httpClient.BaseAddress = new Uri(targetUrl);

        var response = await httpClient.GetAsync("User", cancellationToken);
        var users = await response.Content.ReadFromJsonAsync<IEnumerable<User>>(cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve users from external API");

        activity?.SetTag("UserCount", users.Count());
        await _userRepository.AddUsersAsync(users);

        foreach (var user in users)
        {
            using var userActivity = _activitySource.StartActivity($"Processing User {user.EmailAddress}");
            userActivity?.SetTag("dito.key", user.EmailAddress);
            var error = Utils.GetRandomErrorType(_config.ErrorChances);

            if (error != ErrorType.None)
            {
                userActivity?.SetStatus(ActivityStatusCode.Error, $"Simulated {error} error for user {user.EmailAddress}");
                userActivity?.SetTag("ErrorType", error.ToString());
            }
        }

        var firstEmail = users.First().EmailAddress;
        activity?.SetTag("DeletedUser", firstEmail);

        await httpClient.DeleteAsync($"User/{firstEmail}", cancellationToken);

        var randomEmail = _emailFaker.Generate(1)[0];
        activity?.SetTag("AddedUser", randomEmail);

        var usersToAdd = new List<User>
        {
            new() { EmailAddress = randomEmail }
        };

        await httpClient.PostAsJsonAsync("User", usersToAdd, cancellationToken);
    }

    #endregion Public Methods
}