using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketClassifier.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Batches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NomeArquivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Total = table.Column<int>(type: "integer", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Batches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParametrosClassificacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Termo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Alvo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParametrosClassificacao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Similaridades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketOrigemId = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketRelacionadoId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagsCompartilhadas = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Similaridades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Assunto = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Descricao = table.Column<string>(type: "text", nullable: false),
                    Categoria = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Outro"),
                    Prioridade = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Média"),
                    Departamento = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Suporte"),
                    Resumo = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    Confianca = table.Column<double>(type: "double precision", nullable: false),
                    Justificativa = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    Sentimento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "neutro"),
                    Tags = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    ProcessadoOk = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    RegistroModificado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DataModificacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tickets_Batches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "Batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParametrosClassificacao_Tipo",
                table: "ParametrosClassificacao",
                column: "Tipo");

            migrationBuilder.CreateIndex(
                name: "IX_Similaridades_TicketOrigemId",
                table: "Similaridades",
                column: "TicketOrigemId");

            migrationBuilder.CreateIndex(
                name: "IX_Similaridades_TicketRelacionadoId",
                table: "Similaridades",
                column: "TicketRelacionadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_BatchId",
                table: "Tickets",
                column: "BatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParametrosClassificacao");

            migrationBuilder.DropTable(
                name: "Similaridades");

            migrationBuilder.DropTable(
                name: "Tickets");

            migrationBuilder.DropTable(
                name: "Batches");
        }
    }
}
