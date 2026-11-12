namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public sealed class PublishedLayout : AggregateRoot<Guid>, IUserOwned
{
    public UserId OwnerId { get; private set; }
    public Guid StrategyId { get; private set; }
    public int Version { get; private set; }
    public StrategyGraph Graph { get; private set; }
    public DraftLayout Layout { get; private set; }

    private PublishedLayout(Guid id) : base(id) => (Graph, Layout) = (null!, null!);

    public static PublishedLayout Of(UserId owner, PublishedVersion published, StrategyGraph graph, DraftLayout layout) => new(Guid.CreateVersion7())
    {
        OwnerId = owner,
        StrategyId = published.StrategyId,
        Version = published.Version,
        Graph = graph,
        Layout = layout,
    };
}
