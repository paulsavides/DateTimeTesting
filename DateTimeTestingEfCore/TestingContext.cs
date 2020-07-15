using Microsoft.EntityFrameworkCore;

namespace DateTimeTestingEfCore
{
  public class TestingContext : DbContext
  {
    public TestingContext() : base() {}
    public TestingContext(DbContextOptions<TestingContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      optionsBuilder.UseSqlServer(Program.ConnectionString);
    }

    public DbSet<TestEntity> TestEntities { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
      var entity = builder.Entity<TestEntity>();

      entity.HasKey(e => e.Id);
      entity.Property(e => e.CreateDateUTC).ValueGeneratedOnAdd().HasDefaultValueSql("SYSUTCDATETIME()");
    }
  }
}
