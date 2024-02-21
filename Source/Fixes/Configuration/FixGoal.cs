using Microsoft.Dafny;

namespace DafnyDriver.ContractChecking.Fixes;

public record FixGoal(Method Outer, Method FaultyRoutine, ContractType BrokenContract) {
  public Method OuterRoutine = Outer;
  public Method FaultyRoutine = FaultyRoutine;
  public ContractType BrokenContract = BrokenContract;
}