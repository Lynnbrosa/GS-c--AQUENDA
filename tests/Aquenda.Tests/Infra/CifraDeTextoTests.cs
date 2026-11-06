using System.Security.Cryptography;
using Aquenda.Infrastructure;
using Aquenda.Infrastructure.Chat;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Aquenda.Tests.Infra;

public class CifraDeTextoTests
{
    private const string Sensivel = "Comecei estradiol 2mg semana passada";

    private static List<string> TextosCrus(Cenario c)
    {
        using var cmd = c.Banco.Conexao.CreateCommand();
        cmd.CommandText = "SELECT Texto FROM MensagensChat ORDER BY Id";
        using var r = cmd.ExecuteReader();
        var textos = new List<string>();
        while (r.Read()) textos.Add(r.GetString(0));
        return textos;
    }

    private static async Task<(string Token, int Sala)> EnviarAsync(Cenario c, string texto)
    {
        await using var db = c.Contexto();
        var sala = await c.SalaAsync("transicao");
        var token = (await c.Chat(db).EntrarAsync("Nina", true, true)).Token;
        await c.Chat(db).EnviarAsync(token, sala, texto);
        return (token, sala);
    }

    [Fact]
    public void Ida_e_volta_com_nonce_novo_a_cada_vez()
    {
        var cifra = new CifraDeTexto(RandomNumberGenerator.GetBytes(32));
        var a = cifra.Cifrar(Sensivel);
        var b = cifra.Cifrar(Sensivel);
        Assert.StartsWith(CifraDeTexto.Prefixo, a);
        Assert.NotEqual(a, b);
        Assert.DoesNotContain("estradiol", a);
        Assert.Equal(Sensivel, cifra.Decifrar(a));
        Assert.Equal("texto antigo", cifra.Decifrar("texto antigo"));
    }

    [Fact]
    public void Chave_errada_ou_texto_adulterado_nao_abre()
    {
        var cifrado = new CifraDeTexto(RandomNumberGenerator.GetBytes(32)).Cifrar(Sensivel);
        Assert.ThrowsAny<CryptographicException>(() => new CifraDeTexto(RandomNumberGenerator.GetBytes(32)).Decifrar(cifrado));
    }

    [Fact]
    public async Task Banco_guarda_so_o_texto_cifrado_e_a_api_le_normal()
    {
        using var c = await Cenario.ComSeedAsync();
        var (token, sala) = await EnviarAsync(c, Sensivel);

        var cru = Assert.Single(TextosCrus(c));
        Assert.True(CifraDeTexto.EstaCifrado(cru));
        Assert.DoesNotContain("estradiol", cru);

        await using var db = c.Contexto();
        Assert.Equal(Sensivel, Assert.Single(await c.Chat(db).ListarMensagensAsync(token, sala, null, 50)).Texto);
    }

    [Fact]
    public async Task Mensagens_de_antes_da_cifra_sao_cifradas_na_inicializacao()
    {
        using var c = await Cenario.ComSeedAsync();
        var (token, sala) = await EnviarAsync(c, "mensagem nova");
        using (var cmd = c.Banco.Conexao.CreateCommand())
        {
            cmd.CommandText = "UPDATE MensagensChat SET Texto = $t";
            cmd.Parameters.AddWithValue("$t", Sensivel); // como estava gravado antes desta versão
            cmd.ExecuteNonQuery();
        }

        await using (var db = c.Contexto())
            Assert.Equal(1, await DependencyInjection.CifrarMensagensAntigasAsync(db));

        Assert.All(TextosCrus(c), t => Assert.True(CifraDeTexto.EstaCifrado(t)));
        await using var leitura = c.Contexto();
        Assert.Equal(Sensivel, Assert.Single(await c.Chat(leitura).ListarMensagensAsync(token, sala, null, 50)).Texto);
        Assert.Equal(0, await DependencyInjection.CifrarMensagensAntigasAsync(leitura));
    }

    [Fact]
    public void Chave_fica_num_arquivo_fora_do_banco_e_e_reaproveitada()
    {
        var pasta = Directory.CreateTempSubdirectory("aquenda-cifra");
        try
        {
            var conexao = $"Data Source={Path.Combine(pasta.FullName, "aquenda.db")}";
            var config = new ConfigurationBuilder().Build();
            var cifrado = CifraDeTexto.DaConfiguracao(config, conexao).Cifrar(Sensivel);
            Assert.True(File.Exists(Path.Combine(pasta.FullName, "aquenda-chat.chave")));
            Assert.Equal(Sensivel, CifraDeTexto.DaConfiguracao(config, conexao).Decifrar(cifrado));

            var chave = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var configurada = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Chat:ChaveCifra"] = chave }).Build();
            Assert.Equal("ok", CifraDeTexto.DaConfiguracao(configurada, conexao).Decifrar(new CifraDeTexto(Convert.FromBase64String(chave)).Cifrar("ok")));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            pasta.Delete(true);
        }
    }
}
