using Bogus;

namespace Demo.Models.Faker;

public class ItemFaker : Faker<Item>
{
    #region Public Constructors

    public ItemFaker(int seed)
    {
        ConfigureOrderFaker(seed);
    }

    #endregion Public Constructors

    #region Private Methods

    private void ConfigureOrderFaker(int seed)
    {
        UseSeed(seed)
            .CustomInstantiator(f => new($"{f.Commerce.ProductMaterial()} {f.Commerce.Product()}", f.Random.Int(1, 20)));
    }

    #endregion Private Methods
}