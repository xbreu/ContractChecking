using System;
using System.Collections.Generic;
using Microsoft.Dafny.ContractChecking.Fixes;

namespace Microsoft.Dafny.ContractChecking;

public class TestCase {
  public enum TestResult {
    Passing,
    Failing
  }

  private readonly List<List<IResult>> arguments;

  private readonly MemberDecl method;

  public TestCase(MemberDecl method, List<List<IResult>> arguments = null) {
    this.method = method;
    this.arguments = arguments ?? new List<List<IResult>>();
  }

  public SequenceResult Run() {
    // Console.WriteLine($"Running method {method.Name}");
    var moduleName = DaikonTrace.GetModuleName(method);
    var className = DaikonTrace.GetClassName(method);
    var methodName = method.Name;
    Console.Error.WriteLine("1");
    var trace = PythonExecutor.RunPythonCodeAndReturn(
      moduleName, className, methodName,
      arguments, method.IsStatic);
    Console.Error.WriteLine("3");
    return (SequenceResult)trace;
  }
}