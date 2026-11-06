using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aquenda.Infrastructure.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InteracoesDiarias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Dia = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Categoria = table.Column<int>(type: "INTEGER", nullable: true),
                    ServicoId = table.Column<int>(type: "INTEGER", nullable: true),
                    Quantidade = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InteracoesDiarias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Servicos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Logradouro = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Bairro = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Cidade = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Cep = table.Column<string>(type: "TEXT", maxLength: 9, nullable: true),
                    Latitude = table.Column<double>(type: "REAL", nullable: true),
                    Longitude = table.Column<double>(type: "REAL", nullable: true),
                    EnderecoSigiloso = table.Column<bool>(type: "INTEGER", nullable: false),
                    Telefone = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    Site = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Atendimentos = table.Column<int>(type: "INTEGER", nullable: false),
                    InstrucaoAcesso = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Fonte = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    VerificadoEm = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    AvisosFechamento = table.Column<int>(type: "INTEGER", nullable: false),
                    UltimoAvisoEmUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TipoServico = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PublicoAlvo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CapacidadeDeclarada = table.Column<int>(type: "INTEGER", nullable: true),
                    ExigeEncaminhamento = table.Column<bool>(type: "INTEGER", nullable: true),
                    InscreveTranscidadania = table.Column<bool>(type: "INTEGER", nullable: true),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: true),
                    FaixaPreco = table.Column<int>(type: "INTEGER", nullable: true),
                    Acessivel = table.Column<bool>(type: "INTEGER", nullable: true),
                    TagOsm = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Orgao = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    AtendeRetificacao = table.Column<bool>(type: "INTEGER", nullable: true),
                    RecebeDenuncia = table.Column<bool>(type: "INTEGER", nullable: true),
                    CodigoCnes = table.Column<string>(type: "TEXT", maxLength: 7, nullable: true),
                    TipoAtencao = table.Column<int>(type: "INTEGER", nullable: true),
                    HabilitadaProcessoTransexualizador = table.Column<bool>(type: "INTEGER", nullable: true),
                    RedeSampaTrans = table.Column<bool>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servicos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HorariosFuncionamento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DiaSemana = table.Column<int>(type: "INTEGER", nullable: false),
                    Abre = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    Fecha = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    ServicoId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorariosFuncionamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HorariosFuncionamento_Servicos_ServicoId",
                        column: x => x.ServicoId,
                        principalTable: "Servicos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeiturasOcupacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CasaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Total = table.Column<int>(type: "INTEGER", nullable: false),
                    Ocupadas = table.Column<int>(type: "INTEGER", nullable: false),
                    RegistradaEmUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RecebidaEmUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Origem = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeiturasOcupacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeiturasOcupacao_Servicos_CasaId",
                        column: x => x.CasaId,
                        principalTable: "Servicos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RodasConversa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Titulo = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Organizador = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    InicioUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DuracaoMinutos = table.Column<int>(type: "INTEGER", nullable: false),
                    Presencial = table.Column<bool>(type: "INTEGER", nullable: false),
                    LocalId = table.Column<int>(type: "INTEGER", nullable: true),
                    LinkGrupo = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    IdadeMinima = table.Column<int>(type: "INTEGER", nullable: false),
                    Ilustrativa = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RodasConversa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RodasConversa_Servicos_LocalId",
                        column: x => x.LocalId,
                        principalTable: "Servicos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HorariosFuncionamento_ServicoId",
                table: "HorariosFuncionamento",
                column: "ServicoId");

            migrationBuilder.CreateIndex(
                name: "IX_InteracoesDiarias_Dia_Tipo_Categoria_ServicoId",
                table: "InteracoesDiarias",
                columns: new[] { "Dia", "Tipo", "Categoria", "ServicoId" });

            migrationBuilder.CreateIndex(
                name: "IX_LeiturasOcupacao_CasaId_RegistradaEmUtc",
                table: "LeiturasOcupacao",
                columns: new[] { "CasaId", "RegistradaEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RodasConversa_InicioUtc",
                table: "RodasConversa",
                column: "InicioUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RodasConversa_LocalId",
                table: "RodasConversa",
                column: "LocalId");

            migrationBuilder.CreateIndex(
                name: "IX_Servicos_Ativo",
                table: "Servicos",
                column: "Ativo");

            migrationBuilder.CreateIndex(
                name: "IX_Servicos_Bairro",
                table: "Servicos",
                column: "Bairro");

            migrationBuilder.CreateIndex(
                name: "IX_Servicos_CodigoCnes",
                table: "Servicos",
                column: "CodigoCnes");

            migrationBuilder.CreateIndex(
                name: "IX_Servicos_Latitude_Longitude",
                table: "Servicos",
                columns: new[] { "Latitude", "Longitude" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HorariosFuncionamento");

            migrationBuilder.DropTable(
                name: "InteracoesDiarias");

            migrationBuilder.DropTable(
                name: "LeiturasOcupacao");

            migrationBuilder.DropTable(
                name: "RodasConversa");

            migrationBuilder.DropTable(
                name: "Servicos");
        }
    }
}
