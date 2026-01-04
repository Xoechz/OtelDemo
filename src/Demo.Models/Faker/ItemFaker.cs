using Bogus;

namespace Demo.Models.Faker;

public class ItemFaker : Faker<Item>
{
    #region public constructors

    public ItemFaker(int seed)
    {
        ConfigureOrderFaker(seed);
    }

    #endregion 

    #region private methods

    private void ConfigureOrderFaker(int seed)
    {
        UseSeed(seed)
            .CustomInstantiator(f => new(f.Commerce.Product(), f.Random.Int(1, 20)));
    }

    #endregion 
}
