using Aquenda.Domain.Chat;
using Microsoft.EntityFrameworkCore;

namespace Aquenda.Infrastructure.Dados.Seed;

/// <summary>As quatro salas temáticas do pitch.</summary>
public static class ChatSeed
{
    private const string Moderacao = "ONG parceira (a definir com o Centro de Referência)";

    public static async Task<int> SemearAsync(AquendaDbContext db, CancellationToken ct = default)
    {
        if (await db.SalasChat.AnyAsync(ct)) return 0;

        db.SalasChat.AddRange(
            new SalaChat("primeiro-emprego", "Primeiro emprego",
                "Currículo, entrevista, nome social no trabalho e vagas afirmativas.", Moderacao),
            new SalaChat("transicao", "Transição",
                "Hormonização pelo SUS, retificação de nome e o dia a dia. Sem indicação de dose: isso é com a equipe de saúde.", Moderacao),
            new SalaChat("familia", "Família",
                "Contar para a família, lidar com rejeição e encontrar a família que a gente escolhe.", Moderacao),
            new SalaChat("recem-chegades", "Recém-chegades a SP",
                "Quem acabou de chegar na cidade: onde ficar, como circular e onde encontrar gente.", Moderacao));

        return await db.SaveChangesAsync(ct);
    }
}
