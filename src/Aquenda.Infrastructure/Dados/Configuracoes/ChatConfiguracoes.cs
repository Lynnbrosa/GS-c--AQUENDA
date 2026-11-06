using Aquenda.Domain.Chat;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aquenda.Infrastructure.Dados.Configuracoes;

public class SalaChatConfiguration : IEntityTypeConfiguration<SalaChat>
{
    public void Configure(EntityTypeBuilder<SalaChat> b)
    {
        b.ToTable("SalasChat");
        b.HasKey(s => s.Id);
        b.Property(s => s.Slug).HasMaxLength(40).IsRequired();
        b.Property(s => s.Nome).HasMaxLength(80).IsRequired();
        b.Property(s => s.Descricao).HasMaxLength(300).IsRequired();
        b.Property(s => s.ModeradaPor).HasMaxLength(150).IsRequired();
        b.HasIndex(s => s.Slug).IsUnique();
    }
}

public class ParticipanteChatConfiguration : IEntityTypeConfiguration<ParticipanteChat>
{
    public void Configure(EntityTypeBuilder<ParticipanteChat> b)
    {
        b.ToTable("ParticipantesChat");
        b.HasKey(p => p.Id);
        b.Property(p => p.Apelido).HasMaxLength(24).IsRequired();
        b.Property(p => p.ApelidoNormalizado).HasMaxLength(24).IsRequired();
        b.Property(p => p.TokenHash).HasMaxLength(64).IsRequired();
        b.HasIndex(p => p.ApelidoNormalizado).IsUnique();
        b.HasIndex(p => p.TokenHash).IsUnique();
    }
}

public class MensagemChatConfiguration : IEntityTypeConfiguration<MensagemChat>
{
    public void Configure(EntityTypeBuilder<MensagemChat> b)
    {
        b.ToTable("MensagensChat");
        b.HasKey(m => m.Id);
        b.Property(m => m.Texto).HasMaxLength(SalaChat.TamanhoMaximoMensagem).IsRequired();
        b.Property(m => m.MotivoOcultacao).HasMaxLength(300);
        b.Ignore(m => m.AguardandoModeracao);

        b.HasOne<SalaChat>().WithMany().HasForeignKey(m => m.SalaId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(m => m.Participante).WithMany().HasForeignKey(m => m.ParticipanteId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(m => m.Denuncias).WithOne().HasForeignKey(d => d.MensagemId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(m => new { m.SalaId, m.Oculta, m.Id });
        b.HasIndex(m => new { m.ParticipanteId, m.EnviadaEmUtc });
        b.HasIndex(m => m.EnviadaEmUtc);
    }
}

public class DenunciaMensagemConfiguration : IEntityTypeConfiguration<DenunciaMensagem>
{
    public void Configure(EntityTypeBuilder<DenunciaMensagem> b)
    {
        b.ToTable("DenunciasMensagem");
        b.HasKey(d => d.Id);
        b.HasOne<ParticipanteChat>().WithMany().HasForeignKey(d => d.ParticipanteId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(d => new { d.MensagemId, d.ParticipanteId }).IsUnique();
    }
}
