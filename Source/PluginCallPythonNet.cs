using System.Diagnostics;
using Microsoft.Dafny;
using Microsoft.Dafny.LanguageServer.Language;
using Microsoft.Dafny.LanguageServer.Plugins;
using Python.Runtime;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace DafnyRepair;

public class TestConfiguration : PluginConfiguration
{
    public override DafnyCodeActionProvider[] GetDafnyCodeActionProviders()
    {
        return new DafnyCodeActionProvider[] { new AddCommentDafnyCodeActionProvider() };
    }
}

public class AddCommentDafnyCodeActionProvider : DafnyCodeActionProvider
{
    public override IEnumerable<DafnyCodeAction> GetDafnyCodeActions(IDafnyCodeActionInput input, Range selection)
    {
        var firstTokenRange = input.Program?.GetFirstTopLevelToken()?.GetLspRange();
        if (firstTokenRange != null && firstTokenRange.Start.Line == selection.Start.Line)
            return new DafnyCodeAction[]
            {
                new CustomDafnyCodeAction(input, firstTokenRange)
            };
        return new DafnyCodeAction[] { };
    }
}

public class CustomDafnyCodeAction : DafnyCodeAction
{
    private readonly IDafnyCodeActionInput input;
    public Range WhereToInsert;

    public CustomDafnyCodeAction(IDafnyCodeActionInput input, Range whereToInsert) : base("Insert comment")
    {
        WhereToInsert = whereToInsert;
        this.input = input;
    }

    public override IEnumerable<DafnyCodeActionEdit> GetEdits()
    {
        string x;
        const string pythonDll = @"/usr/lib/python3.10/config-3.10-x86_64-linux-gnu/libpython3.10.so";
        Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", pythonDll);
        PythonEngine.Initialize();
        using (Py.GIL())
        {
            using (var scope = Py.CreateScope())
            {
                scope.Set("a", null);
                scope.Exec("a = 1 + 2");
                var a = scope.Eval("a");
                x = $"a = {a}";
            }
        }

        PythonEngine.Shutdown();

        var value = "Custom Plugin Initiated\n";
        Debug.Assert(input.Program != null, "input.Program != null");
        ProcessStartInfo startInfo = new()
        {
            FileName = "/plugin/Dafny/Scripts/dafny",
            Arguments = "/compile:2 /compileTarget:py /plugin/test.dfy",
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        value += "Compiling Program to Python\n";
        var proc = Process.Start(startInfo);
        ArgumentNullException.ThrowIfNull(proc);
        var _ = proc.StandardOutput.ReadToEnd();
        value += "Starting Contract Repair\n";
        // Works
        /*var checker = new ContractChecker("/plugin/test.dfy");
        var task = checker.CheckProgram(input.Program);
        task.Wait();
        var log = task.Result;
        value += log;*/
        value += "Creating Code Actions\n";

        return new[]
        {
            new DafnyCodeActionEdit(new DafnyRange(new DafnyPosition(0, 0), new DafnyPosition(0, 1)),
                $"/*A comment {x} \n{value}\n*/")
        };
    }
}
