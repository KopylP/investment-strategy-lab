using System.Reflection;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.BuildingBlocks.Tests.Persistence;

public sealed class ModuleDbContextTests
{
    private sealed class Sample
    {
        public Guid Id { get; set; }
        public Currency TradingCurrency { get; set; } = Currency.EUR;
        public Isin? Isin { get; set; }
        public UserId OwnerId { get; set; }
        public YearMonth Period { get; set; }
        public decimal Amount { get; set; }
    }

    private sealed class SampleDbContext(DbContextOptions<SampleDbContext> options) : ModuleDbContext(options)
    {
        protected override string Schema => "sample";

        protected override Assembly MappingsAssembly => typeof(ModuleDbContextTests).Assembly;

        protected override bool IsMappedHere(Type configuration) => configuration.DeclaringType == typeof(ModuleDbContextTests);
    }

    private sealed class OwnedSample : IUserOwned
    {
        public Guid Id { get; set; }
        public UserId OwnerId { get; set; }
    }

    private sealed class OwnedSampleConfiguration : IEntityTypeConfiguration<OwnedSample>
    {
        public void Configure(EntityTypeBuilder<OwnedSample> builder) => builder.HasKey(sample => sample.Id);
    }

    private sealed class SampleConfiguration : IEntityTypeConfiguration<Sample>
    {
        public void Configure(EntityTypeBuilder<Sample> builder) => builder.HasKey(sample => sample.Id);
    }

    private static SampleDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<SampleDbContext>()
            .UseNpgsql("Host=unused")
            .UseSnakeCaseNamingConvention()
            .Options);

    [Fact]
    public void Entities_owned_by_a_user_are_filtered_by_owner_and_others_are_not()
    {
        using var context = CreateContext();

        context.Model.FindEntityType(typeof(OwnedSample))!.GetDeclaredQueryFilters().Count.ShouldBe(1);
        context.Model.FindEntityType(typeof(Sample))!.GetDeclaredQueryFilters().ShouldBeEmpty();
    }

    [Fact]
    public void Tables_live_in_the_module_schema()
    {
        using var context = CreateContext();

        context.Model.FindEntityType(typeof(Sample))!.GetSchema().ShouldBe("sample");
    }

    [Theory]
    [InlineData(nameof(Sample.TradingCurrency), typeof(string))]
    [InlineData(nameof(Sample.Isin), typeof(string))]
    [InlineData(nameof(Sample.OwnerId), typeof(Guid))]
    [InlineData(nameof(Sample.Period), typeof(string))]
    public void Shared_kernel_value_objects_are_stored_as_primitives(string propertyName, Type providerType)
    {
        using var context = CreateContext();

        var property = context.Model.FindEntityType(typeof(Sample))!.FindProperty(propertyName)!;

        property.GetValueConverter().ShouldNotBeNull();
        property.GetValueConverter()!.ProviderClrType.ShouldBe(providerType);
    }

    [Fact]
    public void Decimals_keep_enough_precision_for_fractional_shares_and_prices()
    {
        using var context = CreateContext();

        var property = context.Model.FindEntityType(typeof(Sample))!.FindProperty(nameof(Sample.Amount))!;

        property.GetPrecision().ShouldBe(20);
        property.GetScale().ShouldBe(8);
    }
}
