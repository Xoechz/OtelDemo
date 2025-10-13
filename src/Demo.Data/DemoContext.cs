using Demo.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Demo.Data;

public class DemoContext(DbContextOptions<DemoContext> options) : DbContext(options)
{
    #region Public Properties

    public DbSet<Item> Items => Set<Item>();

    #endregion Public Properties
}