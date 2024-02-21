using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DafnyDriver.ContractChecking.Fixes;

namespace Microsoft.Dafny.ContractChecking.Fixes;

public class DaikonTrace {
  public enum TestResult {
    FAILING,
    PASSING,
    INVALID
  }

  private readonly List<string> declared = new();
  private readonly StringBuilder header = new();
  public readonly StringBuilder Trace = new();

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
          "real" => "double",
          _ => "hashcode"
        });
        var comp = f.Type.ToString() switch {
          "bool" => 1,
          "int" => 2,
          _ => 20
        };
        header.AppendLine($"\tcomparability {comp}");
      }
      
      header.AppendLine("variable pre_condition");
      header.AppendLine("\tvar-kind variable");
      header.AppendLine("\tdec-type boolean");
      header.AppendLine("\trep-type boolean");
      header.AppendLine("\tcomparability 1");

      if (type == "EXIT00") {
        foreach (var f in m.Outs) {
          header.AppendLine($"variable {f.CompileName}");
          header.AppendLine("\tvar-kind variable");
          header.AppendLine($"\tdec-type {f.Type}");
          header.Append("\trep-type ");
          header.AppendLine(f.Type.ToString() switch {
            "int" => "int",
            "real" => "double",
            _ => "hashcode"
          });
          var comp = f.Type.ToString() switch {
            "bool" => 1,
            "int" => 2,
            _ => 20
          };
          header.AppendLine($"\tcomparability {comp}");
        }
        
        header.AppendLine("variable post_condition");
        header.AppendLine("\tvar-kind variable");
        header.AppendLine("\tdec-type boolean");
        header.AppendLine("\trep-type boolean");
        header.AppendLine("\tcomparability 1");
        header.AppendLine("variable passed_all_inside");
        header.AppendLine("\tvar-kind variable");
        header.AppendLine("\tdec-type boolean");
        header.AppendLine("\trep-type boolean");
        header.AppendLine("\tcomparability 1");
      }

      header.AppendLine();
    }
  }

  public TestResult AddPoint(Method method, string nonce, SequenceResult parameters, ContractType place,
    ContractManager contractManager) {
    var name = $"{GetModuleName(method)}.{GetClassName(method)}.{method.Name}";

    // If the method is in the trace it needs to be on the Daikon method declaration module
    if (!declared.Contains(name)) {
      AddMethod(method);
    }

    // Information about the point
    Trace.Append($"{name}():::");
    Trace.AppendLine(place == ContractType.PRE_CONDITION ? "ENTER" : "EXIT00");
    Trace.AppendLine("this_invocation_nonce");
    Trace.AppendLine(nonce);

    // We need to create a mapping of the current variables
    var variableMapping = new Dictionary<string, IResult>();
    var context = new Context(null, variableMapping);
    var evaluator = new Evaluator();
    var i = 0;

    // Map the parameters to their argument name
    var methodArgumentNames = method.Ins.Select(x => x.CompileName).ToList();
    for (; i < methodArgumentNames.Count; i++) {
      var argumentName = methodArgumentNames[i];
      var concreteValue = parameters.At(i);
      variableMapping.Add(argumentName, concreteValue);
      // Add to trace file
      Trace.AppendLine(argumentName);
      Trace.AppendLine(concreteValue.ToDaikonInput());
      Trace.AppendLine("0");
    }

    // Pre-condition
    var followsPreCondition = (BooleanResult)evaluator.Evaluate(
      contractManager.GetContract(method, ContractType.PRE_CONDITION),
      context);
    Trace.AppendLine("pre_condition");
    Trace.AppendLine(followsPreCondition ? "1" : "0");
    Trace.AppendLine("0");

    // No more information to be reported
    var invalidTest = !followsPreCondition && method == FixConfiguration.GetOuter();
    if (place == ContractType.PRE_CONDITION) {
      if (invalidTest) {
        return TestResult.INVALID;
      }

      return followsPreCondition ? TestResult.PASSING : TestResult.FAILING;
    }

    // If we are analysing the exit-point, we need to map the output as well
    var nullValue = parameters.At(i) == null; // Exception occured when running the test

    // Add the outputs to the context
    var methodOutputNames = method.Outs.Select(x => x.CompileName).ToList();
    foreach (var outputName in methodOutputNames) {
      string daikonVariableValue;
      // If the return is null, we add a dummy '0' to the trace
      if (!nullValue) {
        var concreteValue = parameters.At(i++);
        variableMapping.Add(outputName, concreteValue);
        daikonVariableValue = concreteValue.ToDaikonInput();
      } else {
        daikonVariableValue = "0";
      }

      // Add to trace file
      Trace.AppendLine(outputName);
      Trace.AppendLine(daikonVariableValue);
      Trace.AppendLine("0");
    }

    // Post-condition
    var followsPostCondition = nullValue
      ? false
      : (BooleanResult)evaluator.Evaluate(
        contractManager.GetContract(method, ContractType.POST_CONDITION),
        context);
    Trace.AppendLine("post_condition");
    Trace.AppendLine(followsPostCondition ? "1" : "0");
    Trace.AppendLine("0");

    if (invalidTest) {
      return TestResult.INVALID;
    }

    var followsContract = followsPreCondition.And(followsPostCondition);
    return followsContract ? TestResult.PASSING : TestResult.FAILING;
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
    header.Append(Trace);
    return header.ToString();
  }

  public void AddContractVariable(bool passedAllInside) {
    Trace.AppendLine("passed_all_inside");
    Trace.AppendLine(passedAllInside ? "1" : "0");
    Trace.AppendLine("0");
  }
}