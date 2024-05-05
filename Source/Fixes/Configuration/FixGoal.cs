using Microsoft.Dafny;

namespace DafnyRepair.Fixes.Configuration;

public record FixGoal(Method Outer, Method FaultyRoutine, ContractType BrokenContract)
{
    public readonly ContractType BrokenContract = BrokenContract;
    public readonly Method FaultyRoutine = FaultyRoutine;
    public Method OuterRoutine = Outer;
}
