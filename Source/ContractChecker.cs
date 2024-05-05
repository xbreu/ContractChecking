using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DafnyDriver.ContractChecking.Fixes;
using Microsoft.Dafny.ContractChecking.Fixes;

namespace Microsoft.Dafny.ContractChecking;

public class ContractChecker {
  private readonly string programFile;

  public ContractChecker(string programFile) {
    this.programFile = programFile;
    FixConfiguration.CreateInstance(null, null);
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

  public async Task<string> CheckProgram(Program program) {
    Stopwatch total;
    total = Stopwatch.StartNew();

    FixConfiguration.Instance.Program = program;

    Stopwatch sw;
    var argumentList = new List<IResult>();
    for (var i = -10; i < 10; i++) {
      argumentList.Add(new IntegerResult(i));
    }

    var arguments = (from a in argumentList from b in argumentList select new List<IResult> { a, b }).ToList();
    const string name = "Enter";

    sw = Stopwatch.StartNew();
    var trace = TestMethod(name, arguments);
    var log = "Writing\n";
    log += trace.ToDaikonInput();
    log += "\n";
    await using (var writer = new StreamWriter("/plugin/trace.py")) {
      // TODO: write it in a better way
      await writer.WriteAsync(trace.ToDaikonInput());
    }
    log += "Written\n";
    Console.WriteLine($"{sw.ElapsedMilliseconds,7:D} ms to run {20} tests in Python");
    sw.Stop();

    sw = Stopwatch.StartNew();
    var outerMethod = FindMethod("Enter");
    var faultyMethod = FindMethod("Faulty");
    FixConfiguration.Instance.Goal = new FixGoal(outerMethod, faultyMethod, ContractType.PRE_CONDITION);
    Console.WriteLine($"\t{sw.ElapsedMilliseconds,6:D} ms to search for method {name} in the program");
    sw.Stop();

    var fg = new FixGeneration();

    sw = Stopwatch.StartNew();
    var (phi, w) = await fg.Weakening(trace);
    Console.WriteLine($"\t{sw.ElapsedMilliseconds,6:D} ms to generate weakening fixes");
    sw.Stop();

    sw = Stopwatch.StartNew();
    var fixes = await fg.Strengthening(trace, w);
    Console.WriteLine($"\t{sw.ElapsedMilliseconds,6:D} ms to generate strengthening fixes");
    sw.Stop();

    PythonExecutor.Shutdown();
    Console.WriteLine($"{total.ElapsedMilliseconds,7:D} ms total runtime");
    total.Stop();

    return log;
  }
}
