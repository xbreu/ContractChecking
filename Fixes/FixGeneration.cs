using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DafnyDriver.ContractChecking.Fixes;

namespace Microsoft.Dafny.ContractChecking.Fixes;

public class FixGeneration {
  private RuntimePhase currentPhase = RuntimePhase.WEAKENING;
  private Stopwatch sw;

  private List<(Method, ContractType, Expression)> PassingFailingInvariants(
    List<(string, ContractType, Expression)> invariants, bool strengthening = false) {
    var iP = new List<(Method, ContractType, Expression)>();
    var iF = new List<(Method, ContractType, Expression)>();
    var passingStrings = new HashSet<string> { "post_condition" };
    if (strengthening) {
      passingStrings.Add("passed_all_inside");
    }

    foreach (var (methodName, _, e) in invariants) {
      if (e is BinaryExpr { Op : BinaryExpr.Opcode.Eq, E0: IdentifierExpr lhs, E1: IdentifierExpr rhs }) {
        var m = ContractChecker.FindMethod(methodName);
        if ((Expression.IsBoolLiteral(rhs, out var _) && passingStrings.Contains(lhs.Name)) ||
            lhs.Name == "pre_condition") {
          iP.Add((m, strengthening ? ContractType.PRE_CONDITION : ContractType.POST_CONDITION,
            new LiteralExpr(null, true)));
        }
      } else if (e is BinaryExpr { Op: BinaryExpr.Opcode.Iff } be) {
        var m = ContractChecker.FindMethod(methodName);

        if (be.E0 is BinaryExpr { Op : BinaryExpr.Opcode.Eq, E0: IdentifierExpr e00 } eq0 &&
            passingStrings.Contains(e00.Name)) {
          Expression.IsBoolLiteral(eq0.E1, out var val);
          (val ? iP : iF).Add((m, strengthening ? ContractType.PRE_CONDITION : ContractType.POST_CONDITION, be.E1));
        }

        if (be.E0 is BinaryExpr { Op : BinaryExpr.Opcode.Neq, E0: IdentifierExpr e01 } neq0 &&
            passingStrings.Contains(e01.Name)) {
          Expression.IsBoolLiteral(neq0.E1, out var val);
          (val ? iF : iP).Add((m, strengthening ? ContractType.PRE_CONDITION : ContractType.POST_CONDITION, be.E1));
        }

        if (be.E1 is BinaryExpr { Op : BinaryExpr.Opcode.Eq, E0: IdentifierExpr e02 } eq1 &&
            passingStrings.Contains(e02.Name)) {
          Expression.IsBoolLiteral(eq1.E1, out var val);
          (val ? iP : iF).Add((m, strengthening ? ContractType.PRE_CONDITION : ContractType.POST_CONDITION, be.E0));
        }

        if (be.E1 is BinaryExpr { Op : BinaryExpr.Opcode.Neq, E0: IdentifierExpr e03 } neq12 &&
            passingStrings.Contains(e03.Name)) {
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
    currentPhase = RuntimePhase.WEAKENING;

    // Determine invariants of passing and failing test cases
    var contractManager = new ContractManager();
    contractManager.Relax(FixConfiguration.GetGoal());
    var (_, invariants, _) = await GetInvariants(trace, contractManager);
    sw = Stopwatch.StartNew();

    var iP = PassingFailingInvariants(invariants);
    // Remove invariants that use outputs on the pre-condition and invariants for other methods
    iP = iP.Where(x => x.Item1 == FixConfiguration.GetFaultyRoutine())
      .ToList();
    if (FixConfiguration.GetBrokenContract() == ContractType.PRE_CONDITION) {
      iP = iP.Where(m =>
          !ContainsVariable(m.Item3, m.Item1.Outs.Select(x => x.Name)))
        .ToList();
    }

    if (FixConfiguration.ShouldDebug(DebugInformation.EXECUTION_COUNTS)) {
      Console.WriteLine($"R# {iP.Count}");
    }

    if (FixConfiguration.ShouldDebug(DebugInformation.ACTION_RUNTIMES)) {
      FixConfiguration.AddTime(RuntimeActionType.FILTER_WEAKENING_INVARIANTS, sw.ElapsedMilliseconds);
      sw.Stop();
    }

    if (FixConfiguration.ShouldDebug(DebugInformation.INVARIANTS)) {
      foreach (var (m, enter, e) in iP) {
        Console.Write(enter);
        Console.Write($" of {m.Name} : ");
        Console.WriteLine(e);
      }
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
      FixConfiguration.AddTime(RuntimeActionType.CREATE_WEAKENING_FIXES, sw.ElapsedMilliseconds);
      sw.Stop();
    }

    if (FixConfiguration.ShouldDebug(DebugInformation.ACTION_RUNTIMES)) {
      sw = Stopwatch.StartNew();
    }

    var allValidations = fs.ToLookup(manager => ValidateFix(trace, manager));
    var phi = allValidations[true].ToList();
    var w = allValidations[false].ToList();
    if (FixConfiguration.ShouldDebug(DebugInformation.ACTION_RUNTIMES)) {
      FixConfiguration.AddTime(RuntimeActionType.VALIDATE_WEAKENING_FIXES, sw.ElapsedMilliseconds);
      sw.Stop();
    }

    if (FixConfiguration.ShouldDebug(DebugInformation.EXECUTION_COUNTS)) {
      Console.WriteLine($"W# {phi.Count}");
    }

    if (FixConfiguration.ShouldDebug(DebugInformation.FIXES)) {
      foreach (var newContracts in phi) {
        Console.WriteLine(newContracts);
      }
    }

    return (phi, w);
  }

  private bool ContainsVariable(Expression expression, IEnumerable<string> variables) {
    return expression switch {
      IdentifierExpr identifierExpr => variables.Contains(identifierExpr.Name),
      _ => expression.SubExpressions.Any(e => ContainsVariable(e, variables))
    };
  }

  private static (bool, string, HashSet<Method>) GenerateDaikonInput(SequenceResult trace,
    ContractManager contractManager, bool ignoreString = false) {
    var input = new DaikonTrace(ignoreString);
    var passedAll = true;

    // Trace
    var processedEnterPoint = new HashSet<string>();
    var currentInvalid = false;
    var passedBeforeFaulty = new HashSet<Method>();
    var passedByFaulty = false;
    var currentOuterNonce = "";
    var passedAllInside = new Dictionary<string, bool>();
    foreach (var val in trace.Value) {
      var name = (StringResult)((SequenceResult)val).At(0);
      var nonce = ((IntegerResult)((SequenceResult)val).At(1)).Value.ToString();

      DaikonTrace.TestResult testResult;
      if (!ignoreString || !currentInvalid) {
        var parameters = (SequenceResult)((SequenceResult)val).At(2);
        var method = ContractChecker.FindMethod(name.Value.Trim('\''));
        var place = processedEnterPoint.Contains(nonce) ? ContractType.POST_CONDITION : ContractType.PRE_CONDITION;
        testResult = input.AddPoint(method, nonce, parameters, place, contractManager);

        if (!passedByFaulty) {
          if (method == FixConfiguration.GetFaultyRoutine()) {
            passedByFaulty = true;
          } else {
            passedBeforeFaulty.Add(method);
          }
        }

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
      } else {
        if (currentOuterNonce == nonce) {
          currentInvalid = false;
        }

        continue;
      }

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

    return (passedAll, input.ToString(), passedBeforeFaulty);
  }

  private static bool ValidateFix(SequenceResult trace, ContractManager contractManager) {
    var (passed, _, _) = GenerateDaikonInput(trace, contractManager, true);
    return passed;
  }

  private async
    Task<(bool passed, List<(string, ContractType, Expression)> parsedOutput, HashSet<Method> previousRoutines)>
    GetInvariants(
      SequenceResult trace,
      ContractManager contractManager) {
    if (FixConfiguration.ShouldDebug(DebugInformation.ACTION_RUNTIMES)) {
      sw = Stopwatch.StartNew();
    }

    var (passed, daikonInput, previousRoutines) = GenerateDaikonInput(trace, contractManager);

    // Write Daikon configuration
    await DaikonConfiguration.WriteFiles();

    // Write Daikon input
    await using (var writer = new StreamWriter("../test/.trace.dtrace")) {
      await writer.WriteAsync(daikonInput);
    }

    if (FixConfiguration.ShouldDebug(DebugInformation.ACTION_RUNTIMES)) {
      FixConfiguration.AddTime(currentPhase, RuntimePhaseActionType.GENERATE_DAIKON_INPUT, sw.ElapsedMilliseconds);
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
      FixConfiguration.AddTime(currentPhase, RuntimePhaseActionType.RUN_DAIKON, sw.ElapsedMilliseconds);
      sw.Stop();

      sw = Stopwatch.StartNew();
    }

    await using (var writer = new StreamWriter("../test/.trace.invariants")) {
      await writer.WriteAsync(daikonOutput);
    }

    var parsedOutput = InvariantParser.ParseDaikonOutput(daikonOutput);
    if (FixConfiguration.ShouldDebug(DebugInformation.ACTION_RUNTIMES)) {
      FixConfiguration.AddTime(currentPhase, RuntimePhaseActionType.PARSE_DAIKON, sw.ElapsedMilliseconds);
      sw.Stop();
    }

    return (passed, parsedOutput, previousRoutines);
  }

  public async Task<List<ContractManager>> Strengthening(SequenceResult trace, List<ContractManager> w) {
    currentPhase = RuntimePhase.STRENGTHENING;
    var phi = new List<ContractManager>();

    if (FixConfiguration.ShouldDebug(DebugInformation.EXECUTION_COUNTS)) {
      Console.WriteLine($"Rx {w.Count}");
    }

    foreach (var f in w) {
      var (_, invariants, passedBeforeFaulty) = await GetInvariants(trace, f);
      if (FixConfiguration.ShouldDebug(DebugInformation.ACTION_RUNTIMES)) {
        sw = Stopwatch.StartNew();
      }

      if (FixConfiguration.GetFaultyRoutine() == FixConfiguration.GetOuter()) {
        passedBeforeFaulty.Add(FixConfiguration.GetOuter());
      }

      var iP = PassingFailingInvariants(invariants, true);
      iP = iP.Where(m =>
          passedBeforeFaulty.Contains(m.Item1) &&
          !ContainsVariable(m.Item3, m.Item1.Outs.Select(x => x.Name)))
        .ToList();
      if (FixConfiguration.ShouldDebug(DebugInformation.ACTION_RUNTIMES)) {
        FixConfiguration.AddTime(RuntimeActionType.FILTER_STRENGTHENING_INVARIANTS, sw.ElapsedMilliseconds);
        sw.Stop();

        sw = Stopwatch.StartNew();
      }

      if (FixConfiguration.ShouldDebug(DebugInformation.EXECUTION_COUNTS)) {
        Console.WriteLine($"R# {iP.Count}");
      }

      if (FixConfiguration.ShouldDebug(DebugInformation.INVARIANTS)) {
        foreach (var inv in iP) {
          Console.WriteLine(inv);
        }
      }

      // Catalog the invariants per method
      var invariantsPerMethod = new Dictionary<Method, List<Expression>>();
      foreach (var (method, _, invariant) in iP) {
        if (!invariantsPerMethod.ContainsKey(method)) {
          invariantsPerMethod.Add(method, new List<Expression>());
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

      if (FixConfiguration.ShouldDebug(DebugInformation.EXECUTION_COUNTS)) {
        Console.WriteLine($"C# {allCombinations.Count}");
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
        FixConfiguration.AddTime(RuntimeActionType.CREATE_STRENGTHENING_FIXES, sw.ElapsedMilliseconds);
        sw.Stop();
      }
    }

    if (FixConfiguration.ShouldDebug(DebugInformation.ACTION_RUNTIMES)) {
      sw = Stopwatch.StartNew();
    }

    var passedCandidates = new List<ContractManager>();
    foreach (var candidate in phi) {
      var passed = ValidateFix(trace, candidate);
      if (passed) {
        passedCandidates.Add(candidate);
      }
    }

    if (FixConfiguration.ShouldDebug(DebugInformation.EXECUTION_COUNTS)) {
      Console.WriteLine($"S# {passedCandidates.Count}");
    }

    if (FixConfiguration.ShouldDebug(DebugInformation.FIXES)) {
      foreach (var validated in passedCandidates) {
        Console.WriteLine(validated);
        Console.WriteLine();
      }
    }

    if (FixConfiguration.ShouldDebug(DebugInformation.ACTION_RUNTIMES)) {
      FixConfiguration.AddTime(RuntimeActionType.VALIDATE_STRENGTHENING_FIXES, sw.ElapsedMilliseconds);

      sw.Stop();
    }

    return passedCandidates;
  }
}