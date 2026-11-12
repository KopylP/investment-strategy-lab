using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.Rebalancing.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReportActionsAsJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "actions",
                schema: "rebalancing",
                table: "reports",
                type: "jsonb",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE rebalancing.reports AS report
                SET actions = COALESCE((
                    SELECT jsonb_agg(
                        jsonb_build_object(
                            'Id', action.id,
                            'Allocations', COALESCE((
                                SELECT jsonb_agg(jsonb_build_object(
                                    'Quantity', (allocation ->> 'quantity')::numeric,
                                    'Reason', allocation -> 'reason',
                                    'SubPortfolioId', allocation -> 'subPortfolioId'))
                                FROM jsonb_array_elements(action.allocations) AS allocation), '[]'::jsonb),
                            'EstimatedCarriedLoss', action.estimated_carried_loss,
                            'EstimatedGain', action.estimated_gain,
                            'EstimatedTaxChange', action.estimated_tax_change,
                            'Fills', COALESCE((
                                SELECT jsonb_agg(jsonb_build_object(
                                    'Fee', (fill ->> 'fee')::numeric,
                                    'ExecutedOn', fill -> 'executedOn',
                                    'Linked', COALESCE((fill ->> 'linked')::boolean, false),
                                    'Note', fill -> 'note',
                                    'Number', (fill ->> 'number')::integer,
                                    'Price', (fill ->> 'price')::numeric,
                                    'Quantity', (fill ->> 'quantity')::numeric,
                                    'TransactionId', fill -> 'transactionId'))
                                FROM jsonb_array_elements(action.fills) AS fill), '[]'::jsonb),
                            'FromSubPortfolioId', action.from_sub_portfolio_id,
                            'InstrumentId', action.instrument_id,
                            'Kind', action.kind,
                            'NextFillNumber', action.next_fill_number,
                            'PlannedAmount', action.planned_amount,
                            'PlannedPrice', action.planned_price,
                            'PlannedQuantity', action.planned_quantity,
                            'Reason', action.reason,
                            'Rejection', action.rejection,
                            'Sequence', action.sequence,
                            'SkipReason', action.skip_reason,
                            'Status', action.status,
                            'ToSubPortfolioId', action.to_sub_portfolio_id)
                        ORDER BY action.sequence)
                    FROM rebalancing.report_actions AS action
                    WHERE action.report_id = report.id), '[]'::jsonb)
                """);

            migrationBuilder.DropTable(
                name: "report_actions",
                schema: "rebalancing");

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    ALTER TABLE rebalancing.reports ALTER COLUMN actions SET COMPRESSION lz4;
                EXCEPTION WHEN feature_not_supported THEN NULL;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    ALTER TABLE rebalancing.reports ALTER COLUMN sections SET COMPRESSION lz4;
                EXCEPTION WHEN feature_not_supported THEN NULL;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "report_actions",
                schema: "rebalancing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    allocations = table.Column<string>(type: "jsonb", nullable: false),
                    estimated_carried_loss = table.Column<decimal>(type: "numeric(20,8)", precision: 20, scale: 8, nullable: true),
                    estimated_gain = table.Column<decimal>(type: "numeric(20,8)", precision: 20, scale: 8, nullable: true),
                    estimated_tax_change = table.Column<decimal>(type: "numeric(20,8)", precision: 20, scale: 8, nullable: true),
                    fills = table.Column<string>(type: "jsonb", nullable: false),
                    from_sub_portfolio_id = table.Column<Guid>(type: "uuid", nullable: true),
                    instrument_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    next_fill_number = table.Column<int>(type: "integer", nullable: false),
                    planned_amount = table.Column<decimal>(type: "numeric(20,8)", precision: 20, scale: 8, nullable: false),
                    planned_price = table.Column<decimal>(type: "numeric(20,8)", precision: 20, scale: 8, nullable: false),
                    planned_quantity = table.Column<decimal>(type: "numeric(20,8)", precision: 20, scale: 8, nullable: false),
                    reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    rejection = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    report_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    skip_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    to_sub_portfolio_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_actions", x => x.id);
                    table.ForeignKey(
                        name: "fk_report_actions_reports_report_id",
                        column: x => x.report_id,
                        principalSchema: "rebalancing",
                        principalTable: "reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_report_actions_report_id",
                schema: "rebalancing",
                table: "report_actions",
                column: "report_id");

            migrationBuilder.Sql("""
                INSERT INTO rebalancing.report_actions (
                    id, report_id, allocations, estimated_carried_loss, estimated_gain, estimated_tax_change, fills,
                    from_sub_portfolio_id, instrument_id, kind, next_fill_number, planned_amount, planned_price, planned_quantity,
                    reason, rejection, sequence, skip_reason, status, to_sub_portfolio_id)
                SELECT
                    (element ->> 'Id')::uuid,
                    report.id,
                    COALESCE((
                        SELECT jsonb_agg(jsonb_build_object(
                            'quantity', (allocation ->> 'Quantity')::numeric,
                            'reason', allocation -> 'Reason',
                            'subPortfolioId', allocation -> 'SubPortfolioId'))
                        FROM jsonb_array_elements(element -> 'Allocations') AS allocation), '[]'::jsonb),
                    (element ->> 'EstimatedCarriedLoss')::numeric,
                    (element ->> 'EstimatedGain')::numeric,
                    (element ->> 'EstimatedTaxChange')::numeric,
                    COALESCE((
                        SELECT jsonb_agg(jsonb_build_object(
                            'fee', (fill ->> 'Fee')::numeric,
                            'executedOn', fill -> 'ExecutedOn',
                            'linked', COALESCE((fill ->> 'Linked')::boolean, false),
                            'note', fill -> 'Note',
                            'number', (fill ->> 'Number')::integer,
                            'price', (fill ->> 'Price')::numeric,
                            'quantity', (fill ->> 'Quantity')::numeric,
                            'transactionId', fill -> 'TransactionId'))
                        FROM jsonb_array_elements(element -> 'Fills') AS fill), '[]'::jsonb),
                    (element ->> 'FromSubPortfolioId')::uuid,
                    (element ->> 'InstrumentId')::uuid,
                    element ->> 'Kind',
                    (element ->> 'NextFillNumber')::integer,
                    (element ->> 'PlannedAmount')::numeric,
                    (element ->> 'PlannedPrice')::numeric,
                    (element ->> 'PlannedQuantity')::numeric,
                    element ->> 'Reason',
                    element ->> 'Rejection',
                    (element ->> 'Sequence')::integer,
                    element ->> 'SkipReason',
                    element ->> 'Status',
                    (element ->> 'ToSubPortfolioId')::uuid
                FROM rebalancing.reports AS report
                CROSS JOIN LATERAL jsonb_array_elements(COALESCE(report.actions, '[]'::jsonb)) AS element
                """);

            migrationBuilder.DropColumn(
                name: "actions",
                schema: "rebalancing",
                table: "reports");

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    ALTER TABLE rebalancing.reports ALTER COLUMN sections SET COMPRESSION DEFAULT;
                EXCEPTION WHEN feature_not_supported THEN NULL;
                END $$;
                """);
        }
    }
}
