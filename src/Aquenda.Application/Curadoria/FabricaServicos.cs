using Aquenda.Domain;
using Aquenda.Domain.Comum;
using Aquenda.Domain.Entidades;
using Aquenda.Domain.ValueObjects;

namespace Aquenda.Application.Curadoria;

/// <summary>Fábrica: escolhe a classe concreta de <see cref="Servico"/> pela categoria.</summary>
public static class FabricaServicos
{
    public static Servico Criar(NovoServico n, Coordenada? coordenada, DateOnly hoje)
    {
        var endereco = new Endereco(n.EnderecoSigiloso ? null : n.Logradouro, n.Bairro, n.Cidade,
            NormalizarCep(n.Cep), coordenada, n.EnderecoSigiloso);
        var verificadoEm = n.VerificadoEm ?? hoje;

        Servico servico = n.Categoria switch
        {
            CategoriaServico.Acolhimento => new CasaAcolhimento(n.Nome, n.Descricao, endereco, n.Fonte, verificadoEm,
                Obrigatorio(n.PublicoAlvo, "publicoAlvo"), n.ExigeEncaminhamento, n.CapacidadeDeclarada, n.Atendimentos),
            CategoriaServico.CentroReferencia => new CentroReferencia(n.Nome, n.Descricao, endereco, n.Fonte, verificadoEm,
                n.Atendimentos, n.InscreveTranscidadania),
            CategoriaServico.Saude => new UnidadeSaude(n.Nome, n.Descricao, endereco, n.Fonte, verificadoEm, n.Atendimentos,
                n.TipoAtencao ?? throw new DomainException("Informe tipoAtencao (Primaria ou Secundaria)."),
                n.RedeSampaTrans, n.HabilitadaProcessoTransexualizador, n.ExigeEncaminhamento, n.CodigoCnes),
            CategoriaServico.Juridico => new ServicoJuridico(n.Nome, n.Descricao, endereco, n.Fonte, verificadoEm,
                n.Orgao ?? n.Nome, n.AtendeRetificacao, n.RecebeDenuncia),
            CategoriaServico.Role => new LocalRole(n.Nome, n.Descricao, endereco, n.Fonte, verificadoEm,
                n.TipoRole ?? throw new DomainException("Informe tipoRole (Bar, Balada, Cultural ou EspacoPublico)."),
                n.FaixaPreco, n.Acessivel),
            _ => throw new DomainException("Categoria inválida.")
        };

        servico.DefinirContato(n.Telefone, n.Site);
        servico.DefinirInstrucaoAcesso(n.InstrucaoAcesso);
        servico.DefinirHorarios(n.Horarios.Select(h => new HorarioFuncionamento(h.Dia, h.Abre, h.Fecha)));
        return servico;
    }

    /// <summary>Aceita "01049-000" ou "01049000" e devolve só os 8 dígitos.</summary>
    public static string? NormalizarCep(string? cep)
    {
        if (string.IsNullOrWhiteSpace(cep)) return null;
        var digitos = new string(cep.Where(char.IsDigit).ToArray());
        if (digitos.Length != 8) throw new DomainException("CEP deve ter 8 dígitos.");
        return digitos;
    }

    private static string Obrigatorio(string? valor, string campo) =>
        string.IsNullOrWhiteSpace(valor) ? throw new DomainException($"Informe {campo}.") : valor;
}
