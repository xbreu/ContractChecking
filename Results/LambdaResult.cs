using System.Collections.Generic;

namespace Microsoft.Dafny.ContractChecking;

internal class LambdaResult : IResult {
  public readonly List<string> Args;
  private readonly Expression body;
  private readonly Context closure;
  private readonly DafnyOptions options;


  public LambdaResult(DafnyOptions options, Expression body, IEnumerable<NonglobalVariable> args,
    Context closure = null) {
    this.body = body;
    this.options = options;
    Args = new List<string>();
    foreach (var arg in args) {
      Args.Add(arg.Name);
    }

    this.closure = closure ?? new Context(null);
  }

  public BooleanResult Eq(IResult other) {
    return this == other;
  }

  public object ToPythonObject() {
    return null;
  }

  public IResult Evaluate(Context ctx) {
    return new Evaluator(null, options).Evaluate(body, closure.Merge(ctx));
  }

  public override string ToString() {
    return $"Lambda({body})";
  }
}