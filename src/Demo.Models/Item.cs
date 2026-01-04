namespace Demo.Models;

public class Item(string articleName, int stock)
{
    #region public properties
    public string ArticleName { get; set; } = articleName;
    public int Stock { get; set; } = stock;
    #endregion 

    #region public methods

    public override string ToString()
    {
        return $"{ArticleName} ({Stock})";
    }

    #endregion 
}
