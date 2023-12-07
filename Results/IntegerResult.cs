using System.Numerics;
using Python.Runtime;

namespace Microsoft.Dafny.ContractChecking;

public class IntegerResult : INumericResult {
  public BigInteger Value;

  public IntegerResult(BigInteger value) {
    Value = value;
  }

  public INumericResult Neg() {
    return new IntegerResult(-Value);
  }

  public BooleanResult Eq(IResult other) {
    return Value.Equals(((IntegerResult)other).Value);
  }

  public object ToPythonObject() {
    return new PyInt(Value);
  }

  public string ToDaikonInput() {
    return Value.ToString();
  }
  
  public string ToPythonInput() {
    return Value.ToString();
  }

  public BooleanResult Lt(IOrderableResult other) {
    return Value < ((IntegerResult)other).Value;
  }

  public INumericResult Add(INumericResult other) {
    return new IntegerResult(Value + ((IntegerResult)other).Value);
  }

  public INumericResult Sub(INumericResult other) {
    return new IntegerResult(Value - ((IntegerResult)other).Value);
  }

  public INumericResult Mul(INumericResult other) {
    return new IntegerResult(Value * ((IntegerResult)other).Value);
  }

  public INumericResult Div(INumericResult other) {
    // TODO: Euclidean division
    return new IntegerResult(Value / ((IntegerResult)other).Value);
  }

  public static implicit operator IntegerResult(BigInteger i) {
    return new IntegerResult(i);
  }

  public static implicit operator IntegerResult(int i) {
    return new IntegerResult(new BigInteger(i));
  }

  public BooleanResult IsZero() {
    return Value.Equals(0);
  }

  public IntegerResult Mod(IntegerResult other) {
    return Value % other.Value;
  }

  public static IntegerResult Zero() {
    return new BigInteger(0);
  }

  public static IntegerResult One() {
    return new BigInteger(1);
  }

  public override string ToString() {
    return $"Integer({Value.ToString()})";
  }
}