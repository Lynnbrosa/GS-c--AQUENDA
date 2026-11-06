using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Aquenda.Infrastructure.Dados;

/// <summary>Usada só pelo "dotnet ef" para gerar migrações.</summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AquendaDbContext>
{
    public AquendaDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AquendaDbContext>()
            .UseSqlite("Data Source=aquenda.db")
            .Options;
        return new AquendaDbContext(options);
    }
}
