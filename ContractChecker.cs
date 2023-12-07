using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DafnyDriver.ContractChecking.Fixes;
using DafnyTestGeneration;
using Microsoft.Dafny.ContractChecking.Fixes;

namespace Microsoft.Dafny.ContractChecking;

public class ContractChecker {
  private readonly string programFile;

  public ContractChecker(DafnyOptions options, string programFile) {
    this.programFile = programFile;
    FixConfiguration.CreateInstance(null, null, options);
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

  private SequenceResult TestMethod(string methodFullName, List<List<IResult>> arguments) {
    var method = FindMethod(methodFullName);
    var test = new TestCase(method, arguments);
    var trace = test.Run();
    return trace;
  }

  public static Method FindMethod(string methodFullName) {
    var l = methodFullName.Split(".");
    return (Method)Find(FixConfiguration.GetProgram().DefaultModule, l);
  }

  public static Function FindFunction(string methodFullName) {
    var l = methodFullName.Split(".");
    return (Function)Find(FixConfiguration.GetProgram().DefaultModule, l);
  }

  public async Task CheckProgram(Program program) {
    FixConfiguration.Instance.Program = program;

    Stopwatch total;
    total = Stopwatch.StartNew();

    Stopwatch sw = null;
    var argumentList = new List<IResult>();
    /*var r = new Random();
    for (var i = 0; i < 50; ++i) {
      var length = r.Next(0, 5);
      var a = new SequenceResult(new List<IResult>());
      for (var j = 0; j < length; j++) {
        a = a.Append(new IntegerResult(r.Next(0, 3)));
      }

      argumentList.Add(a);
    }*/
    
    for (var i = -5; i < 10; i++) {
      argumentList.Add(new IntegerResult(i));
    }

    var arguments = (from a in argumentList from b in argumentList select new List<IResult> { a, b }).ToList();
    const string outerName = "rem";
    const string faultyName = "divRem";

    if (FixConfiguration.ShouldDebug(DebugInformation.ACTION_RUNTIMES)) {
      sw = Stopwatch.StartNew();
    }

    var trace = TestMethod(outerName, arguments);
    await using (var writer = new StreamWriter("../test/.trace.py")) {
      // TODO: write it in a better way
      await writer.WriteAsync(trace.ToDaikonInput());
    }

    if (FixConfiguration.ShouldDebug(DebugInformation.ACTION_RUNTIMES)) {
      FixConfiguration.AddTime(RuntimeActionType.RUN_TESTS, sw.ElapsedMilliseconds);
      sw.Stop();
    }

    if (FixConfiguration.ShouldDebug(DebugInformation.EXECUTION_COUNTS)) {
      Console.WriteLine($"T# {arguments.Count}");
    }

    var outerMethod = FindMethod(outerName);
    var faultyMethod = FindMethod(faultyName);
    FixConfiguration.Instance.Goal = new FixGoal(outerMethod, faultyMethod, ContractType.PRE_CONDITION);

    for (var k = 0; k < 1; k++) {
      Console.WriteLine("---------------------------------------------");
      Console.WriteLine("New run");
      Console.WriteLine("---------------------------------------------");
      var fg = new FixGeneration();
      var (phi, w) = await fg.Weakening(trace);

      // System.Environment.Exit(0);

      Console.WriteLine("Strengthening");

      var fixes = await fg.Strengthening(trace, w);

      FixConfiguration.AddTime(RuntimeActionType.ENTIRE_PROGRAM, total.ElapsedMilliseconds);
      total.Stop();
    }

    Console.WriteLine(FixConfiguration.Instance.logger);
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