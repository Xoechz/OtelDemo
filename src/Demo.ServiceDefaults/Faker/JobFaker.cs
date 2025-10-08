using Bogus;

namespace Demo.ServiceDefaults.Faker;

public class JobFaker : Faker<string>
{
    #region Public Constructors

    public JobFaker(int seed)
    {
        ConfigureJobFaker(seed);
    }

    #endregion Public Constructors

    #region Private Methods

    private void ConfigureJobFaker(int seed)
    {
        UseSeed(seed)
            .CustomInstantiator(f => $"{f.Commerce.Department()}: {f.Hacker.IngVerb()} {f.Hacker.Adjective()} {f.Hacker.Noun()}");
    }

    #endregion Private Methods
}