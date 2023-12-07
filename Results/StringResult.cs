using System;
using Python.Runtime;

namespace Microsoft.Dafny.ContractChecking;

public class StringResult : IResult {
  public readonly string Value;

  public StringResult(string value) {
    this.Value = value;
  }

  public BooleanResult Eq(IResult other) {
    return string.Compare(Value, ((StringResult)other).Value, StringComparison.Ordinal) == 0;
  }

  public object ToPythonObject() {
    return new PyString(Value);
  }

  public string ToDaikonInput() {
    return Value.Trim('\'');
  }

  public string ToPythonInput() {
    return Value;
  }

  public override string ToString() {
    return $"String({Value})";
  }
}