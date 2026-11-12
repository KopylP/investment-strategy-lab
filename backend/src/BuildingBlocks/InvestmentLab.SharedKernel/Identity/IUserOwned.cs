namespace InvestmentLab.SharedKernel;

public interface IUserOwned
{
    UserId OwnerId { get; }
}
