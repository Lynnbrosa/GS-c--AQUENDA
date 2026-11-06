using Aquenda.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aquenda.Infrastructure.Dados.Configuracoes;

/// <summary>
/// Todos os tipos de serviço ficam numa tabela só (TPH), separados pela coluna "TipoServico".
/// Assim a busca percorre o catálogo inteiro com uma consulta.
/// </summary>
public class ServicoConfiguration : IEntityTypeConfiguration<Servico>
{
    public void Configure(EntityTypeBuilder<Servico> b)
    {
        b.ToTable("Servicos");
        b.HasKey(s => s.Id);

        b.HasDiscriminator<string>(Discriminadores.Coluna)
            .HasValue<CasaAcolhimento>(Discriminadores.Casa)
            .HasValue<CentroReferencia>(Discriminadores.Centro)
            .HasValue<UnidadeSaude>(Discriminadores.Saude)
            .HasValue<ServicoJuridico>(Discriminadores.Juridico)
            .HasValue<LocalRole>(Discriminadores.Role);
        b.Property<string>(Discriminadores.Coluna).HasMaxLength(20);

        b.Property(s => s.Nome).HasMaxLength(150).IsRequired();
        b.Property(s => s.Descricao).HasMaxLength(1000).IsRequired();
        b.Property(s => s.Telefone).HasMaxLength(30);
        b.Property(s => s.Site).HasMaxLength(300);
        b.Property(s => s.InstrucaoAcesso).HasMaxLength(500);
        b.Property(s => s.Fonte).HasMaxLength(500).IsRequired();
        b.Property(s => s.Atendimentos).HasConversion<int>();

        b.Ignore(s => s.Categoria);
        b.Ignore(s => s.PrecisaRevisao);

        b.OwnsOne(s => s.Endereco, e =>
        {
            e.Property(x => x.Logradouro).HasColumnName("Logradouro").HasMaxLength(200);
            e.Property(x => x.Bairro).HasColumnName("Bairro").HasMaxLength(100).IsRequired();
            e.Property(x => x.Cidade).HasColumnName("Cidade").HasMaxLength(100).IsRequired();
            e.Property(x => x.Cep).HasColumnName("Cep").HasMaxLength(9);
            e.Property(x => x.Latitude).HasColumnName("Latitude");
            e.Property(x => x.Longitude).HasColumnName("Longitude");
            e.Property(x => x.Sigiloso).HasColumnName("EnderecoSigiloso");
            e.Ignore(x => x.Coordenada);
            e.HasIndex(x => x.Bairro);
            e.HasIndex(x => new { x.Latitude, x.Longitude });
        });
        b.Navigation(s => s.Endereco).IsRequired();

        b.OwnsMany(s => s.Horarios, h =>
        {
            h.ToTable("HorariosFuncionamento");
            h.WithOwner().HasForeignKey("ServicoId");
            h.Property<int>("Id");
            h.HasKey("Id");
            h.Ignore(x => x.DiaInteiro);
            h.Ignore(x => x.AtravessaMeiaNoite);
        });

        b.Property(s => s.IdExterno).HasMaxLength(80);
        b.HasIndex(s => s.IdExterno).IsUnique();
        b.HasIndex(s => s.Ativo);
    }
}

public class CasaAcolhimentoConfiguration : IEntityTypeConfiguration<CasaAcolhimento>
{
    public void Configure(EntityTypeBuilder<CasaAcolhimento> b)
    {
        b.Property(c => c.PublicoAlvo).HasMaxLength(200);
        b.Property(c => c.ExigeEncaminhamento).HasColumnName("ExigeEncaminhamento");
        b.HasMany(c => c.Leituras)
            .WithOne()
            .HasForeignKey(l => l.CasaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class UnidadeSaudeConfiguration : IEntityTypeConfiguration<UnidadeSaude>
{
    public void Configure(EntityTypeBuilder<UnidadeSaude> b)
    {
        b.Property(u => u.CodigoCnes).HasMaxLength(7);
        b.Property(u => u.TipoAtencao).HasConversion<int>();
        // Mesmo significado que na casa de acolhimento: as duas usam a mesma coluna.
        b.Property(u => u.ExigeEncaminhamento).HasColumnName("ExigeEncaminhamento");
        b.HasIndex(u => u.CodigoCnes);
    }
}

public class ServicoJuridicoConfiguration : IEntityTypeConfiguration<ServicoJuridico>
{
    public void Configure(EntityTypeBuilder<ServicoJuridico> b) =>
        b.Property(j => j.Orgao).HasMaxLength(200);
}

public class LocalRoleConfiguration : IEntityTypeConfiguration<LocalRole>
{
    public void Configure(EntityTypeBuilder<LocalRole> b)
    {
        b.Property(r => r.Tipo).HasConversion<int>();
        b.Property(r => r.TagOsm).HasMaxLength(10);
    }
}
