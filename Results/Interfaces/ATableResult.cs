using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Dafny.ContractChecking;

public abstract class ATableResult<T> : ICollectionResult {
  public Dictionary<IResult, T> Value;

  public abstract IntegerResult Length();

  public BooleanResult Contains(IResult element) {
    return Value.ContainsKey(element);
  }

  public BooleanResult Eq(IResult otherR) {
    var other = (ATableResult<T>)otherR;
    var thisKeys = GetKeys();
    return thisKeys.SetEquals(other.GetKeys()) &&
           thisKeys.All(key => !AreDifferent(GetValue(key), other.GetValue(key)));
  }

  public abstract object ToPythonObject();
  public abstract string ToDaikonInput();
  
  public abstract string ToPythonInput();

  protected abstract bool AreDifferent(T left, T right);

  private HashSet<IResult> GetKeys() {
    return new HashSet<IResult>(Value.Keys, ResultComparer.Get());
  }

  private T GetValue(IResult key) {
    return Value[key];
  }
}