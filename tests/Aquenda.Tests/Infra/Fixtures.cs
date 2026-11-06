using Aquenda.Application.Catalogo;
using Aquenda.Application.Chat;
using Aquenda.Application.Curadoria;
using Aquenda.Domain.Chat;
using Aquenda.Domain.ValueObjects;
using Aquenda.Application.Comum;
using Aquenda.Application.Metricas;
using Aquenda.Application.Ocupacao;
using Aquenda.Application.Rodas;
using Aquenda.Infrastructure.Chat;
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

    /// <summary>Mesma cifra para todos os contextos do banco de teste, como o singleton da API.</summary>
    public CifraDeTexto Cifra { get; } = new(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

    /// <summary>Cada chamada devolve um contexto novo, como numa requisição HTTP nova.</summary>
    public AquendaDbContext NovoContexto() =>
        new(new DbContextOptionsBuilder<AquendaDbContext>().UseSqlite(_conexao).Options, Cifra);

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
        await ChatSeed.SemearAsync(db);
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

    public NotificadorFalso Notificador { get; } = new();
    public GeocodificadorFalso Geo { get; } = new();
    public FonteOsmFalsa Osm { get; } = new();

    public ChatService Chat(AquendaDbContext db) =>
        new(new ChatRepository(db), db, Relogio, Notificador);

    public CuradoriaService Curadoria(AquendaDbContext db) =>
        new(new ServicoRepository(db), Geo, db, Relogio);

    public IngestaoOsmService Ingestao(AquendaDbContext db) =>
        new(Osm, new ServicoRepository(db), db, Relogio);

    public async Task<int> SalaAsync(string slug)
    {
        await using var db = Contexto();
        return db.SalasChat.Single(s => s.Slug == slug).Id;
    }

    public async Task<int> IdPorNomeAsync(string nome)
    {
        await using var db = Contexto();
        return db.Servicos.Single(s => s.Nome == nome).Id;
    }

    public void Dispose() => Banco.Dispose();
}

public sealed class NotificadorFalso : INotificadorChat
{
    public List<int> Novas { get; } = new();
    public List<int> Removidas { get; } = new();
    public List<int> Restauradas { get; } = new();

    public Task MensagemNovaAsync(MensagemChat mensagem, CancellationToken ct = default) { Novas.Add(mensagem.Id); return Task.CompletedTask; }
    public Task MensagemRemovidaAsync(int salaId, int mensagemId, CancellationToken ct = default) { Removidas.Add(mensagemId); return Task.CompletedTask; }
    public Task MensagemRestauradaAsync(MensagemChat mensagem, CancellationToken ct = default) { Restauradas.Add(mensagem.Id); return Task.CompletedTask; }
}

public sealed class GeocodificadorFalso : IGeocodificador
{
    public List<string> Consultas { get; } = new();
    public Coordenada? Resposta { get; set; } = new Coordenada(-23.5475, -46.63611);

    public Task<Coordenada?> CoordenadaDoCepAsync(string cep, CancellationToken ct = default)
    {
        Consultas.Add(cep);
        return Task.FromResult(Resposta);
    }
}

public sealed class FonteOsmFalsa : IFonteOsm
{
    public List<LugarOsm> Lugares { get; } = new();
    public Task<IReadOnlyList<LugarOsm>> BuscarLugaresLgbtqAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<LugarOsm>>(Lugares.ToList());
}
