using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace InvestmentLab.BuildingBlocks.Persistence;

public sealed record ModuleDbContextType(Type ContextType);
