using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Dafny;

namespace DafnyDriver.ContractChecking.Fixes;

public record ContractManager {
  private readonly Dictionary<(Method, ContractType), Expression> overwriteList = new();

  public ContractManager() {
  }

  private ContractManager(Dictionary<(Method, ContractType), Expression> overwriteList) {
    this.overwriteList = overwriteList;
  }

  private void Relax(Method method, ContractType type) {
    Update(method, type, new LiteralExpr(null, true));
  }

  public void Relax(FixGoal configurationGoal) {
    Relax(configurationGoal.FaultyRoutine, configurationGoal.BrokenContract);
  }

  public void Update(Method method, ContractType type, Expression newContract) {
    overwriteList.Remove((method, type));
    overwriteList.Add((method, type), newContract);
  }

  public Expression GetContract(Method method, ContractType place) {
    var found = overwriteList.TryGetValue((method, place), out var contract);
    return !found ? CondenseContract(method, place) : contract;
  }

  private static Expression CondenseContract(Method method, ContractType place) {
    Expression finalContract = new LiteralExpr(null, true);
    var currentContracts = place == ContractType.PRE_CONDITION ? method.Req : method.Ens;
    finalContract = currentContracts.Aggregate(finalContract,
      (current, e) => new BinaryExpr(null, BinaryExpr.Opcode.And, current, e.E));
    return finalContract;
  }

  public Expression Weaken(Method method, ContractType place, Expression appendage) {
    var currentContract = GetContract(method, place);
    var newContract = new BinaryExpr(null, BinaryExpr.Opcode.Or, currentContract, appendage);
    Update(method, place, newContract);
    return newContract;
  }

  public Expression Strengthen(Method method, ContractType place, Expression appendage) {
    var currentContract = GetContract(method, place);
    var newContract = new BinaryExpr(null, BinaryExpr.Opcode.And, currentContract, appendage);
    Update(method, place, newContract);
    return newContract;
  }

  public override string ToString() {
    var b = new StringBuilder();
    foreach (var ((method, place), newContract) in overwriteList) {
      b.AppendLine($"Change {place} of method {method} to {newContract}");
    }

    return b.ToString();
  }

  public ContractManager Copy() {
    var copyOfOverwriteList = overwriteList.ToDictionary(
      entry => entry.Key,
      entry => entry.Value);
    return new ContractManager(copyOfOverwriteList);
  }
}