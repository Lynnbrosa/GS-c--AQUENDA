namespace Aquenda.Domain.Comum;

/// <summary>
/// Base de todas as entidades persistidas. A identidade é o Id, não o conteúdo.
/// </summary>
public abstract class Entidade
{
    public int Id { get; protected set; }

    public override bool Equals(object? obj)
    {
        if (obj is not Entidade outra || outra.GetType() != GetType())
            return false;
        // Entidades ainda não salvas (Id 0) só são iguais a si mesmas.
        if (Id == 0 || outra.Id == 0)
            return ReferenceEquals(this, outra);
        return Id == outra.Id;
    }

    public override int GetHashCode() => Id == 0 ? base.GetHashCode() : HashCode.Combine(GetType(), Id);
}
