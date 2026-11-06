using Aquenda.Domain.Chat;
using Aquenda.Domain.Comum;

namespace Aquenda.Tests.Dominio;

public class ModeradorAutomaticoTests
{
    [Theory]
    [InlineData("me chama em https://golpe.com", "Links")]
    [InlineData("entra no www.grupo.net", "Links")]
    [InlineData("olha meuperfil.com.br", "Links")]
    [InlineData("meu email é fulane@gmail.com", "e-mail")]
    [InlineData("meu cpf 123.456.789-09", "CPF")]
    [InlineData("12345678909", "CPF")]
    [InlineData("liga (11) 98765-4321", "telefone")]
    [InlineData("zap 11 98765 4321", "telefone")]
    [InlineData("+55 11 98765-4321", "telefone")]
    public void Recusa_dado_pessoal_e_link(string texto, string trecho)
    {
        var erro = Assert.Throws<DomainException>(() => ModeradorAutomatico.Padrao.Verificar(texto));
        Assert.Contains(trecho, erro.Message);
    }

    [Fact]
    public void Onze_digitos_seguidos_sao_recusados_seja_cpf_ou_celular() =>
        Assert.NotNull(ModeradorAutomatico.Padrao.PrimeiraViolacao("me chama 11987654321"));

    [Theory]
    [InlineData("O Transcidadania paga R$ 1.702,05 por mês")]
    [InlineData("Fui na UBS República às 14h e deu certo!")]
    [InlineData("Alguém já fez retificação no cartório? Demorou 15 dias pra mim.")]
    [InlineData("Tô em SP faz 2 meses, cheguei de BH")]
    public void Deixa_passar_conversa_normal(string texto) =>
        Assert.Null(ModeradorAutomatico.Padrao.PrimeiraViolacao(texto));

    [Fact]
    public void Regras_sao_plugaveis()
    {
        var soLink = new ModeradorAutomatico(new IRegraConteudo[] { new RegraSemLink() });
        Assert.Null(soLink.PrimeiraViolacao("liga 11987654321"));
        Assert.NotNull(soLink.PrimeiraViolacao("https://x.com"));
    }
}

public class ParticipanteChatTests
{
    private static readonly DateTime Agora = new(2026, 11, 16, 15, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Cria_com_token_aleatorio_e_guarda_so_o_hash()
    {
        var (a, tokenA) = ParticipanteChat.Criar("Nina", true, true, Agora);
        var (_, tokenB) = ParticipanteChat.Criar("Nina", true, true, Agora);

        Assert.NotEqual(tokenA, tokenB);
        Assert.True(tokenA.Length >= 40);
        Assert.Equal(ParticipanteChat.HashToken(tokenA), a.TokenHash);
        Assert.DoesNotContain(tokenA, a.TokenHash);
        Assert.Equal("NINA", a.ApelidoNormalizado);
    }

    [Fact]
    public void Exige_aceite_das_regras_e_maioridade()
    {
        Assert.Contains("regras", Assert.Throws<DomainException>(() => ParticipanteChat.Criar("Nina", false, true, Agora)).Message);
        Assert.Contains("18", Assert.Throws<DomainException>(() => ParticipanteChat.Criar("Nina", true, false, Agora)).Message);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("um apelido grande demais aqui")]
    [InlineData("nina<script>")]
    [InlineData("nina11987654")]
    [InlineData("Moderacao")]
    [InlineData("admin")]
    [InlineData("nina.com")]
    public void Recusa_apelido_invalido(string apelido) =>
        Assert.Throws<DomainException>(() => ParticipanteChat.Criar(apelido, true, true, Agora));

    [Fact]
    public void Silencio_tem_limite_e_expira()
    {
        var (p, _) = ParticipanteChat.Criar("Nina", true, true, Agora);
        Assert.Throws<DomainException>(() => p.Silenciar(TimeSpan.FromDays(31), Agora));
        p.Silenciar(TimeSpan.FromHours(2), Agora);
        Assert.True(p.EstaSilenciado(Agora.AddHours(1)));
        Assert.False(p.EstaSilenciado(Agora.AddHours(2)));
    }
}

public class SalaEMensagemTests
{
    private static readonly DateTime Agora = new(2026, 11, 16, 15, 0, 0, DateTimeKind.Utc);

    private static SalaChat Sala() => new("transicao", "Transição", "", "ONG");

    private static ParticipanteChat Pessoa(string apelido, int id)
    {
        var (p, _) = ParticipanteChat.Criar(apelido, true, true, Agora);
        typeof(Entidade).GetProperty(nameof(Entidade.Id))!.SetValue(p, id);
        return p;
    }

    [Fact]
    public void Publica_texto_limpo_e_aparado()
    {
        var m = Sala().Publicar(Pessoa("Nina", 1), "  oi gente  ", Agora, ModeradorAutomatico.Padrao);
        Assert.Equal("oi gente", m.Texto);
        Assert.Equal(1, m.ParticipanteId);
    }

    [Fact]
    public void Sala_fechada_silenciado_e_banido_nao_publicam()
    {
        var sala = Sala();
        var nina = Pessoa("Nina", 1);

        sala.Fechar();
        Assert.Throws<DomainException>(() => sala.Publicar(nina, "oi", Agora, ModeradorAutomatico.Padrao));
        sala.Reabrir();

        nina.Silenciar(TimeSpan.FromHours(1), Agora);
        Assert.Throws<DomainException>(() => sala.Publicar(nina, "oi", Agora, ModeradorAutomatico.Padrao));

        var bia = Pessoa("Bia", 2);
        bia.Banir();
        Assert.Throws<DomainException>(() => sala.Publicar(bia, "oi", Agora, ModeradorAutomatico.Padrao));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Mensagem_vazia_e_recusada(string texto) =>
        Assert.Throws<DomainException>(() => Sala().Publicar(Pessoa("Nina", 1), texto, Agora, ModeradorAutomatico.Padrao));

    [Fact]
    public void Mensagem_longa_demais_e_recusada() =>
        Assert.Throws<DomainException>(() =>
            Sala().Publicar(Pessoa("Nina", 1), new string('a', SalaChat.TamanhoMaximoMensagem + 1), Agora, ModeradorAutomatico.Padrao));

    [Fact]
    public void Tres_denuncias_de_pessoas_diferentes_ocultam_e_restaurar_limpa()
    {
        var m = Sala().Publicar(Pessoa("Nina", 1), "texto", Agora, ModeradorAutomatico.Padrao);

        Assert.Throws<DomainException>(() => m.Denunciar(Pessoa("Nina", 1), Agora));       // a própria autora
        Assert.False(m.Denunciar(Pessoa("Bia", 2), Agora));
        Assert.Throws<DomainException>(() => m.Denunciar(Pessoa("Bia", 2), Agora));        // em dobro
        Assert.False(m.Denunciar(Pessoa("Cris", 3), Agora));
        Assert.False(m.Oculta);
        Assert.True(m.AguardandoModeracao);
        Assert.True(m.Denunciar(Pessoa("Duda", 4), Agora));
        Assert.True(m.Oculta);

        m.Restaurar(Agora);
        Assert.False(m.Oculta);
        Assert.Empty(m.Denuncias);
        Assert.False(m.AguardandoModeracao);
    }

    [Fact]
    public void Slug_e_moderacao_sao_obrigatorios()
    {
        Assert.Throws<DomainException>(() => new SalaChat("Com Espaço", "X", "", "ONG"));
        Assert.Throws<DomainException>(() => new SalaChat("ok", "X", "", " "));
    }
}
