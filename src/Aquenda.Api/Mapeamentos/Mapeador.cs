using Aquenda.Api.Contratos;
using Aquenda.Application.Catalogo;
using Aquenda.Application.Comum;
using Aquenda.Application.Ocupacao;
using Aquenda.Domain;
using Aquenda.Domain.Entidades;
using Aquenda.Domain.ValueObjects;

namespace Aquenda.Api.Mapeamentos;

/// <summary>Converte entidades e resultados em respostas da API.</summary>
public static class Mapeador
{
    private static readonly Atendimento[] TodosAtendimentos =
        Enum.GetValues<Atendimento>().Where(a => a != Atendimento.Nenhum).ToArray();

    public static ServicoResumoResposta ParaResumo(this ResultadoBusca r)
    {
        var s = r.Servico;
        return new ServicoResumoResposta(
            s.Id, s.Nome, s.Categoria, s.Endereco.ParaResposta(), r.DistanciaKm, r.AbertoAgora, s.ComoAcessar(),
            TodosAtendimentos.Where(s.Oferece).Select(a => a.ToString()).ToList(),
            s.Telefone, s.Site, s.Fonte, s.VerificadoEm, s.PrecisaRevisao);
    }

    public static ServicoDetalheResposta ParaDetalhe(this ResultadoBusca r)
    {
        var s = r.Servico;
        return new ServicoDetalheResposta(
            r.ParaResumo(),
            s.Descricao,
            s.Horarios.OrderBy(h => ((int)h.DiaSemana + 6) % 7).ThenBy(h => h.Abre).Select(ParaResposta).ToList(),
            s is CasaAcolhimento c ? new AcolhimentoDetalhe(c.PublicoAlvo, c.CapacidadeDeclarada, c.ExigeEncaminhamento) : null,
            s is CentroReferencia cr ? new CentroReferenciaDetalhe(cr.InscreveTranscidadania) : null,
            s is UnidadeSaude u ? new SaudeDetalhe(u.CodigoCnes, u.TipoAtencao, u.RedeSampaTrans,
                u.HabilitadaProcessoTransexualizador, u.ExigeEncaminhamento) : null,
            s is ServicoJuridico j ? new JuridicoDetalhe(j.Orgao, j.AtendeRetificacao, j.RecebeDenuncia) : null,
            s is LocalRole lr ? new RoleDetalhe(lr.Tipo, lr.FaixaPreco, lr.Acessivel, lr.TagOsm) : null);
    }

    /// <summary>Endereço sigiloso nunca sai com logradouro.</summary>
    public static EnderecoResposta ParaResposta(this Endereco e) => new(
        e.TextoPublico(), e.Bairro, e.Cidade, e.Sigiloso ? null : e.Logradouro, e.Sigiloso, e.Latitude, e.Longitude);

    public static HorarioResposta ParaResposta(HorarioFuncionamento h) => new(
        NomeDia(h.DiaSemana),
        h.DiaInteiro ? null : h.Abre.ToString("HH:mm"),
        h.DiaInteiro ? null : h.Fecha.ToString("HH:mm"),
        h.DiaInteiro);

    public static PaginaResposta<TDestino> Mapear<TOrigem, TDestino>(this Pagina<TOrigem> p, Func<TOrigem, TDestino> f) =>
        new(p.Itens.Select(f).ToList(), p.Total, p.NumeroPagina, p.TamanhoPagina, p.TotalPaginas);

    public static VagasResposta ParaResposta(this VagasAgora v) => new(
        v.CasaId, v.Casa, v.Situacao, Mensagem(v), v.Total, v.Ocupadas, v.Livres,
        v.AtualizadoEmUtc is DateTime d ? new DateTimeOffset(d, TimeSpan.Zero) : null,
        v.Origem, v.ComoAcessar);

    public static HistoricoResposta ParaResposta(this HistoricoOcupacao h) => new(
        h.CasaId, new DateTimeOffset(h.DesdeUtc, TimeSpan.Zero), h.OcupacaoMedia, h.PicoOcupadas, h.VezesLotada,
        h.Leituras.Select(l => new LeituraResposta(l.Total, l.Ocupadas, l.Livres,
            new DateTimeOffset(l.RegistradaEmUtc, TimeSpan.Zero), l.Origem)).ToList());

    public static RodaResposta ParaResposta(this RodaConversa r) => new(
        r.Id, r.Titulo, r.Descricao, r.Organizador,
        new DateTimeOffset(r.InicioUtc, TimeSpan.Zero), new DateTimeOffset(r.FimUtc, TimeSpan.Zero),
        r.Presencial,
        r.Local is null ? null : new LocalRodaResposta(r.Local.Id, r.Local.Nome, r.Local.Endereco.TextoPublico()),
        r.LinkGrupo, r.IdadeMinima, r.Ilustrativa);

    private static string Mensagem(VagasAgora v) => v.Situacao switch
    {
        SituacaoVagas.Disponivel => v.Livres == 1 ? "1 vaga agora" : $"{v.Livres} vagas agora",
        SituacaoVagas.Lotada => "Lotada agora. Vale perguntar pela lista de espera.",
        SituacaoVagas.Desatualizada => "Sem informação recente. Confirme por telefone ou no CRAS/CREAS.",
        _ => "Esta casa ainda não informa vagas pelo app."
    };

    private static string NomeDia(DayOfWeek dia) => dia switch
    {
        DayOfWeek.Monday => "segunda",
        DayOfWeek.Tuesday => "terça",
        DayOfWeek.Wednesday => "quarta",
        DayOfWeek.Thursday => "quinta",
        DayOfWeek.Friday => "sexta",
        DayOfWeek.Saturday => "sábado",
        _ => "domingo"
    };
}
