using System;
using System.Linq;
using System.Threading.Tasks;
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

      const int numRums = 100000;
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

        //Console.WriteLine($"Using entity: Id={entity.Id}, CreateDateUTC={entity.CreateDateUTC:O}");

        using (var recordSelectScope = provider.CreateScope())
        {
          var context = recordSelectScope.ServiceProvider.GetRequiredService<TestingContext>();
          var records = await context.TestEntities.Where(t => t.Id == entity.Id).Where(t => t.CreateDateUTC < entity.CreateDateUTC).ToListAsync();

          if (records.Any())
          {
            ++weirdCount;
            var found = records.FirstOrDefault();

            if (weirdCount % printLimiter == 0)
            {
              Console.WriteLine($"Weird #{weirdCount} - found id: {found.Id}, createDate: {found.CreateDateUTC:O}, inputId: {entity.Id}, inputDate: {entity.CreateDateUTC:O}");
            }
          }
        }
      }

      Console.WriteLine($"Runs = {numRums}, Weird = {weirdCount}, WeirdPct = {(double)weirdCount / (double)numRums}");
    }
  }
}
