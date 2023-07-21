using System.Collections.Generic;
using System.Linq;
using System.Text;
using Python.Runtime;

namespace Microsoft.Dafny.ContractChecking;

public class SequenceResult : ICollectionResult {
  public readonly List<IResult> Value;

  public SequenceResult(IEnumerable<IResult> value) {
    Value = value.ToList();
  }

  public IntegerResult Length() {
    return Value.Count;
  }

  public BooleanResult Contains(IResult element) {
    return Value.Find(x => x.IsEqual(element)) != null;
  }

  public BooleanResult Eq(IResult other) {
    // See if the lists are compatible
    var otherL = ((SequenceResult)other).Value;
    if (Value.Count != otherL.Count) {
      return false;
    }

    // Compare each element
    return !Value.Where((t, i) => t.IsDifferent(otherL[i])).Any();
  }

  public object ToPythonObject() {
    return Value.ToPython();
  }

  public string ToDaikonInput() {
    var result = new StringBuilder();
    result.Append("[ ");
    foreach (var v in Value) {
      if (v == null) {
        result.Append("null ");
      } else {
        result.Append($"{v.ToDaikonInput()} ");
      }
    }

    result.Append(']');
    return result.ToString();
  }

  public BooleanResult Prefix(SequenceResult other) {
    // See if the lists are compatible
    var otherL = other.Value;
    if (Value.Count > otherL.Count) {
      return false;
    }

    // Compare each element
    return !Value.Where((t, i) => t.IsDifferent(otherL[i])).Any();
  }

  public BooleanResult ProperPrefix(SequenceResult other) {
    return Prefix(other) & ((IResult)this).Neq(other);
  }

  public SequenceResult Concat(SequenceResult other) {
    return new SequenceResult(Value.Concat(other.Value));
  }

  public IResult At(IntegerResult index) {
    return Value[(int)index.Value];
  }

  public IResult Range(IntegerResult index, IntegerResult count) {
    return new SequenceResult(Value.GetRange((int)index.Value, (int)count.Value));
  }

  public IResult UpdateAt(IntegerResult index, IResult value) {
    var newList = new List<IResult>(Value) {
      [(int)index.Value] = value
    };
    return new SequenceResult(newList);
  }

  public override string ToString() {
    var result = Value.Aggregate("Sequence(", (current, item) => current + $"{item},");
    result.Remove(result.Length - 1, 1);
    result += ")";
    return result;
  }
}