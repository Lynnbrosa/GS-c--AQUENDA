using Aquenda.Application.Comum;
using Aquenda.Domain;
using Aquenda.Domain.Entidades;
using Aquenda.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Aquenda.Infrastructure.Dados.Seed;

/// <summary>
/// Catálogo inicial, montado a partir da pesquisa do grupo (pitch, nov/2026).
/// Cada item tem a fonte pública de onde veio. Coordenadas são aproximadas
/// (rua ou centro do bairro) e devem ser conferidas com a BrasilAPI/CEP.
/// Onde a pesquisa não achou endereço ou horário, o campo fica vazio de propósito.
/// </summary>
public static class CatalogoSeed
{
    public static readonly DateOnly DataPesquisa = new(2026, 11, 5);

    private const string FonteCentros = "https://www.prefeitura.sp.gov.br/cidade/secretarias/direitos_humanos/lgbti/rede_de_atendimento/index.php?p=271098";
    private const string FonteSampaTrans = "https://prefeitura.sp.gov.br/documents/d/saude/avancos-e-desafios-_-jan26-pdf";
    private const string FonteSmads = "https://prefeitura.sp.gov.br/web/assistencia_social/w/m%C3%AAs-do-orgulho-lgbtqia-existir-resistir-e-recome%C3%A7ar";
    private const string FonteNudiversis = "https://www.oabsp.org.br/upload/548328681.pdf";
    private const string FonteCasa1 = "https://www.brasildefato.com.br/?p=568969";
    private const string FonteGuiaLgbt = "https://saopaulosecreto.com/guia-balada-bares-lgbt-gay-sp/";
    private const string FonteGuiaLgbtEn = "https://saopaulosecreto.com/en/guide-to-lgbt-gay-bars-in-sp/";

    public static async Task<int> SemearAsync(AquendaDbContext db, IRelogio relogio, CancellationToken ct = default)
    {
        if (await db.Servicos.AnyAsync(ct)) return 0;

        var servicos = Servicos().ToList();
        db.Servicos.AddRange(servicos);

        // Leitura de exemplo (origem "Simulado") até o ESP32 do Wokwi começar a publicar.
        var florescer = servicos.OfType<CasaAcolhimento>().First(c => c.Nome == "Casa Florescer");
        var agora = relogio.AgoraUtc;
        florescer.RegistrarLeitura(10, 7, agora.AddMinutes(-30), OrigemLeitura.Simulado, agora);

        await db.SaveChangesAsync(ct);

        var centro = servicos.OfType<CentroReferencia>().First();
        db.Rodas.Add(new RodaConversa(
            "Roda de conversa presencial",
            "Exemplo para o protótipo. Encontro aberto no Centro de Referência.",
            "Centro de Referência LGBTI+",
            ProximoSabado15h(agora), 120, presencial: true, localId: centro.Id, linkGrupo: null,
            idadeMinima: 0, ilustrativa: true, agoraUtc: agora));
        await db.SaveChangesAsync(ct);

        return servicos.Count;
    }

