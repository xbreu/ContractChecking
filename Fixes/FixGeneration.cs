using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Dafny.ContractChecking.Fixes;

public class FixGeneration {
  private readonly DafnyOptions options;

  public FixGeneration(DafnyOptions options) {
    this.options = options;
  }

  public async void Weakening(IEnumerable<IEnumerable<IResult>> testsArguments, SequenceResult trace,
    Program program) {
    // Determine invariants of passing and failing test cases
    var iP = new List<(Method, bool, Expression)>();
    var iF = new List<(Method, bool, Expression)>();

    Dictionary<Method, (Expression, Expression)> contrs = new();

    var (_, invariants) = await GetInvariants(trace, program, null, null, null);
    foreach (var (methodName, _, e) in invariants) {
      if (e is BinaryExpr { Op: BinaryExpr.Opcode.Iff } be) {
        var m = ContractChecker.FindMethod(program, methodName);
        if (!contrs.ContainsKey(m)) {
          Expression req = new LiteralExpr(null, true);
          Expression ens = new LiteralExpr(null, true);
          req = m.Req.Aggregate(req, (current, en) => new BinaryExpr(null, BinaryExpr.Opcode.And, current, en.E));
          ens = m.Ens.Aggregate(ens, (current, en) => new BinaryExpr(null, BinaryExpr.Opcode.And, current, en.E));
          contrs.Add(m, (req, ens));
        }

        if (be.E0 is BinaryExpr { Op : BinaryExpr.Opcode.Eq, E0: IdentifierExpr { Name: "post_condition" } } eq0) {
          Expression.IsBoolLiteral(eq0.E1, out var val);
          (val ? iP : iF).Add((m, false, be.E1));
        }

        if (be.E0 is BinaryExpr { Op : BinaryExpr.Opcode.Neq, E0: IdentifierExpr { Name: "post_condition" } } neq0) {
          Expression.IsBoolLiteral(neq0.E1, out var val);
          (val ? iF : iP).Add((m, false, be.E1));
        }

        if (be.E1 is BinaryExpr { Op : BinaryExpr.Opcode.Eq, E0: IdentifierExpr { Name: "post_condition" } } eq1) {
          Expression.IsBoolLiteral(eq1.E1, out var val);
          (val ? iP : iF).Add((m, false, be.E0));
        }

        if (be.E1 is BinaryExpr { Op : BinaryExpr.Opcode.Neq, E0: IdentifierExpr { Name: "post_condition" } } neq12) {
          Expression.IsBoolLiteral(neq12.E1, out var val);
          (val ? iF : iP).Add((m, false, be.E0));
        }

        if (be.E0 is BinaryExpr { Op : BinaryExpr.Opcode.Eq, E0: IdentifierExpr { Name: "pre_condition" } } eq02) {
          Expression.IsBoolLiteral(eq02.E1, out var val);
          (val ? iP : iF).Add((m, true, be.E1));
        }

        if (be.E0 is BinaryExpr { Op : BinaryExpr.Opcode.Neq, E0: IdentifierExpr { Name: "pre_condition" } } neq02) {
          Expression.IsBoolLiteral(neq02.E1, out var val);
          (val ? iF : iP).Add((m, true, be.E1));
        }

        if (be.E1 is BinaryExpr { Op : BinaryExpr.Opcode.Eq, E0: IdentifierExpr { Name: "pre_condition" } } eq12) {
          Expression.IsBoolLiteral(eq12.E1, out var val);
          (val ? iP : iF).Add((m, true, be.E0));
        }

        if (be.E1 is BinaryExpr { Op : BinaryExpr.Opcode.Neq, E0: IdentifierExpr { Name: "pre_condition" } } neq1) {
          Expression.IsBoolLiteral(neq1.E1, out var val);
          (val ? iF : iP).Add((m, true, be.E0));
        }
      }
    }

    Console.WriteLine("\nPassing tests have the following invariants:");
    foreach (var (m, enter, e) in iP) {
      Console.Write(enter ? "pre-condition " : "post-condition ");
      Console.Write($"of {m.Name}:");
      Console.WriteLine(e);
    }

    Console.WriteLine("\nFailing tests have the following invariants:");
    foreach (var (m, enter, e) in iF) {
      Console.Write(enter ? "pre-condition " : "post-condition ");
      Console.Write($"of {m.Name}:");
      Console.WriteLine(e);
    }

    // Set of weakening assertions
    var o = iP;
    foreach (var key in contrs.Keys) {
      o.Add((key, true, new LiteralExpr(null, false)));
    }

    // Set of weakening fixes
    var fs = new List<(Method, Expression, Expression)>();
    foreach (var (m, enter, omega) in o) {
      contrs.TryGetValue(m, out var res);
      // TODO: verify where to put the value
      var realEnter = true;
      var (req, ens) = res;
      var r = realEnter ? new BinaryExpr(null, BinaryExpr.Opcode.Or, req, omega) : req;
      var e = realEnter ? ens : new BinaryExpr(null, BinaryExpr.Opcode.Or, ens, omega);
      fs.Add((m, r, e));
    }

    var phi = new List<(Method, Expression, Expression)>();
    var w = new List<(Method, Expression, Expression)>();

    Console.WriteLine("\nValid pure weakening fixes:\n");

    foreach (var (m, r, e) in fs) {
      Console.WriteLine($"Method {m.Name}");
      Console.WriteLine($"\trequires {r}");
      Console.WriteLine($"\tensures {e}");
      
      var (passed, _) = await GetInvariants(trace, program, r, e, m);
      (passed ? phi : w).Add((m, r, e));
      
      Console.WriteLine();
    }
  }

