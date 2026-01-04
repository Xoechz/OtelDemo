using Demo.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Demo.Data;

public class DemoContext(DbContextOptions<DemoContext> options) : DbContext(options)
{
    #region public properties
    public DbSet<Item> Items => Set<Item>();
    #endregion 
}
