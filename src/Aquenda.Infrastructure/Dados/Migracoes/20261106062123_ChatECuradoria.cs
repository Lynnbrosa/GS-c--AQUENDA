using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aquenda.Infrastructure.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class ChatECuradoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdExterno",
                table: "Servicos",
                type: "TEXT",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ParticipantesChat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Apelido = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    ApelidoNormalizado = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    TokenHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CriadoEmUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SilenciadoAteUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Banido = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipantesChat", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalasChat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    ModeradaPor = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Aberta = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalasChat", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MensagensChat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SalaId = table.Column<int>(type: "INTEGER", nullable: false),
                    ParticipanteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Texto = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    EnviadaEmUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Oculta = table.Column<bool>(type: "INTEGER", nullable: false),
                    MotivoOcultacao = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    RevisadaEmUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MensagensChat", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MensagensChat_ParticipantesChat_ParticipanteId",
                        column: x => x.ParticipanteId,
                        principalTable: "ParticipantesChat",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MensagensChat_SalasChat_SalaId",
                        column: x => x.SalaId,
                        principalTable: "SalasChat",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DenunciasMensagem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MensagemId = table.Column<int>(type: "INTEGER", nullable: false),
                    ParticipanteId = table.Column<int>(type: "INTEGER", nullable: false),
                    CriadaEmUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DenunciasMensagem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DenunciasMensagem_MensagensChat_MensagemId",
                        column: x => x.MensagemId,
                        principalTable: "MensagensChat",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DenunciasMensagem_ParticipantesChat_ParticipanteId",
                        column: x => x.ParticipanteId,
                        principalTable: "ParticipantesChat",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Servicos_IdExterno",
                table: "Servicos",
                column: "IdExterno",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DenunciasMensagem_MensagemId_ParticipanteId",
                table: "DenunciasMensagem",
                columns: new[] { "MensagemId", "ParticipanteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DenunciasMensagem_ParticipanteId",
                table: "DenunciasMensagem",
                column: "ParticipanteId");

            migrationBuilder.CreateIndex(
                name: "IX_MensagensChat_EnviadaEmUtc",
                table: "MensagensChat",
                column: "EnviadaEmUtc");

            migrationBuilder.CreateIndex(
                name: "IX_MensagensChat_ParticipanteId_EnviadaEmUtc",
                table: "MensagensChat",
                columns: new[] { "ParticipanteId", "EnviadaEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MensagensChat_SalaId_Oculta_Id",
                table: "MensagensChat",
                columns: new[] { "SalaId", "Oculta", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantesChat_ApelidoNormalizado",
                table: "ParticipantesChat",
                column: "ApelidoNormalizado",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantesChat_TokenHash",
                table: "ParticipantesChat",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalasChat_Slug",
                table: "SalasChat",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DenunciasMensagem");

            migrationBuilder.DropTable(
                name: "MensagensChat");

            migrationBuilder.DropTable(
                name: "ParticipantesChat");

            migrationBuilder.DropTable(
                name: "SalasChat");

            migrationBuilder.DropIndex(
                name: "IX_Servicos_IdExterno",
                table: "Servicos");

            migrationBuilder.DropColumn(
                name: "IdExterno",
                table: "Servicos");
        }
    }
}
