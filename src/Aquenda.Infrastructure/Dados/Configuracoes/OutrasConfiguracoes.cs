using Aquenda.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aquenda.Infrastructure.Dados.Configuracoes;

public class LeituraOcupacaoConfiguration : IEntityTypeConfiguration<LeituraOcupacao>
{
    public void Configure(EntityTypeBuilder<LeituraOcupacao> b)
    {
        b.ToTable("LeiturasOcupacao");
        b.HasKey(l => l.Id);
        b.Property(l => l.Origem).HasConversion<int>();
        b.Ignore(l => l.Livres);
        b.Ignore(l => l.TaxaOcupacao);
        b.Ignore(l => l.Lotada);
        b.HasIndex(l => new { l.CasaId, l.RegistradaEmUtc });
    }
}

public class RodaConversaConfiguration : IEntityTypeConfiguration<RodaConversa>
{
    public void Configure(EntityTypeBuilder<RodaConversa> b)
    {
        b.ToTable("RodasConversa");
        b.HasKey(r => r.Id);
        b.Property(r => r.Titulo).HasMaxLength(150).IsRequired();
        b.Property(r => r.Descricao).HasMaxLength(1000).IsRequired();
        b.Property(r => r.Organizador).HasMaxLength(150).IsRequired();
        b.Property(r => r.LinkGrupo).HasMaxLength(300);
        b.Ignore(r => r.FimUtc);
        b.HasOne(r => r.Local)
            .WithMany()
            .HasForeignKey(r => r.LocalId)
            .OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(r => r.InicioUtc);
    }
}

public class InteracaoDiariaConfiguration : IEntityTypeConfiguration<InteracaoDiaria>
{
    public void Configure(EntityTypeBuilder<InteracaoDiaria> b)
    {
        b.ToTable("InteracoesDiarias");
        b.HasKey(i => i.Id);
        b.Property(i => i.Tipo).HasConversion<int>();
        b.Property(i => i.Categoria).HasConversion<int?>();
        b.HasIndex(i => new { i.Dia, i.Tipo, i.Categoria, i.ServicoId });
    }
}
