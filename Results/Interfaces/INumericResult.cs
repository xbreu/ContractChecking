namespace Microsoft.Dafny.ContractChecking;

public interface INumericResult : IOrderableResult {
  public INumericResult Neg();
  public INumericResult Add(INumericResult other);
  public INumericResult Sub(INumericResult other);
  public INumericResult Mul(INumericResult other);
  public INumericResult Div(INumericResult other);
}