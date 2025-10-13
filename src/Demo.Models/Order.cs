namespace Demo.Models;

public class Order(string articleName, int stock)
{
    #region Public Properties

    public string ArticleName { get; set; } = articleName;

    public int Stock { get; set; } = stock;

    #endregion Public Properties

    #region Public Methods

    public override string ToString()
    {
        return $"{ArticleName} ({Stock})";
    }

    #endregion Public Methods
}