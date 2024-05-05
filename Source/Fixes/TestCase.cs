using DafnyRepair.Results;
using Microsoft.Dafny;

namespace DafnyRepair.Fixes;

public class TestCase
{
    public enum TestResult
    {
        PASSING,
        FAILING
    }

    private readonly List<List<IResult>> arguments;

    private readonly MemberDecl method;

    public TestCase(MemberDecl method, List<List<IResult>> arguments = null)
    {
        this.method = method;
        this.arguments = arguments ?? new List<List<IResult>>();
    }

    public SequenceResult Run()
    {
        // Console.WriteLine($"Running method {method.Name}");
        var moduleName = DaikonTrace.GetModuleName(method);
        var className = DaikonTrace.GetClassName(method);
        var methodName = method.Name;
        var trace = PythonExecutor.RunPythonCodeAndReturn(
            moduleName, className, methodName,
            arguments, method.IsStatic);
        return (SequenceResult)trace;
    }
}
