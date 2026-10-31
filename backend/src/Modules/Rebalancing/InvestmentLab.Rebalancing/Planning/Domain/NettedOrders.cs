namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record NettedOrders(IReadOnlyList<BrokerOrder> BrokerOrders, IReadOnlyList<InternalTransfer> Transfers);
