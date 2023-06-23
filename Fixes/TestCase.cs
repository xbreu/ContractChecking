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

  private readonly Method method;
  private readonly DafnyOptions options;
  private readonly Program program;

  public TestCase(Method method, DafnyOptions options, Program program, List<List<IResult>> arguments = null) {
    this.method = method;
    this.options = options;
    this.program = program;
    this.arguments = arguments ?? new List<List<IResult>>();
  }

  public SequenceResult Run() {
    Console.WriteLine($"Running method {method.Name}");
    var trace = PythonExecutor.RunPythonCodeAndReturn(
      DaikonTrace.GetModuleName(method), DaikonTrace.GetClassName(method), method.Name,
      arguments, method.IsStatic);
    return (SequenceResult)trace;
  }
}