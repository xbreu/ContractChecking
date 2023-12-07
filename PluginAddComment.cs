using System.Diagnostics;
using Microsoft.Dafny;
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
            var edits = input.Program.DefaultModuleDef.DefaultClass.Members.OfType<Method>()
                .Select(method => new InstantDafnyCodeAction($"Comment {method.Name}'s method name", new[]{
                        new DafnyCodeActionEdit(method.NameNode.RangeToken.ToDafnyRange(), $"/*{method.Name}*/")
                    })
                );
            return edits;
        }
    }
}