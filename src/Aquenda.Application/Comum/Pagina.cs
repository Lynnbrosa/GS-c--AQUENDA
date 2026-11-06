namespace Aquenda.Application.Comum;

public sealed record Pagina<T>(IReadOnlyList<T> Itens, int Total, int NumeroPagina, int TamanhoPagina)
{
    public int TotalPaginas => TamanhoPagina == 0 ? 0 : (int)Math.Ceiling(Total / (double)TamanhoPagina);
}
