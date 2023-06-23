using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.BaseTypes;

namespace Microsoft.Dafny.ContractChecking;

public interface IResult {
  public BooleanResult Eq(IResult other);

  public object ToPythonObject();

  public string ToDaikonInput();

  public BooleanResult Neq(IResult other) {
    return !Eq(other);
  }

  public bool IsEqual(IResult other) {
    return Eq(other).Value;
  }

  public bool IsDifferent(IResult other) {
    return Eq(other).Value == false;
  }

  public static IResult FromLiteralExpression(LiteralExpr e) {
    return (e, e.Value) switch {
      (StringLiteralExpr, string v) => new StringResult(v),
      (CharLiteralExpr, string v) => new CharResult(v),
      (_, BigDec v) => new RealResult(v),
      (_, BigInteger v) => new IntegerResult(v),
      (_, bool v) => new BooleanResult(v),
      _ => throw new ArgumentOutOfRangeException()
    };
  }
}

public class ResultComparer : IEqualityComparer<IResult> {
  private static readonly ResultComparer Singleton = new();

  private ResultComparer() {
  }

  public bool Equals(IResult left, IResult right) {
    if (left == null) {
      return right == null;
    }

    return right != null && left.IsEqual(right);
  }

  public int GetHashCode(IResult obj) {
    return obj.GetType().GetHashCode();
  }

  public static ResultComparer Get() {
    return Singleton;
  }
}