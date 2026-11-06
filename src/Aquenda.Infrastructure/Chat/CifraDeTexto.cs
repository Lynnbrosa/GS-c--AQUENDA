using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Aquenda.Infrastructure.Chat;

/// <summary>
/// Cifra o texto das mensagens das salas antes de ir para o banco (AES-256-GCM).
/// As conversas falam de hormônio, HIV e transição: quem copiar o arquivo do banco não lê nada sem a chave.
/// Formato gravado: "v1:" + base64(nonce 12 | tag 16 | texto cifrado).
/// Texto sem o prefixo é de antes da cifra e volta como está (a inicialização cifra o que sobrou).
/// </summary>
public sealed class CifraDeTexto
{
    public const string Prefixo = "v1:";
    private const int TamanhoNonce = 12;
    private const int TamanhoTag = 16;

    private readonly byte[] _chave;

    public CifraDeTexto(byte[] chave)
    {
        if (chave.Length != 32) throw new ArgumentException("A chave precisa ter 32 bytes (AES-256).", nameof(chave));
        _chave = chave;
    }

    public static bool EstaCifrado(string? valor) => valor is not null && valor.StartsWith(Prefixo, StringComparison.Ordinal);

    public string Cifrar(string texto)
    {
        var claro = Encoding.UTF8.GetBytes(texto);
        var saida = new byte[TamanhoNonce + TamanhoTag + claro.Length];
        var nonce = saida.AsSpan(0, TamanhoNonce);
        RandomNumberGenerator.Fill(nonce);
        using var aes = new AesGcm(_chave, TamanhoTag);
        aes.Encrypt(nonce, claro, saida.AsSpan(TamanhoNonce + TamanhoTag), saida.AsSpan(TamanhoNonce, TamanhoTag));
        return Prefixo + Convert.ToBase64String(saida);
    }

    public string Decifrar(string valor)
    {
        if (!EstaCifrado(valor)) return valor;
        var dados = Convert.FromBase64String(valor[Prefixo.Length..]);
        var claro = new byte[dados.Length - TamanhoNonce - TamanhoTag];
        using var aes = new AesGcm(_chave, TamanhoTag);
        aes.Decrypt(dados.AsSpan(0, TamanhoNonce), dados.AsSpan(TamanhoNonce + TamanhoTag), dados.AsSpan(TamanhoNonce, TamanhoTag), claro);
        return Encoding.UTF8.GetString(claro);
    }

    /// <summary>
    /// A chave vem de Chat:ChaveCifra (base64 de 32 bytes, para a VM ou um cofre de segredos).
    /// Sem ela, fica num arquivo ao lado do banco, criado na primeira vez: nunca dentro do próprio banco.
    /// </summary>
    public static CifraDeTexto DaConfiguracao(IConfiguration config, string conexao)
    {
        var configurada = config[$"{ChatOptions.Secao}:ChaveCifra"];
        if (!string.IsNullOrWhiteSpace(configurada))
            return new CifraDeTexto(Convert.FromBase64String(configurada));

        var banco = new SqliteConnectionStringBuilder(conexao).DataSource;
        if (string.IsNullOrWhiteSpace(banco) || banco == ":memory:")
            return new CifraDeTexto(RandomNumberGenerator.GetBytes(32));

        var arquivo = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(banco)) ?? ".", "aquenda-chat.chave");
        if (File.Exists(arquivo))
            return new CifraDeTexto(Convert.FromBase64String(File.ReadAllText(arquivo).Trim()));

        var nova = RandomNumberGenerator.GetBytes(32);
        File.WriteAllText(arquivo, Convert.ToBase64String(nova));
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(arquivo, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        return new CifraDeTexto(nova);
    }
}
