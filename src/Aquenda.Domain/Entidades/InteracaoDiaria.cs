using Aquenda.Domain.Comum;

namespace Aquenda.Domain.Entidades;

/// <summary>
/// Contador anônimo do indicador de impacto. Guarda só "quantas vezes, em que dia,
/// em qual serviço". Não existe campo para usuário, IP ou aparelho, de propósito.
/// </summary>
public class InteracaoDiaria : Entidade
{
    public DateOnly Dia { get; private set; }
    public TipoInteracao Tipo { get; private set; }
    public CategoriaServico? Categoria { get; private set; }
    public int? ServicoId { get; private set; }
    public int Quantidade { get; private set; }

    protected InteracaoDiaria() { }

    public InteracaoDiaria(DateOnly dia, TipoInteracao tipo, CategoriaServico? categoria, int? servicoId)
    {
        if (!Enum.IsDefined(tipo)) throw new DomainException("Tipo de interação inválido.");
        if (tipo != TipoInteracao.Busca && servicoId is null)
            throw new DomainException("\"Como chegar\", \"ligar\" e \"abrir site\" precisam do serviço.");
        Dia = dia;
        Tipo = tipo;
        Categoria = categoria;
        ServicoId = servicoId;
    }

    public void Incrementar() => Quantidade++;
}
