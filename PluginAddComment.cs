using System.Diagnostics;
using Microsoft.Dafny;
using Microsoft.Dafny.ContractChecking;
using Microsoft.Dafny.LanguageServer.Plugins;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace ContractFixer {
    public class ContractFixerConfiguration : PluginConfiguration {
        public override DafnyCodeActionProvider[] GetDafnyCodeActionProviders() {
            return new DafnyCodeActionProvider[] { new ContractFixerCodeActionProvider() };
        }
    }

    public class ContractFixerCodeActionProvider : DafnyCodeActionProvider {
        public override IEnumerable<DafnyCodeAction> GetDafnyCodeActions(IDafnyCodeActionInput input, Range selection)
        {
            Debug.Assert(input.Program != null, "input.Program != null");
            ProcessStartInfo startInfo = new() {
                  FileName = "/dafny-custom/Scripts/dafny",
                  Arguments =
                    "/compile:2 /compileTarget:py /plugin/test/test.dfy",
                  CreateNoWindow = true,
                  RedirectStandardOutput = true,
                  RedirectStandardError = true
            };
            var proc = Process.Start(startInfo);
            ArgumentNullException.ThrowIfNull(proc);
            var _ = proc.StandardOutput.ReadToEnd();
            var checker = new ContractChecker("test.dfy");
            var output = checker.CheckProgram(input.Program);
            output.Wait();
            var edits = new List<InstantDafnyCodeAction>
            {
                new ($"{output}", new[]
                {
                    new DafnyCodeActionEdit(input.Program.RangeToken.ToDafnyRange(), $"{output}")
                })
            };
            /*var edits = input.Program.DefaultModuleDef.DefaultClass.Members.OfType<Method>()
                .Select(method => new InstantDafnyCodeAction($"Comment {method.Name}'s method name", new[]{
                        new DafnyCodeActionEdit(method.NameNode.RangeToken.ToDafnyRange(), $"/*{method.Name}/")
                    })
                );*/
            return edits;
        }
    }
}