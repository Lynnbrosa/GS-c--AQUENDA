using Aquenda.Domain.Entidades;
using Aquenda.Domain.Repositorios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Aquenda.Infrastructure.Dados;

public class AquendaDbContext : DbContext, IUnitOfWork
{
    public AquendaDbContext(DbContextOptions<AquendaDbContext> options) : base(options) { }

    public DbSet<Servico> Servicos => Set<Servico>();
    public DbSet<CasaAcolhimento> Casas => Set<CasaAcolhimento>();
    public DbSet<LeituraOcupacao> Leituras => Set<LeituraOcupacao>();
    public DbSet<RodaConversa> Rodas => Set<RodaConversa>();
    public DbSet<InteracaoDiaria> Interacoes => Set<InteracaoDiaria>();

    public Task<int> SalvarAsync(CancellationToken ct = default) => SaveChangesAsync(ct);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AquendaDbContext).Assembly);
        MarcarDatasComoUtc(modelBuilder);
    }

    /// <summary>
    /// O SQLite guarda DateTime como texto e perde o Kind. Tudo aqui é UTC,
    /// então o conversor devolve as datas já marcadas como UTC.
    /// </summary>
    private static void MarcarDatasComoUtc(ModelBuilder modelBuilder)
    {
        var conversor = new ValueConverter<DateTime, DateTime>(
            v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        var conversorNulavel = new ValueConverter<DateTime?, DateTime?>(
            v => v.HasValue ? (v.Value.Kind == DateTimeKind.Utc ? v : v.Value.ToUniversalTime()) : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        foreach (var propriedade in modelBuilder.Model.GetEntityTypes().SelectMany(t => t.GetProperties()))
        {
            if (propriedade.ClrType == typeof(DateTime))
                propriedade.SetValueConverter(conversor);
            else if (propriedade.ClrType == typeof(DateTime?))
                propriedade.SetValueConverter(conversorNulavel);
        }
    }
}
