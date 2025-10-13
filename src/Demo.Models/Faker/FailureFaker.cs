using Bogus;

namespace Demo.Models.Faker;

public class FailureFaker : Faker<string?>
{
    #region Private Fields

    private readonly List<string> _errorMessages = new()
    {
        "Payment failed",
        "Address not serviceable",
        "Item lost in transit",
        "Item reserved for another customer",
        "Item not deliverable, due to personal grudge against customer",
    };

    #endregion Private Fields

    #region Public Constructors

    public FailureFaker(int seed)
    {
        ConfigureFailureFaker(seed);
    }

    #endregion Public Constructors

    #region Private Methods

    private void ConfigureFailureFaker(int seed)
    {
        UseSeed(seed)
            .CustomInstantiator(f =>
            {
                var shouldFail = f.Random.Bool(0.05f);
                return shouldFail ? f.PickRandom(_errorMessages) : null;
            });
    }

    #endregion Private Methods
}