using System;
using Python.Runtime;

namespace Microsoft.Dafny.ContractChecking;

public class StringResult : IResult {
  private readonly string value;

  public StringResult(string value) {
    this.value = value;
  }

  public BooleanResult Eq(IResult other) {
    return string.Compare(value, ((StringResult)other).value, StringComparison.Ordinal) == 0;
  }

  public object ToPythonObject() {
    return new PyString(value);
  }

  public override string ToString() {
    return $"String({value})";
  }
}