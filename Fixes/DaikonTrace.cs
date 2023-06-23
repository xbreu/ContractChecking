using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Dafny.ContractChecking.Fixes;

public class DaikonTrace {
  private readonly List<string> declared = new();
  private readonly StringBuilder header = new();
  private readonly StringBuilder trace = new();

  public DaikonTrace() {
    // File info
    header.AppendLine("decl-version 2.0");
    header.AppendLine("input-language Dafny");
    header.AppendLine();
  }

  private void AddMethod(Method m) {
    var name = $"{GetModuleName(m)}.{GetClassName(m)}.{m.Name}";
    declared.Add(name);
    foreach (var type in new List<string> { "ENTER", "EXIT00" }) {
      header.AppendLine($"ppt {name}():::{type}");
      if (type == "ENTER") {
        header.AppendLine("ppt-type enter");
      } else {
        header.AppendLine("ppt-type subexit");
      }

      foreach (var f in m.Ins) {
        header.AppendLine($"variable {f.CompileName}");
        header.AppendLine("\tvar-kind variable");
        header.AppendLine($"\tdec-type {f.Type}");
        header.Append("\trep-type ");
        header.AppendLine(f.Type.ToString() switch {
          "int" => "int",
          _ => "hashcode"
        });
        var comp = f.Type.ToString() switch {
          "bool" => 1,
          "int" => 2,
          _ => 20
        };
        header.AppendLine($"\tcomparability {comp}");
      }

      if (type == "EXIT00") {
        foreach (var f in m.Outs) {
          header.AppendLine($"variable {f.CompileName}");
          header.AppendLine("\tvar-kind variable");
          header.AppendLine($"\tdec-type {f.Type}");
          header.Append("\trep-type ");
          header.AppendLine(f.Type.ToString() switch {
            "int" => "int",
            _ => "hashcode"
          });
          var comp = f.Type.ToString() switch {
            "bool" => 1,
            "int" => 2,
            _ => 20
          };
          header.AppendLine($"\tcomparability {comp}");
        }
      }

      if (type == "ENTER") {
        header.AppendLine("variable pre_condition");
        header.AppendLine("\tvar-kind variable");
        header.AppendLine("\tdec-type boolean");
        header.AppendLine("\trep-type boolean");
        header.AppendLine("\tcomparability 1");
      } else {
        header.AppendLine("variable pre_condition");
        header.AppendLine("\tvar-kind variable");
        header.AppendLine("\tdec-type boolean");
        header.AppendLine("\trep-type boolean");
        header.AppendLine("\tcomparability 1");
        header.AppendLine("variable post_condition");
        header.AppendLine("\tvar-kind variable");
        header.AppendLine("\tdec-type boolean");
        header.AppendLine("\trep-type boolean");
        header.AppendLine("\tcomparability 1");
      }
      header.AppendLine();
    }
  }

  public bool AddPoint(Method m, string nonce, SequenceResult args, bool enter, Program program, DafnyOptions opts) {
    // Build two expressions representing the method's contract
    Expression req = new LiteralExpr(null, true);
    Expression ens = new LiteralExpr(null, true);
    req = m.Req.Aggregate(req, (current, e) => new BinaryExpr(null, BinaryExpr.Opcode.And, current, e.E));
    ens = m.Ens.Aggregate(ens, (current, e) => new BinaryExpr(null, BinaryExpr.Opcode.And, current, e.E));
    return AddPoint(m, nonce, args, enter, program, opts, req, ens);
  }

  public bool AddPoint(Method m, string nonce, SequenceResult args, bool enter, Program program, DafnyOptions opts,
    Expression req, Expression ens) {
    var name = $"{GetModuleName(m)}.{GetClassName(m)}.{m.Name}";
    
    if (!declared.Contains(name)) {
      AddMethod(m);
    }

    trace.Append($"{name}():::");
    trace.AppendLine(enter ? "ENTER" : "EXIT00");
    trace.AppendLine("this_invocation_nonce");
    trace.AppendLine(nonce);
    var arguments = m.Ins.Select(x => x.CompileName);
    var returns = m.Outs.Select(x => x.CompileName);
    var i = 0;
    var contextD = new Dictionary<string, IResult>();
    foreach (var arg in arguments) {
      var val = args.At(i++);
      contextD.Add(arg, val);
      trace.AppendLine(arg);
      trace.AppendLine(val.ToDaikonInput());
      trace.AppendLine("0");
    }

    if (!enter) {
      foreach (var arg in returns) {
        var val = args.At(i++);
        contextD.Add(arg, val);
        trace.AppendLine(arg);
        trace.AppendLine(val.ToDaikonInput());
        trace.AppendLine("0");
      }
    }

    var context = new Context(null, contextD);
    var eval = new Evaluator(program, opts);
    var passedPre = (BooleanResult)eval.Evaluate(req, context);
    trace.AppendLine("pre_condition");
    trace.AppendLine(passedPre ? "1" : "0");
    trace.AppendLine("0");

    // TODO: ignore pre-condition?
    var passedAll = new BooleanResult(true);
    
    if (!enter) {
      var passedPost = (BooleanResult)eval.Evaluate(ens, context);
      trace.AppendLine("post_condition");
      trace.AppendLine(passedPost ? "1" : "0");
      trace.AppendLine("0");
      passedAll = passedPre.Imp(passedPost);
      if (!passedAll) {
        Console.Write($"Failed ({ens}) with ");
        foreach (var (n, val) in context.Arguments) {
          Console.Write($"{n} = {val} ");
        }
        Console.WriteLine();
      }
    }
    
    trace.AppendLine();
    return passedAll.Value;
  }

  public static string GetModuleName(MemberDecl method) {
    var items = method.FullSanitizedName.Split(".");
    var result = new StringBuilder();
    foreach (var item in items.Take(items.Length - 2)) {
      if (item.StartsWith('_')) {
        result.Append(item.AsSpan(1));
        result.Append('_');
      } else {
        result.Append(item);
      }

      if (item != "_module") {
        result.Append("_Compile");
      }

      result.Append('.');
    }

    result.Remove(result.Length - 1, 1);
    return result.ToString();
  }

  public static string GetClassName(MemberDecl method) {
    var items = method.FullSanitizedName.Split(".");
    var className = items[^2];
    if (className.StartsWith("__")) {
      className = string.Join("", className.Skip(2)) + "__";
    }

    return className;
  }

  public override string ToString() {
    header.AppendLine();
    header.Append(trace);
    return header.ToString();
  }
}