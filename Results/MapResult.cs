using System.Collections.Generic;
using System.Linq;
using System.Text;
using Python.Runtime;

namespace Microsoft.Dafny.ContractChecking;

// TODO: Infinite maps
public class MapResult : ATableResult<IResult> {
  public MapResult(Dictionary<IResult, IResult> value) {
    Value = value;
  }

  public override IntegerResult Length() {
    return Value.Count;
  }

  public override object ToPythonObject() {
    return Value.ToPython();
  }

  public override string ToDaikonInput() {
    var result = new StringBuilder();
    result.Append("[ ");
    foreach (var p in Value) {
      result.Append($"[ {p.Key.ToDaikonInput()} {p.Value.ToDaikonInput()} ] ");
    }

    result.Append(']');
    return result.ToString();
  }

  protected override bool AreDifferent(IResult left, IResult right) {
    return left.IsDifferent(right);
  }

  public MapResult Merge(MapResult other) {
    var result = Value.ToDictionary(x => x.Key, x => x.Value, ResultComparer.Get());
    foreach (var y in other.Value) {
      result[y.Key] = y.Value;
    }

    return new MapResult(result);
  }

  public MapResult Difference(SetResult keys) {
    var result = Value.ToDictionary(x => x.Key, x => x.Value, ResultComparer.Get());
    foreach (var key in keys.Value) {
      result.Remove(key);
    }

    return new MapResult(result);
  }

  public IResult Get(IResult key) {
    return Value[key];
  }

  public MapResult Update(IResult key, IResult value) {
    var result = Value.ToDictionary(x => x.Key, x => x.Value, ResultComparer.Get());
    result[key] = value;
    return new MapResult(result);
  }

  public override string ToString() {
    var result = Value.Aggregate("Map(", (current, item) => current + $"{item.Key}:={item.Value},");
    result.Remove(result.Length - 1, 1);
    result += ")";
    return result;
  }
}