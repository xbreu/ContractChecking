using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DafnyDriver.ContractChecking.Fixes;

namespace Microsoft.Dafny.ContractChecking.Fixes;

public class FixGeneration {
  private Stopwatch sw;

  public List<(Method, ContractType, Expression)> PassingFailingInvariants(
    List<(string, ContractType, Expression)> invariants, bool strengthening = false) {
    var iP = new List<(Method, ContractType, Expression)>();
    var iF = new List<(Method, ContractType, Expression)>();
    var passingString = strengthening ? "passed_all_inside" : "post_condition";

    foreach (var (methodName, enter, e) in invariants) {
      if (e is BinaryExpr { Op: BinaryExpr.Opcode.Iff } be) {
        var m = ContractChecker.FindMethod(methodName);

        if (be.E0 is BinaryExpr { Op : BinaryExpr.Opcode.Eq, E0: IdentifierExpr e00 } eq0 &&
            e00.Name == passingString) {
          Expression.IsBoolLiteral(eq0.E1, out var val);
          (val ? iP : iF).Add((m, strengthening ? ContractType.PRE_CONDITION : ContractType.POST_CONDITION, be.E1));
        }

        if (be.E0 is BinaryExpr { Op : BinaryExpr.Opcode.Neq, E0: IdentifierExpr e01 } neq0 &&
            e01.Name == passingString) {
          Expression.IsBoolLiteral(neq0.E1, out var val);
          (val ? iF : iP).Add((m, strengthening ? ContractType.PRE_CONDITION : ContractType.POST_CONDITION, be.E1));
        }

        if (be.E1 is BinaryExpr { Op : BinaryExpr.Opcode.Eq, E0: IdentifierExpr e02 } eq1 &&
            e02.Name == passingString) {
          Expression.IsBoolLiteral(eq1.E1, out var val);
          (val ? iP : iF).Add((m, strengthening ? ContractType.PRE_CONDITION : ContractType.POST_CONDITION, be.E0));
        }

        if (be.E1 is BinaryExpr { Op : BinaryExpr.Opcode.Neq, E0: IdentifierExpr e03 } neq12 &&
            e03.Name == passingString) {
          Expression.IsBoolLiteral(neq12.E1, out var val);
          (val ? iF : iP).Add((m, strengthening ? ContractType.PRE_CONDITION : ContractType.POST_CONDITION, be.E0));
        }

        if (be.E0 is BinaryExpr { Op : BinaryExpr.Opcode.Eq, E0: IdentifierExpr { Name: "pre_condition" } } eq02) {
          Expression.IsBoolLiteral(eq02.E1, out var val);
          (val ? iP : iF).Add((m, ContractType.PRE_CONDITION, be.E1));
        }

        if (be.E0 is BinaryExpr { Op : BinaryExpr.Opcode.Neq, E0: IdentifierExpr { Name: "pre_condition" } } neq02) {
          Expression.IsBoolLiteral(neq02.E1, out var val);
          (val ? iF : iP).Add((m, ContractType.PRE_CONDITION, be.E1));
        }

        if (be.E1 is BinaryExpr { Op : BinaryExpr.Opcode.Eq, E0: IdentifierExpr { Name: "pre_condition" } } eq12) {
          Expression.IsBoolLiteral(eq12.E1, out var val);
          (val ? iP : iF).Add((m, ContractType.PRE_CONDITION, be.E0));
        }

        if (be.E1 is BinaryExpr { Op : BinaryExpr.Opcode.Neq, E0: IdentifierExpr { Name: "pre_condition" } } neq1) {
          Expression.IsBoolLiteral(neq1.E1, out var val);
          (val ? iF : iP).Add((m, ContractType.PRE_CONDITION, be.E0));
        }
      }
    }

    iP = iP.Concat(
        iF.Select(x => (x.Item1, x.Item2, (Expression)new UnaryOpExpr(null, UnaryOpExpr.Opcode.Not, x.Item3))))
      .ToList();
    iP = iP
      .Where(m =>
        !ContainsVariable(m.Item3, new List<string> { "pre_condition", "post_condition", "passed_all_inside" }))
      .DistinctBy(x => x.Item1 + x.Item2.ToString() + x.Item3)
      .ToList();
    return iP;
  }

