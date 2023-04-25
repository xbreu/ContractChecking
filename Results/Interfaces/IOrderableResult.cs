namespace Microsoft.Dafny.ContractChecking;

public interface IOrderableResult : IResult {
  public BooleanResult Lt(IOrderableResult other);

  public BooleanResult Le(IOrderableResult other) {
    return Lt(other) | Eq(other);
  }

  public BooleanResult Ge(IOrderableResult other) {
    return !Lt(other);
  }

  public BooleanResult Gt(IOrderableResult other) {
    return !Le(other);
  }
}