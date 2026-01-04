using System.ComponentModel.DataAnnotations;

namespace Demo.Data.Entities;

public class Item
{
    #region public properties

    [Key, MaxLength(50)]
    public required string ArticleName { get; set; }

    [Required]
    public required int Stock { get; set; }

    #endregion 
}
