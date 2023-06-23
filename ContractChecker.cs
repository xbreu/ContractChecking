using System;
using System.Collections.Generic;
using System.Linq;
using DafnyTestGeneration;
using Microsoft.Dafny.ContractChecking.Fixes;

namespace Microsoft.Dafny.ContractChecking;

public class ContractChecker {
  private readonly DafnyOptions options;
  private readonly string programFile;

  public ContractChecker(DafnyOptions options, string programFile) {
    this.options = options;
    this.programFile = programFile;
  }

  private static Declaration Find(Declaration decl, IEnumerable<string> names) {
    var current = decl;

    foreach (var name in names) {
      if (name == "module_") {
        continue;
      }

      var realName = name == "default__" ? "_default" : name;

      switch (current) {
        case ModuleDecl md: {
          var sig = md.Signature;

          if (sig.TopLevels.TryGetValue(realName, out var level)) {
            current = level;
            continue;
          }

          if (sig.StaticMembers.TryGetValue(realName, out var member)) {
            current = member;
            continue;
          }

          if (sig.TopLevels.TryGetValue("_default", out level)) {
            current = level;
            continue;
          }
        }
          break;
        case TopLevelDeclWithMembers sig: {
          var found = false;
          foreach (var member in sig.Members.Union(sig.InheritedMembers)) {
            if (!member.Name.Equals(realName)) {
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

      throw new ArgumentException($"Could not find member {realName} on {current.Name}");
    }

    return current;
  }

  private SequenceResult TestMethod(Program program, string methodFullName, List<List<IResult>> arguments) {
    var method = FindMethod(program, methodFullName);
    var test = new TestCase(method, options, program, arguments);
    var trace = test.Run();
    // Console.WriteLine("Daikon Input");
    // Console.WriteLine("------------");
    // Console.WriteLine(daikonInput);
    return trace;
  }

  public static Method FindMethod(Program program, string methodFullName) {
    var l = methodFullName.Split(".");
    return (Method)Find(program.DefaultModule, l);
  }

  public void CheckProgram(Program program) {
    var arguments = new List<List<IResult>>();
    for (var i = -10; i < 11; i++) {
      arguments.Add(new List<IResult> { new IntegerResult(i) });
    }

    var trace = TestMethod(program, "M.duplicate", arguments);
    new FixGeneration(options).Weakening(null, trace, program);
    // Console.WriteLine(GetTestArguments(program).CountAsync());
  }

  private static async IAsyncEnumerable<TestMethod> GetTestArguments(Program program) {
    var tests = DafnyTestGeneration.Main.GetTestMethodsForProgram(program);
    await foreach (var test in tests) {
      Console.WriteLine($"-- {test.MethodName}");
      test.ArgValues.ForEach(Console.WriteLine);
      Console.WriteLine();
    }

    yield return null;
  }
}