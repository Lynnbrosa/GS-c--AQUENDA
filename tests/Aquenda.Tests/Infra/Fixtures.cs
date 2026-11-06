using Aquenda.Application.Catalogo;
using Aquenda.Application.Comum;
using Aquenda.Application.Metricas;
using Aquenda.Application.Ocupacao;
using Aquenda.Application.Rodas;
using Aquenda.Infrastructure.Dados.Seed;
using Aquenda.Infrastructure.Repositorios;
using Aquenda.Infrastructure.Dados;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Aquenda.Tests.Infra;

/// <summary>Relógio parado, para testes que dependem de "agora".</summary>
public sealed class RelogioFixo : IRelogio
{
    public RelogioFixo(DateTime agoraUtc) => AgoraUtc = DateTime.SpecifyKind(agoraUtc, DateTimeKind.Utc);
    public DateTime AgoraUtc { get; set; }
    public void Avancar(TimeSpan tempo) => AgoraUtc += tempo;
}

/// <summary>
/// SQLite em memória com as migrações reais aplicadas. Testa o mesmo SQL que roda em produção,
/// coisa que o provedor InMemory do EF não faz.
/// </summary>
public sealed class BancoEmMemoria : IDisposable
{
    private readonly SqliteConnection _conexao;

    public BancoEmMemoria()
    {
        _conexao = new SqliteConnection("Data Source=:memory:");
        _conexao.Open();
        using var db = NovoContexto();
        db.Database.Migrate();
    }

    /// <summary>Cada chamada devolve um contexto novo, como numa requisição HTTP nova.</summary>
    public AquendaDbContext NovoContexto() =>
        new(new DbContextOptionsBuilder<AquendaDbContext>().UseSqlite(_conexao).Options);

    public SqliteConnection Conexao => _conexao;

    public void Dispose() => _conexao.Dispose();
}

/// <summary>Monta os serviços da aplicação sobre um contexto novo (uma "requisição").</summary>
public sealed class Cenario : IDisposable
{
    public static readonly DateTime Agora = new(2026, 11, 16, 15, 0, 0, DateTimeKind.Utc); // segunda, 12h em SP

    public BancoEmMemoria Banco { get; } = new();
    public RelogioFixo Relogio { get; } = new(Agora);

    public static async Task<Cenario> ComSeedAsync()
    {
        var c = new Cenario();
        await using var db = c.Banco.NovoContexto();
        await CatalogoSeed.SemearAsync(db, c.Relogio);
        return c;
    }

    public AquendaDbContext Contexto() => Banco.NovoContexto();

    public MetricaService Metricas(AquendaDbContext db) =>
        new(new InteracaoRepository(db), new ServicoRepository(db), db, Relogio);

    public CatalogoService Catalogo(AquendaDbContext db) =>
        new(new ServicoRepository(db), Metricas(db), db, Relogio);

    public OcupacaoService Ocupacao(AquendaDbContext db) =>
        new(new CasaAcolhimentoRepository(db), db, Relogio);

    public RodaService Rodas(AquendaDbContext db) =>
        new(new RodaConversaRepository(db), new ServicoRepository(db), db, Relogio);

    public async Task<int> IdPorNomeAsync(string nome)
    {
        await using var db = Contexto();
        return db.Servicos.Single(s => s.Nome == nome).Id;
    }

    public void Dispose() => Banco.Dispose();
}
