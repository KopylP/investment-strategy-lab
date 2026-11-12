using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.StrategyBuilder.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CompressLargeJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    ALTER TABLE strategy_builder.drafts ALTER COLUMN graph SET COMPRESSION lz4;
                EXCEPTION WHEN feature_not_supported THEN NULL;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    ALTER TABLE strategy_builder.published_layouts ALTER COLUMN graph SET COMPRESSION lz4;
                EXCEPTION WHEN feature_not_supported THEN NULL;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    ALTER TABLE strategy_builder.drafts ALTER COLUMN graph SET COMPRESSION DEFAULT;
                EXCEPTION WHEN feature_not_supported THEN NULL;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    ALTER TABLE strategy_builder.published_layouts ALTER COLUMN graph SET COMPRESSION DEFAULT;
                EXCEPTION WHEN feature_not_supported THEN NULL;
                END $$;
                """);
        }
    }
}
