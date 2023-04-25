using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Dafny.ContractChecking;

public class ContractChecker {
  private readonly DafnyOptions options;
  private readonly string programFile;

  public ContractChecker(DafnyOptions options, string programFile) {
    this.options = options;
    this.programFile = programFile;
  }

  public static Declaration Find(Declaration decl, IEnumerable<string> names) {
    var current = decl;
    foreach (var name in names) {
      switch (current) {
        case ModuleDecl md: {
          var sig = md.Signature;

          if (sig.TopLevels.TryGetValue(name, out var level)) {
            current = level;
            continue;
          }

          if (sig.StaticMembers.TryGetValue(name, out var member)) {
            current = member;
            continue;
          }
        }
          break;
        case TopLevelDeclWithMembers sig: {
          var found = false;
          foreach (var member in sig.Members.Union(sig.InheritedMembers)) {
            if (!member.Name.Equals(name)) {
              continue;
            }

            current = member;
            found = true;
            break;
          }

          if (found) {
            continue;
          }
        }
          break;
      }

      throw new ArgumentException($"Could not find member {name} on {current.Name}");
    }

    return current;
  }

  public void TestMethod(Program program, string methodFullName, List<IResult> arguments) {
    var l = methodFullName.Split(".");
    var method = (Method)Find(program.DefaultModule, l);
    var moduleName = string.Join("_Compile.", l.Take(l.Length - 3));
    string className = null;
    if (method.EnclosingClass is DefaultClassDecl) {
      moduleName += l[^2] + "_Compile";
    } else {
      className = l[^2];
    }

    className ??= "default__";

    var methodName = l.Last();
    
    Console.WriteLine($"Testing contract of method \"{methodName}\" from module \"{moduleName}\" and class \"{className}\"...");

    var i = 0;
    var parameters = method.Ins.ToDictionary(formal => formal.Name, _ => arguments[i++]);
    var ctx = new Context(null, parameters);
    var anyFail = false;
    method.Req.ForEach(req => {
      var evaluator = new Evaluator(program, options);
      var e = req.E;
      var satisfied = (BooleanResult)evaluator.Evaluate(e, ctx);
      if (satisfied) {
        return;
      }

      Console.WriteLine($"The pre-condition \"{req.E}\" was not satisfied");
      anyFail = true;
    });

    var result = PythonExecutor.RunPythonCodeAndReturn(moduleName, className, methodName, arguments, method.IsStatic);
    Console.WriteLine($"Type: {result.Item1} {result.Item1.GetType()}");
    
    // TODO: Support more than one return
    // TODO: Parse Python return and add it to the context
    // ctx.Add(method.Outs[0].Name, result.Item1);
    
    method.Ens.ForEach(ens => {
      var evaluator = new Evaluator(program, options);
      var e = ens.E;
      var satisfied = (BooleanResult)evaluator.Evaluate(e, ctx);
      if (satisfied) {
        return;
      }

      Console.WriteLine($"The post-condition \"{ens.E}\" was not satisfied");
      anyFail = true;
    });

    if (!anyFail) {
      Console.WriteLine($"All pre- and post-conditions were satisfied");
    }
  }

  public void CheckProgram(Program program) {
    TestMethod(program, "M.test", new List<IResult> {
      new IntegerResult(1)
    });
  }
}