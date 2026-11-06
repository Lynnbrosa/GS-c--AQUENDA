using Aquenda.Domain.Chat;
using Aquenda.Domain.Entidades;
using Aquenda.Domain.Repositorios;
using Aquenda.Infrastructure.Chat;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Aquenda.Infrastructure.Dados;

public class AquendaDbContext : DbContext, IUnitOfWork
{
    public AquendaDbContext(DbContextOptions<AquendaDbContext> options, CifraDeTexto? cifra = null) : base(options) => Cifra = cifra;

    /// <summary>Cifra do texto das mensagens das salas. Sem ela (ferramentas de migração), o texto passa como está.</summary>
    public CifraDeTexto? Cifra { get; }

    public DbSet<Servico> Servicos => Set<Servico>();
    public DbSet<CasaAcolhimento> Casas => Set<CasaAcolhimento>();
    public DbSet<LeituraOcupacao> Leituras => Set<LeituraOcupacao>();
    public DbSet<RodaConversa> Rodas => Set<RodaConversa>();
    public DbSet<InteracaoDiaria> Interacoes => Set<InteracaoDiaria>();
    public DbSet<SalaChat> SalasChat => Set<SalaChat>();
    public DbSet<ParticipanteChat> ParticipantesChat => Set<ParticipanteChat>();
    public DbSet<MensagemChat> MensagensChat => Set<MensagemChat>();
    public DbSet<DenunciaMensagem> DenunciasMensagem => Set<DenunciaMensagem>();

    public Task<int> SalvarAsync(CancellationToken ct = default) => SaveChangesAsync(ct);

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, ChaveDoModelo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AquendaDbContext).Assembly);
        MarcarDatasComoUtc(modelBuilder);
        if (Cifra is { } cifra)
            modelBuilder.Entity<MensagemChat>().Property(m => m.Texto)
                .HasConversion(new ValueConverter<string, string>(v => cifra.Cifrar(v), v => cifra.Decifrar(v)));
    }

    /// <summary>O EF guarda o modelo em cache por tipo; com a cifra no conversor, o cache também separa por cifra.</summary>
    private sealed class ChaveDoModelo : IModelCacheKeyFactory
    {
        public object Create(DbContext context, bool designTime) =>
            (context.GetType(), (context as AquendaDbContext)?.Cifra, designTime);
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
