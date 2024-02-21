using System.Collections.Generic;
using System.Linq;
using System.Text;
using Python.Runtime;

namespace Microsoft.Dafny.ContractChecking;

// TODO: Infinite multisets
public class MultisetResult : ATableResult<IntegerResult>, IBagResult {
  public MultisetResult(IEnumerable<IResult> value) {
    Value = new Dictionary<IResult, IntegerResult>(ResultComparer.Get());
    foreach (var item in value) {
      if (!Value.ContainsKey(item)) {
        Value[item] = IntegerResult.One();
      } else {
        Value[item] = (IntegerResult)Value[item].Add(IntegerResult.One());
      }
    }
  }

  private MultisetResult(Dictionary<IResult, IntegerResult> value) {
    Value = value;
  }

  public IBagResult Union(IBagResult otherB) {
    var other = (MultisetResult)otherB;
    var result = new Dictionary<IResult, IntegerResult>();
    foreach (var item in Value.Keys.Union(other.Value.Keys, ResultComparer.Get())) {
      Value.TryGetValue(item, out var count1);
      other.Value.TryGetValue(item, out var count2);
      count1 ??= IntegerResult.Zero();
      count2 ??= IntegerResult.Zero();
      result[item] = (IntegerResult)count1.Add(count2);
    }

    return new MultisetResult(result);
  }

  public IBagResult Intersection(IBagResult otherB) {
    var other = (MultisetResult)otherB;
    var result = new Dictionary<IResult, IntegerResult>();
    foreach (var item in Value.Keys.Union(other.Value.Keys, ResultComparer.Get())) {
      Value.TryGetValue(item, out var count1);
      other.Value.TryGetValue(item, out var count2);
      count1 ??= IntegerResult.Zero();
      count2 ??= IntegerResult.Zero();
      var commonCount = count1.Lt(count2) ? count1 : count2;

      if (!commonCount.IsZero()) {
        result[item] = commonCount;
      }
    }

    return new MultisetResult(result);
  }

  public IBagResult Difference(IBagResult otherB) {
    var other = (MultisetResult)otherB;
    var result = new Dictionary<IResult, IntegerResult>();
    foreach (var item in Value.Keys.Union(other.Value.Keys, ResultComparer.Get())) {
      Value.TryGetValue(item, out var count1);
      other.Value.TryGetValue(item, out var count2);
      count1 ??= IntegerResult.Zero();
      count2 ??= IntegerResult.Zero();
      var subtractedCount = (IntegerResult)count1.Sub(count2);
      if (subtractedCount.Lt(IntegerResult.Zero())) {
        subtractedCount = IntegerResult.Zero();
      }

      if (!subtractedCount.IsZero()) {
        result[item] = subtractedCount;
      }
    }

    return new MultisetResult(result);
  }

  public override IntegerResult Length() {
    var len = IntegerResult.Zero();
    return Value.Values.Aggregate(len, (current, val) => (IntegerResult)current.Add(val));
  }

  public override object ToPythonObject() {
    return Value.ToPython();
  }

  public override string ToDaikonInput() {
    var result = new StringBuilder();
    result.Append("[ ");
    foreach (var v in Value) {
      for (var i = 0; i < v.Value.Value; i++) {
        result.Append($"{v.Key.ToDaikonInput()} ");
      }
    }

    result.Append(']');
    return result.ToString();
  }

  public IntegerResult Multiplicity(IResult element) {
    Value.TryGetValue(element, out var ret);
    ret ??= IntegerResult.Zero();
    return ret;
  }

  public MultisetResult UpdateMultiplicity(IResult element, IntegerResult multiplicity) {
    var result = Value.ToDictionary(x => x.Key, x => x.Value, ResultComparer.Get());
    if (multiplicity.IsZero()) {
      if (result.ContainsKey(element)) {
        result.Remove(element);
      }
    } else {
      result[element] = multiplicity;
    }

    return new MultisetResult(result);
  }

  protected override bool AreDifferent(IntegerResult left, IntegerResult right) {
    return !((IResult)left).IsEqual(right);
  }

  public override string ToString() {
    var result = Value.Aggregate("Multiset(", (current, item) => current + $"{item.Key}:={item.Value},");
    result.Remove(result.Length - 1, 1);
    result += ")";
    return result;
  }
}