    private static IEnumerable<Servico> Servicos()
    {
        var brunna = new CentroReferencia(
            "Centro de Referência LGBTI+ Brunna Valin",
            "Atendimento psicossocial e jurídico para a população LGBTI+.",
            new Endereco("R. Maj. Sertório, 292", "República", coordenada: new Coordenada(-23.5447, -46.6459)),
            FonteCentros, DataPesquisa,
            Atendimento.Psicossocial | Atendimento.Juridico | Atendimento.Trabalho,
            inscreveTranscidadania: true);
        brunna.DefinirHorarios(HorarioFuncionamento.SegundaASexta(9, 17));
        yield return brunna;

        var florescer = new CasaAcolhimento(
            "Casa Florescer",
            "Centro de acolhida da SMADS para pessoas trans.",
            new Endereco(null, "Centro", coordenada: new Coordenada(-23.5505, -46.6333), sigiloso: true),
            FonteSmads, DataPesquisa,
            publicoAlvo: "Mulheres trans e travestis",
            exigeEncaminhamento: true);
        florescer.DefinirHorarios(HorarioFuncionamento.TodosOsDias24h());
        yield return florescer;

        var casa1 = new CasaAcolhimento(
            "Casa 1",
            "ONG com república de acolhida para jovens LGBTQIA+ expulsos de casa e centro cultural.",
            new Endereco(null, "Bela Vista", coordenada: new Coordenada(-23.5580, -46.6450), sigiloso: true),
            FonteCasa1, DataPesquisa,
            publicoAlvo: "Jovens LGBTQIA+ de 18 a 25 anos",
            exigeEncaminhamento: false,
            atendimentos: Atendimento.Convivencia);
        casa1.DefinirInstrucaoAcesso("Fale com a Casa 1 pelo site ou redes sociais. A república tem fila e passa por conversa com a equipe.");
        casa1.DefinirContato(null, "https://www.casaum.org");
        yield return casa1;

        yield return new UnidadeSaude(
            "UBS República",
            "UBS da Rede SAMPA Trans (CRS Centro). Porta de entrada para a hormonização pelo SUS.",
            new Endereco(null, "República", coordenada: new Coordenada(-23.5440, -46.6420)),
            FonteSampaTrans, DataPesquisa,
            Atendimento.Hormonizacao,
            TipoAtencao.Primaria, redeSampaTrans: true);

        var crPopTT = new UnidadeSaude(
            "CR POP TT Janaína Lima",
            "Atenção secundária para pessoas trans e travestis.",
            new Endereco(null, "a confirmar"),
            FonteSampaTrans, DataPesquisa,
            Atendimento.Hormonizacao | Atendimento.SaudeMental,
            TipoAtencao.Secundaria, redeSampaTrans: true, exigeEncaminhamento: true);
        crPopTT.DefinirHorarios(HorarioFuncionamento.SegundaASexta(8, 20));
        yield return crPopTT;

        yield return new ServicoJuridico(
            "Defensoria Pública de SP – NUDIVERSIS",
            "Núcleo Especializado de Defesa da Diversidade e da Igualdade Racial.",
            new Endereco(null, "Centro"),
            FonteNudiversis, DataPesquisa,
            orgao: "Defensoria Pública do Estado de São Paulo",
            atendeRetificacao: true, recebeDenuncia: true);

        yield return new LocalRole(
            "Largo do Arouche",
            "Região histórica LGBT+ do centro, com bares e baladas em volta.",
            new Endereco("Largo do Arouche", "República", coordenada: new Coordenada(-23.5427, -46.6449)),
            FonteGuiaLgbt, DataPesquisa, TipoRole.EspacoPublico, faixaPreco: 0, acessivel: true);

        yield return new LocalRole(
            "Rua Frei Caneca",
            "Rua com bares e baladas LGBT+ na Consolação.",
            new Endereco("R. Frei Caneca", "Consolação", coordenada: new Coordenada(-23.5535, -46.6545)),
            FonteGuiaLgbt, DataPesquisa, TipoRole.EspacoPublico, faixaPreco: 0, acessivel: false);

        yield return new LocalRole(
            "Casa Fluida",
            "Bar com shows de drag.",
            new Endereco("R. Bela Cintra, 569", "Consolação", coordenada: new Coordenada(-23.5525, -46.6553)),
            FonteGuiaLgbtEn, DataPesquisa, TipoRole.Bar, faixaPreco: 2, acessivel: false);

        yield return new LocalRole(
            "Edifício Copan",
            "Dezenas de bares no térreo do prédio.",
            new Endereco("Av. Ipiranga, 200", "República", coordenada: new Coordenada(-23.5464, -46.6446)),
            FonteGuiaLgbt, DataPesquisa, TipoRole.Bar, faixaPreco: 1, acessivel: true);
    }

    private static DateTime ProximoSabado15h(DateTime agoraUtc)
    {
        // 15h em São Paulo = 18h UTC.
        var dias = ((int)DayOfWeek.Saturday - (int)agoraUtc.DayOfWeek + 7) % 7;
        var data = agoraUtc.Date.AddDays(dias == 0 ? 7 : dias).AddHours(18);
        return DateTime.SpecifyKind(data, DateTimeKind.Utc);
    }
}