  public async Task<(List<ContractManager>, List<ContractManager>)> Weakening(SequenceResult trace) {
    // Determine invariants of passing and failing test cases
    sw = Stopwatch.StartNew();
    var contractManager = new ContractManager();
    contractManager.Relax(FixConfiguration.GetGoal());
    var (_, invariants) = await GetInvariants(trace, contractManager);

    if (FixConfiguration.ShouldDebug(DebugInformation.ACTION_RUNTIMES)) {
      Console.WriteLine($"\t\t{sw.ElapsedMilliseconds,6:D} ms to get invariants from Daikon");
      sw.Stop();
      sw = Stopwatch.StartNew();
    }

    var iP = PassingFailingInvariants(invariants);
    // Remove invariants that use outputs on the pre-condition and invariants for other methods
    iP = iP.Where(x => x.Item1 == FixConfiguration.GetFaultyRoutine())
      .ToList();
    if (FixConfiguration.GetBrokenContract() == ContractType.PRE_CONDITION) {
      iP = iP.Where(m =>
          !ContainsVariable(m.Item3, m.Item1.Outs.Select(x => x.Name)))
        .ToList();
    }

    if (FixConfiguration.ShouldDebug(DebugInformation.ACTION_RUNTIMES)) {
      Console.WriteLine($"\t\t{sw.ElapsedMilliseconds,6:D} ms to filter invariants");
      sw.Stop();
    }

    Console.WriteLine("\nPassing tests have the following invariants:");
    foreach (var (m, enter, e) in iP) {
      Console.Write(enter);
      Console.Write($" of {m.Name} : ");
      Console.WriteLine(e);
    }

    sw = Stopwatch.StartNew();
    // Set of weakening assertions
    var o = iP.Select(x => (x.Item2, x.Item3)).ToList();
    o.Add((FixConfiguration.GetBrokenContract(), new LiteralExpr(null, false)));

    // Set of weakening fixes
    List<ContractManager> fs = new();
    foreach (var (_, omega) in o) {
      var manager = new ContractManager();
      manager.Weaken(FixConfiguration.GetFaultyRoutine(), FixConfiguration.GetBrokenContract(), omega);
      fs.Add(manager);
    }

    if (FixConfiguration.ShouldDebug(DebugInformation.ACTION_RUNTIMES)) {
      Console.WriteLine($"\t\t{sw.ElapsedMilliseconds,6:D} ms to create candidate weakening fixes");
      sw.Stop();
    }

    Console.WriteLine("\nValid pure weakening fixes:\n");

    if (FixConfiguration.ShouldDebug(DebugInformation.ACTION_RUNTIMES)) {
      sw = Stopwatch.StartNew();
    }

    var allValidations = fs.ToLookup(manager => ValidateFix(trace, manager));
    var phi = allValidations[true].ToList();
    var w = allValidations[false].ToList();
    if (FixConfiguration.ShouldDebug(DebugInformation.ACTION_RUNTIMES)) {
      Console.WriteLine($"\t\t{sw.ElapsedMilliseconds,6:D} ms to validate those candidates");
      sw.Stop();
    }

    foreach (var newContracts in phi) {
      Console.WriteLine(newContracts);
    }

    return (phi, w);
  }

  private bool ContainsVariable(Expression expression, IEnumerable<string> variables) {
    return expression switch {
      IdentifierExpr identifierExpr => variables.Contains(identifierExpr.Name),
      _ => expression.SubExpressions.Any(e => ContainsVariable(e, variables))
    };
  }

