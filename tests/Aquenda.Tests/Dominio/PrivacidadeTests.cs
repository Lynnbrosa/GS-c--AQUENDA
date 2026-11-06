using Aquenda.Api.Infra;
using Aquenda.Domain.Chat;
using Aquenda.Domain.Comum;
using Aquenda.Domain.Privacidade;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aquenda.Tests.Dominio;

/// <summary>Filtro de PII das salas e dos logs: casos positivos (barra) e negativos (deixa passar).</summary>
public class PrivacidadeTests
{
    private static readonly ModeradorAutomatico Moderador = ModeradorAutomatico.Padrao;

    [Theory]
    [InlineData("meu nome de registro é João")]
    [InlineData("Meu nome morto era Carlos, ninguém mais me chama assim")]
    [InlineData("nome de batismo: Marcos")]
    public void Recusa_nome_de_registro(string texto) =>
        Assert.Contains("nome de registro", Moderador.PrimeiraViolacao(texto));

    [Theory]
    [InlineData("como faço para mudar o nome de registro no cartório?")]
    [InlineData("a retificação do nome de registro saiu, que alívio")]
    [InlineData("o nome morto é um assunto difícil pra mim")]
    public void Falar_sobre_nome_de_registro_continua_livre(string texto) =>
        Assert.Null(Moderador.PrimeiraViolacao(texto));

    [Theory]
    [InlineData("minha senha: Kiwi#2026")]
    [InlineData("o pin é 4821")]
    [InlineData("usa essa chave sk-ant-api03-abcdefghijklmnop")]
    [InlineData("Bearer eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjMifQ.abcDEF123456ghiJKL")]
    public void Recusa_segredo(string texto) =>
        Assert.Contains("senha, PIN ou chave", Moderador.PrimeiraViolacao(texto));

    [Theory]
    [InlineData("a chave é ter paciência com a família")]
    [InlineData("esqueci a senha do app do banco, alguém sabe como recuperar?")]
    public void Falar_de_senha_sem_valor_continua_livre(string texto) =>
        Assert.Null(Moderador.PrimeiraViolacao(texto));

    [Fact]
    public void Saude_e_permitida_na_sala()
    {
        // É para isso que as salas existem; o texto vai cifrado para o banco.
        Assert.Null(Moderador.PrimeiraViolacao("Comecei estradiol 2mg semana passada, alguém sentiu enjoo?"));
    }

    [Fact]
    public void Apelido_com_nome_de_registro_tambem_e_recusado() =>
        Assert.Throws<DomainException>(() => Moderador.Verificar("nome morto: Paulo"));

    [Theory]
    [InlineData("Login de 529.982.247-25 pela recepção", "Login de [CPF] pela recepção")]
    [InlineData("contato lu@exemplo.com.br", "contato [E-MAIL]")]
    [InlineData("ligar (11) 98765-4321", "ligar [TELEFONE]")]
    [InlineData("texto: tomo estradiol 2mg por dia", "texto: tomo [DADO DE SAÚDE] por dia")]
    [InlineData("texto: sou soropositiva há 5 anos", "texto: sou [DADO DE SAÚDE] há 5 anos")]
    [InlineData("meu nome de registro é João", "meu [NOME DE REGISTRO]")]
    [InlineData("Authorization Bearer abcdefghijklmnopqrstuvwxyz123", "Authorization [SEGREDO]")]
    public void Mascarador_troca_o_dado_pelo_marcador(string entrada, string esperado) =>
        Assert.Equal(esperado, MascaradorPii.Mascarar(entrada));

    [Fact]
    public void Mascarador_nao_mexe_em_log_comum() =>
        Assert.Equal("Request finished in 12ms 200", MascaradorPii.Mascarar("Request finished in 12ms 200"));

    [Fact]
    public void Formatador_do_console_grava_so_a_versao_mascarada()
    {
        var formatador = new FormatadorLogSemPii();
        var saida = new StringWriter();
        var erro = new InvalidOperationException("falhou para 529.982.247-25");
        var entrada = new LogEntry<string>(LogLevel.Warning, "Aquenda.Chat", new EventId(7), "mensagem de lu@exemplo.com: tomo testosterona 250mg",
            erro, (s, _) => s);

        formatador.Write(entrada, null, saida);

        var texto = saida.ToString();
        Assert.Contains("warn: Aquenda.Chat[7]", texto);
        Assert.Contains("mensagem de [E-MAIL]: tomo [DADO DE SAÚDE]", texto);
        Assert.Contains("falhou para [CPF]", texto);
        Assert.DoesNotContain("529.982.247-25", texto);
        Assert.DoesNotContain("testosterona", texto);
        Assert.DoesNotContain("lu@exemplo.com", texto);
    }
}
