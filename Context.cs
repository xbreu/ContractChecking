using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Dafny.ContractChecking;

public class Context {
  private readonly Dictionary<string, IResult> arguments;
  private readonly List<ObjectResult> obj;

  public Context(List<ObjectResult> obj, Dictionary<string, IResult> args = null) {
    this.obj = obj;
    arguments = args ?? new Dictionary<string, IResult>();
  }

  public void Add(string name, IResult value) {
    arguments[name] = value;
  }

  public Context Copy() {
    var newArgs = arguments.ToDictionary(x => x.Key, x => x.Value);
    return new Context(obj, newArgs);
  }

  public IResult GetValue(string name) {
    IResult val;
    if (obj != null) {
      foreach (var item in obj) {
        item.Attributes.TryGetValue(name, out val);
      }
    }

    arguments.TryGetValue(name, out val);
    return val;
  }

  public Context AddObj(ObjectResult o) {
    var result = Copy();
    result.obj.Add(o);
    return result;
  }

  public Context Merge(Context other) {
    var result = arguments.ToDictionary(x => x.Key, x => x.Value);
    foreach (var y in other.arguments) {
      result[y.Key] = y.Value;
    }

    return new Context(obj.Concat(other.obj).ToList(), result);
  }
}