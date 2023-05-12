using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Dafny.ContractChecking;

public class TestCase {
  public enum TestResult {
    Passing,
    Failing
  }

  private readonly List<IResult> arguments;

  private readonly Method method;
  private readonly DafnyOptions options;
  private readonly ObjectResult target;

  public TestCase(Method method, DafnyOptions options, List<IResult> arguments = null, ObjectResult target = null) {
    this.method = method;
    this.options = options;
    this.arguments = arguments ?? new List<IResult>();
    this.target = target;
  }

  public TestResult Run(Evaluator evaluator) {
    Console.WriteLine($"Running method {method.Name}");

    var i = 0;
    var parameters = method.Ins.ToDictionary(formal => formal.Name, _ => arguments[i++]);
    var ctx = new Context(null, parameters);
    var anyFail = false;
    method.Req.ForEach(req => {
      var e = req.E;
      var satisfied = (BooleanResult)evaluator.Evaluate(e, ctx);
      if (satisfied) {
        return;
      }

      Console.WriteLine($"The pre-condition \"{req.E}\" was not satisfied");
      anyFail = true;
    });

    var (ret, (ctx0, ctx1), trace) = PythonExecutor.RunPythonCodeAndReturn(
      GetModuleName(), GetClassName(), method.Name,
      new List<string>(), arguments, method.IsStatic);
    Console.WriteLine($":- {ret} {ret.GetType()}");
    Console.WriteLine($":- {ctx0}");
    Console.WriteLine($"{trace.GetType()}");
    return anyFail ? TestResult.Failing : TestResult.Passing;
  }

  private string GetModuleName() {
    var items = method.FullSanitizedName.Split(".");
    return string.Join("_Compile.", items.Take(items.Length - 2)) + "_Compile";
  }

  private string GetClassName() {
    var items = method.FullSanitizedName.Split(".");
    var className = items[^2];
    if (className.StartsWith("__")) {
      className = string.Join("", className.Skip(2)) + "__";
    }

    return className;
  }
}