  public (bool, string) GenerateDaikonInput(SequenceResult trace, ContractManager contractManager) {
    var input = new DaikonTrace();
    var passedAll = true;

    // Trace
    var processedEnterPoint = new List<string>();
    var currentInvalid = false;
    var currentOuterNonce = "";
    var passedAllInside = new Dictionary<string, bool>();
    foreach (var val in trace.Value) {
      var name = (StringResult)((SequenceResult)val).At(0);
      var nonce = ((IntegerResult)((SequenceResult)val).At(1)).Value.ToString();
      var parameters = (SequenceResult)((SequenceResult)val).At(2);
      var method = ContractChecker.FindMethod(name.Value.Trim('\''));
      var place = processedEnterPoint.Contains(nonce) ? ContractType.POST_CONDITION : ContractType.PRE_CONDITION;
      var testResult = input.AddPoint(method, nonce, parameters, place, contractManager);

      if (place == ContractType.PRE_CONDITION) {
        processedEnterPoint.Add(nonce);
        passedAllInside[nonce] = testResult == DaikonTrace.TestResult.PASSING;
        if (!passedAllInside[nonce]) {
          foreach (var k in passedAllInside.Keys) {
            passedAllInside[k] = false;
          }
        }
      } else {
        processedEnterPoint.Remove(nonce);
        input.AddContractVariable(passedAllInside[nonce]);
        passedAllInside.Remove(nonce);
      }

      input.Trace.AppendLine();

      if (currentInvalid) {
        if (currentOuterNonce == nonce) {
          currentInvalid = false;
        }

        continue;
      }

      if (testResult == DaikonTrace.TestResult.INVALID) {
        currentInvalid = true;
        currentOuterNonce = nonce;
        continue;
      }

      passedAll = passedAll && testResult == DaikonTrace.TestResult.PASSING;
    }

    return (passedAll, input.ToString());
  }

  public bool ValidateFix(SequenceResult trace, ContractManager contractManager) {
    var (passed, _) = GenerateDaikonInput(trace, contractManager);
    return passed;
  }

  private async Task<(bool passed, List<(string, ContractType, Expression)> parsedOutput)> GetInvariants(
    SequenceResult trace,
    ContractManager contractManager) {
    if (FixConfiguration.ShouldDebug(DebugInformation.ACTION_RUNTIMES)) {
      sw = Stopwatch.StartNew();
    }

    var (passed, daikonInput) = GenerateDaikonInput(trace, contractManager);
    if (FixConfiguration.ShouldDebug(DebugInformation.ACTION_RUNTIMES)) {
      Console.WriteLine($"\t\t\t{sw.ElapsedMilliseconds,6:D} ms to generate Daikon input");
      sw.Stop();

      sw = Stopwatch.StartNew();
    }

    // Write Daikon configuration
    await DaikonConfiguration.WriteFiles();

    // Write Daikon input
    await using (var writer = new StreamWriter("../test/.trace.dtrace")) {
      await writer.WriteAsync(daikonInput);
    }

    if (FixConfiguration.ShouldDebug(DebugInformation.ACTION_RUNTIMES)) {
      Console.WriteLine($"\t\t\t{sw.ElapsedMilliseconds,6:D} ms to write Daikon input files");
      sw.Stop();

      sw = Stopwatch.StartNew();
    }

    ProcessStartInfo startInfo = new() {
      FileName = "java",
      Arguments =
        "-cp /home/xbreu/Applications/daikon/daikon.jar daikon.Daikon ../test/.trace.dtrace ../test/.trace.spinfo --format Simplify --config ../test/.trace.config",
      CreateNoWindow = true,
      RedirectStandardOutput = true,
      RedirectStandardError = true
    };
    var proc = Process.Start(startInfo);
    ArgumentNullException.ThrowIfNull(proc);
    var daikonOutput = proc.StandardOutput.ReadToEnd();
    if (FixConfiguration.ShouldDebug(DebugInformation.ACTION_RUNTIMES)) {
      Console.WriteLine($"\t\t\t{sw.ElapsedMilliseconds,6:D} ms to run and read the output of Daikon");
      sw.Stop();

      sw = Stopwatch.StartNew();
    }

    await using (var writer = new StreamWriter("../test/.trace.invariants")) {
      await writer.WriteAsync(daikonOutput);
    }

    if (FixConfiguration.ShouldDebug(DebugInformation.ACTION_RUNTIMES)) {
      Console.WriteLine($"\t\t\t{sw.ElapsedMilliseconds,6:D} ms to save the Daikon output in a file");
      sw.Stop();

      sw = Stopwatch.StartNew();
    }

    var parsedOutput = InvariantParser.ParseDaikonOutput(daikonOutput);
    if (FixConfiguration.ShouldDebug(DebugInformation.ACTION_RUNTIMES)) {
      Console.WriteLine($"\t\t\t{sw.ElapsedMilliseconds,6:D} ms to parse Daikon output");
      sw.Stop();
    }

    return (passed, parsedOutput);
  }

