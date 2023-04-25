using System.Collections.Generic;
using System.Linq;
using Python.Runtime;

namespace Microsoft.Dafny.ContractChecking;

// TODO: Infinite sets
public class SetResult : IBagResult {
  public readonly HashSet<IResult> Value;

  public SetResult(IEnumerable<IResult> value) {
    Value = new HashSet<IResult>(value, ResultComparer.Get());
  }

  public IntegerResult Length() {
    return Value.Count;
  }

  public BooleanResult Contains(IResult element) {
    return Value.Contains(element);
  }

  public IBagResult Union(IBagResult otherB) {
    var other = (SetResult)otherB;
    return new SetResult(Value.Union(other.Value, ResultComparer.Get()));
  }

  public IBagResult Intersection(IBagResult otherB) {
    var other = (SetResult)otherB;
    return new SetResult(Value.Intersect(other.Value, ResultComparer.Get()));
  }

  public IBagResult Difference(IBagResult otherB) {
    var other = (SetResult)otherB;
    return new SetResult(Value.Except(other.Value, ResultComparer.Get()));
  }

  public BooleanResult Eq(IResult other) {
    return Value.SetEquals(((SetResult)other).Value);
  }

  public object ToPythonObject() {
    return Value.ToPython();
  }

  public override string ToString() {
    var result = Value.Aggregate("Set(", (current, item) => current + $"{item},");
    result.Remove(result.Length - 1, 1);
    result += ")";
    return result;
  }
}