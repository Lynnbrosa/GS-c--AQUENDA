using System.Text.RegularExpressions;

namespace Aquenda.Domain.Privacidade;

/// <summary>
/// Padrões de dado pessoal usados em dois lugares: nas regras das salas (recusar a mensagem)
/// e no mascarador dos logs (nunca gravar o dado). São as mesmas regras do motor de PII da
/// entrega de Cyber (GS-CYBER-AQUENDA), na versão que cabe na API.
/// </summary>
public static class PadroesPii
{
    private const RegexOptions Opcoes = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
    private static readonly TimeSpan Limite = TimeSpan.FromMilliseconds(100);

    public static readonly Regex Email = new(@"[\w.+-]+@[\w-]+\.[\w.]+", Opcoes, Limite);
    public static readonly Regex Cpf = new(@"\b\d{3}\.?\d{3}\.?\d{3}-?\d{2}\b", Opcoes, Limite);
    public static readonly Regex Telefone = new(@"(\+?55[\s-]?)?\(?\b\d{2}\)?[\s-]?9?\d{4}[\s.-]?\d{4}\b", Opcoes, Limite);
    public static readonly Regex Link = new(@"(https?://|www\.|\b[\w-]+\.(com|net|org|br|io|me|app|ly|gg|link|site|xyz)\b)", Opcoes, Limite);

    /// <summary>
    /// "meu nome de registro é João", "nome morto: Carlos". Só conta com um nome logo depois
    /// (inicial maiúscula): perguntar "como mudo o nome de registro?" continua livre.
    /// </summary>
    public static readonly Regex NomeDeRegistro = new(
        @"\bnome\s+(de\s+registro|morto|de\s+batismo|civil|antigo)\s*(é|era|:|=)\s*(?-i:[A-ZÀ-Ý][\p{L}'-]+)",
        Opcoes, Limite);

    /// <summary>Senha, PIN ou chave escrita com valor, e formatos conhecidos de token.</summary>
    public static readonly Regex Segredo = new(
        @"(\b(senha|password|pin|token|chave)\b\s*(é|:|=)\s*(?=\S*[\d#@$!%&*])\S{4,})|\bsk-ant-[\w-]{10,}|\bAKIA[0-9A-Z]{16}\b|\beyJ[\w-]{8,}\.eyJ[\w-]{8,}\.[\w-]{8,}|\bBearer\s+[\w.-]{16,}",
        Opcoes, Limite);

    /// <summary>
    /// Saúde que identifica alguém: hormônio ou remédio com dose, HIV/PrEP, código CID.
    /// Nas salas é permitido (é para isso que elas existem) e o texto já é cifrado no banco;
    /// nos logs, é mascarado.
    /// </summary>
    public static readonly Regex Saude = new(
        @"\b(estradiol|estrogênio|estrogenio|testosterona|deposteron|durateston|nebido|espironolactona|ciproterona|androcur|progesterona|finasterida|bicalutamida|truvada|prep|pep|tarv|antirretrovira\w*)\b[^.\n]{0,25}?\b\d+([.,]\d+)?\s?(mg|ml|mcg|µg|g)\b"
        + @"|\b(hiv|soropositiv\w*|aids|sífilis|sifilis|hepatite\s+[bc])\b|\bCID[\s-]?(10)?[\s:-]*[A-TV-Z]\d{2}(\.\d)?\b",
        Opcoes, Limite);
}

/// <summary>Troca o dado pessoal por um marcador antes de ir para o log.</summary>
public static class MascaradorPii
{
    // A ordem importa: e-mail antes de link, CPF antes de telefone, segredo antes de tudo.
    private static readonly (Regex Padrao, string Marcador)[] Regras =
    {
        (PadroesPii.Segredo, "[SEGREDO]"),
        (PadroesPii.NomeDeRegistro, "[NOME DE REGISTRO]"),
        (PadroesPii.Email, "[E-MAIL]"),
        (PadroesPii.Cpf, "[CPF]"),
        (PadroesPii.Telefone, "[TELEFONE]"),
        (PadroesPii.Saude, "[DADO DE SAÚDE]"),
    };

    public static string Mascarar(string? texto)
    {
        if (string.IsNullOrEmpty(texto)) return texto ?? string.Empty;
        foreach (var (padrao, marcador) in Regras)
        {
            try { texto = padrao.Replace(texto, marcador); }
            catch (RegexMatchTimeoutException) { return "[LOG OMITIDO: texto grande demais para conferir]"; }
        }
        return texto;
    }
}