  public async Task<List<ContractManager>> Strengthening(SequenceResult trace, List<ContractManager> w) {
    var phi = new List<ContractManager>();

    foreach (var f in w) {
      if (FixConfiguration.ShouldDebug(DebugInformation.ACTION_RUNTIMES)) {
        sw = Stopwatch.StartNew();
      }

      var (_, invariants) = await GetInvariants(trace, f);
      if (FixConfiguration.ShouldDebug(DebugInformation.ACTION_RUNTIMES)) {
        Console.WriteLine($"\t\t{sw.ElapsedMilliseconds,6:D} ms to get invariants from Daikon");
        sw.Stop();

        sw = Stopwatch.StartNew();
      }

      var iP = PassingFailingInvariants(invariants, true);
      iP = iP.Where(m => !ContainsVariable(m.Item3, m.Item1.Outs.Select(x => x.Name))).ToList();
      if (FixConfiguration.ShouldDebug(DebugInformation.ACTION_RUNTIMES)) {
        Console.WriteLine($"\t\t{sw.ElapsedMilliseconds,6:D} ms to filter invariants");
        sw.Stop();

        sw = Stopwatch.StartNew();
      }

      // Catalog the invariants per method
      var invariantsPerMethod = new Dictionary<Method, List<Expression>>();
      foreach (var (method, _, invariant) in iP) {
        if (!invariantsPerMethod.ContainsKey(method)) {
          invariantsPerMethod.Add(method, new List<Expression>());
          invariantsPerMethod[method].Add(new LiteralExpr(null, true));
        }

        invariantsPerMethod[method].Add(invariant);
      }

      // Calculate all possible combinations of method contracts
      var methods = invariantsPerMethod.Keys.ToList();
      var allCombinations = new List<List<Expression>> { new() };
      foreach (var method in methods) {
        var sequence = invariantsPerMethod[method];
        var s = sequence;
        var allCombinationsEnum =
          from seq in allCombinations
          from item in s
          select seq.Concat(new List<Expression> { item }).ToList();
        allCombinations = allCombinationsEnum.ToList();
      }

      foreach (var combination in allCombinations) {
        var newContracts = f.Copy();

        var i = 0;
        foreach (var method in methods) {
          newContracts.Strengthen(method, ContractType.PRE_CONDITION, combination[i++]);
        }

        phi.Add(newContracts);
      }

      if (FixConfiguration.ShouldDebug(DebugInformation.ACTION_RUNTIMES)) {
        Console.WriteLine($"\t\t{sw.ElapsedMilliseconds,6:D} ms to create candidate strengthening fixes");

        sw.Stop();
      }
    }

    if (FixConfiguration.ShouldDebug(DebugInformation.ACTION_RUNTIMES)) {
      sw = Stopwatch.StartNew();
    }

    Console.WriteLine("Valid fixes with strengthening:");
    var passedCandidates = new List<ContractManager>();
    foreach (var candidate in phi) {
      var passed = ValidateFix(trace, candidate);
      if (passed) {
        Console.WriteLine("\tFix:");
        Console.WriteLine(candidate);
        passedCandidates.Add(candidate);
      }
    }

    if (FixConfiguration.ShouldDebug(DebugInformation.ACTION_RUNTIMES)) {
      Console.WriteLine($"\t\t{sw.ElapsedMilliseconds,6:D} ms to validate those candidates");

      sw.Stop();
    }

    return passedCandidates;
  }
}