  public (bool, string) GenerateDaikonInput(SequenceResult trace, Program program, Expression req, Expression ens,
    Method outM) {
    var input = new DaikonTrace();
    var passedAll = true;

    // Trace
    var passed = new List<string>();
    foreach (var val in trace.Value) {
      var name = (StringResult)((SequenceResult)val).At(0);
      var nonce = ((IntegerResult)((SequenceResult)val).At(1)).Value.ToString();
      var args = ((SequenceResult)val).At(2);
      var m = ContractChecker.FindMethod(program, name.Value.Trim('\''));
      bool passedM;
      if (m == outM) {
        passedM = input.AddPoint(m, nonce, (SequenceResult)args, !passed.Contains(nonce), program, options, req, ens);
      } else {
        passedM = input.AddPoint(m, nonce, (SequenceResult)args, !passed.Contains(nonce), program, options);
      }

      passedAll = passedAll && passedM;

      if (!passed.Contains(nonce)) {
        passed.Add(nonce);
      } else {
        passed.Remove(nonce);
      }
    }

    return (passedAll, input.ToString());
  }

  private async Task<(bool, List<(string, bool, Expression)>)> GetInvariants(SequenceResult trace, Program program,
    Expression req,
    Expression ens, Method outM) {
    var (passed, daikonInput) = GenerateDaikonInput(trace, program, req, ens, outM);
    await using (var writer = new StreamWriter("../test/.trace.dtrace")) {
      await writer.WriteAsync(daikonInput);
    }

    await using (var writer = new StreamWriter("../test/.trace.spinfo")) {
      await writer.WriteAsync(@"PPT_NAME module_
pre_condition
  SIMPLIFY_FORMAT (EQ |pre_condition| |@true|)
!pre_condition
  SIMPLIFY_FORMAT (EQ |pre_condition| |@false|)
post_condition
  SIMPLIFY_FORMAT (EQ |post_condition| |@true|)
!post_condition
  SIMPLIFY_FORMAT (EQ |post_condition| |@false|)");
    }

    await using (var writer = new StreamWriter("../test/.trace.config")) {
      await writer.WriteAsync(@"daikon.inv.Invariant.confidence_limit = 0
daikon.split.PptSplitter.dummy_invariant_level = 1
daikon.PrintInvariants.print_all = true");
    }

    ProcessStartInfo startInfo = new() {
      FileName = "java",
      Arguments =
        "-cp $DAIKONDIR/daikon.jar daikon.Daikon ../test/.trace.dtrace ../test/.trace.spinfo --format Simplify --config ../test/.trace.config",
      CreateNoWindow = true,
      RedirectStandardOutput = true,
      RedirectStandardError = true
    };
    var proc = Process.Start(startInfo);
    ArgumentNullException.ThrowIfNull(proc);
    var daikonOutput = await proc.StandardOutput.ReadToEndAsync();
    daikonOutput = @"Daikon version 5.8.17, released November 9, 2022; http://plse.cs.washington.edu/daikon.
(read 1 spinfo file, 4 splitters)
Processing trace data; reading 1 dtrace file:
[2023-06-23T12:24:52.744516788]: Reading ../test/.trace.dtrace (line 34, 3.29%)module_: 4 of 4 splitters successful
[2023-06-23T12:24:53.128443397]: Finished reading ../test/.trace.dtrace
===========================================================================
module_.default__.duplicate():::ENTER
(EQ |a| |a|)
(EQ |pre_condition| |pre_condition|)
(<= |a| 10)
(>= |a| -10)
(EQ 0 (+ |a| (* -1 |a|)))
===========================================================================
module_.default__.duplicate():::EXIT
(EQ |a| |a|)
(EQ |a| |__orig__a|)
(EQ |pre_condition| |pre_condition|)
(EQ |pre_condition| |__orig__pre_condition|)
(EQ |post_condition| |post_condition|)
(IFF (<= |a| -1) (EQ |pre_condition| |@false|))
(IMPLIES (<= |a| -1) (NEQ |a| 0))
(IFF (>= |a| 0) (NEQ |pre_condition| 0))
(IFF (>= |a| 0) (EQ |pre_condition| |@true|))
(IMPLIES (>= |a| 0) (EQ |pre_condition| |post_condition|))
(IMPLIES (NOT (EQ |post_condition| |@false|)) (NEQ |post_condition| 0))
(IMPLIES (NOT (EQ |post_condition| |@false|)) (EQ |post_condition| |@true|))
(IMPLIES (EQ |post_condition| |@true|) (NEQ |post_condition| 0))
(IMPLIES (EQ |post_condition| |@true|) (EQ |post_condition| |@true|))
(<= |a| 10)
(>= |a| -10)
(EQ |post_condition| |@true|)
(NEQ |post_condition| 0)
(EQ 0 (+ |a| (* -1 |a|)))
===========================================================================
module_.default__.make():::ENTER
(EQ |b| |b|)
(EQ |pre_condition| |pre_condition|)
(IFF (<= |b| 0) (EQ |pre_condition| |@false|))
(IFF (>= |b| 1) (NEQ |pre_condition| 0))
(IFF (>= |b| 1) (EQ |pre_condition| |@true|))
(IMPLIES (>= |b| 1) (NEQ |b| 0))
(<= |b| 10)
(>= |b| -10)
(EQ 0 (+ |b| (* -1 |b|)))
===========================================================================
module_.default__.make():::EXIT
(EQ |b| |b|)
(EQ |b| |__orig__b|)
(EQ |pre_condition| |pre_condition|)
(EQ |pre_condition| |post_condition|)
(EQ |pre_condition| |__orig__pre_condition|)
(EQ |post_condition| |post_condition|)
(IFF (<= |b| -1) (EQ |post_condition| |@false|))
(IMPLIES (<= |b| -1) (NEQ |b| 0))
(IMPLIES (<= |b| -1) (EQ |pre_condition| |@false|))
(IMPLIES (<= |b| -1) (EQ |pre_condition| |post_condition|))
(IFF (<= |b| 0) (EQ |pre_condition| |@false|))
(IFF (>= |b| 0) (NEQ |post_condition| 0))
(IFF (>= |b| 0) (EQ |post_condition| |@true|))
(IFF (>= |b| 1) (NEQ |pre_condition| 0))
(IFF (>= |b| 1) (EQ |pre_condition| |@true|))
(IMPLIES (>= |b| 1) (NEQ |b| 0))
(IMPLIES (>= |b| 1) (NEQ |post_condition| 0))
(IMPLIES (>= |b| 1) (EQ |post_condition| |@true|))
(<= |b| 10)
(>= |b| -10)
(EQ 0 (+ |b| (* -1 |b|)))
Exiting Daikon.";
    await using (var writer = new StreamWriter("../test/.trace.invariants")) {
      await writer.WriteAsync(daikonOutput);
    }

    return (passed, InvariantParser.ParseDaikonOutput(daikonOutput));
  }
}