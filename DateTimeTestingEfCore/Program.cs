using System;
using System.Linq;
using System.Threading.Tasks;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DateTimeTestingEfCore
{
  public class Program
  {
    public const string ConnectionString = "Server=(localdb)\\mssqllocaldb;Database=EFCoreDTTesting;Trusted_Connection=True;ConnectRetryCount=0";

    public static async Task Main()
    {
      var serviceCollection = new ServiceCollection();
      serviceCollection.AddDbContext<TestingContext>(options => options.UseSqlServer(ConnectionString));

      var provider = serviceCollection.BuildServiceProvider();

      using (var migrationScope = provider.CreateScope())
      {
        var context = migrationScope.ServiceProvider.GetRequiredService<TestingContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
      }

      const int numRums = 10;
      const int printLimiter = 100;
      int weirdCount = 0;

      for (int i = 0; i < numRums; ++i)
      {
        if ((i + 1) % printLimiter == 0)
        {
          Console.WriteLine($"Run #{i + 1}");
        }

        TestEntity entity = null;

        using (var recordCreateScope = provider.CreateScope())
        {
          var context = recordCreateScope.ServiceProvider.GetRequiredService<TestingContext>();
          var created = context.TestEntities.Add(new TestEntity());

          await context.SaveChangesAsync();
          entity = created.Entity;
        }

        using (var recordSelectScope = provider.CreateScope())
        {
          var context = recordSelectScope.ServiceProvider.GetRequiredService<TestingContext>();
          var deleteCount = await context.TestEntities.Where(t => t.Id == entity.Id).Where(t => t.CreateDateUTC < entity.CreateDateUTC).BatchDeleteAsync();

          if (deleteCount > 0)
          {
            ++weirdCount;
            if (weirdCount % printLimiter == 0)
            {
              Console.WriteLine($"Expected deleted to be 0 but was actually {deleteCount}");
            }
          }
        }
      }

      Console.WriteLine($"Runs = {numRums}, Weird = {weirdCount}, WeirdPct = {(double)weirdCount / (double)numRums}");
    }
  }
}
