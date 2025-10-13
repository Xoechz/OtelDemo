using Bogus;

namespace Demo.Models.Faker;

public class OrderFaker : Faker<Order>
{
    #region Public Constructors

    public OrderFaker(int seed)
    {
        ConfigureOrderFaker(seed);
    }

    #endregion Public Constructors

    #region Private Methods

    private void ConfigureOrderFaker(int seed)
    {
        UseSeed(seed)
            .RuleFor(o => o.ArticleName, f => f.Commerce.ProductName())
            .RuleFor(o => o.Stock, f => f.Random.Int(1, 20));
    }

    #endregion Private Methods
}