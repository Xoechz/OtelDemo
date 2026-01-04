using Bogus;

namespace Demo.Models.Faker;

public class FailureFaker : Faker<string?>
{
    #region public constructors

    public FailureFaker(int seed)
    {
        ConfigureFailureFaker(seed);
    }

    #endregion 

    #region private fields

    private readonly List<string> _errorMessages =
    [
        "Payment failed",
        "Address not serviceable",
        "Item lost in transit",
        "Item reserved for another customer",
        "Item not deliverable, due to personal grudge against customer",
    ];

    #endregion 

    #region private methods

    private void ConfigureFailureFaker(int seed)
    {
        UseSeed(seed)
            .CustomInstantiator(f =>
            {
                var shouldFail = f.Random.Bool(0.05f);
                return shouldFail ? f.PickRandom(_errorMessages) : null;
            });
    }

    #endregion 
}
