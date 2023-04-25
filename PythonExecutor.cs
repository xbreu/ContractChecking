using System;
using System.Collections.Generic;
using System.Linq;
using Python.Runtime;

namespace Microsoft.Dafny.ContractChecking;

internal static class PythonExecutor {
  private static void Initialize() {
    const string pythonDll = @"/usr/lib/libpython3.10.so";
    Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", pythonDll);
    PythonEngine.Initialize();
  }

  public static void RunPythonCode(string pythonCode) {
    Initialize();
    using (Py.GIL()) {
      PythonEngine.RunSimpleString(pythonCode);
    }
  }

  public static void RunPythonCode(string pythonCode, object parameter, string parameterName) {
    Initialize();
    using (Py.GIL()) {
      using (var scope = Py.CreateScope()) {
        scope.Set(parameterName, parameter.ToPython());
        scope.Exec(pythonCode);
      }
    }
  }

  public static (object, object, object) RunPythonCodeAndReturn(string moduleName, string className, string methodName, List<IResult> args,
    bool isStatic) {
    object returnedVariableInitial, returnVariable, returnedVariableFinal;
    const string varName = "__python_net_obj";
    const string returnName = "__python_net_ret";
    Initialize();

    var objArgs = args.Select(arg => arg.ToPythonObject()).ToList();

    using (Py.GIL()) {
      using (var scope = Py.CreateScope()) {
        scope.Exec($"import sys\n" +
                   $"sys.path.append('../test/test-py')\n" +
                   $"import test\n");

        // Create object
        className ??= "default__";
        var code = $"{varName} = test.module_.{moduleName}.{className}";
        if (!isStatic) {
          code += "()";
        }

        code += "\n";
        scope.Exec(code);
        returnedVariableInitial = scope.Get<object>(varName);

        // Call the method
        code = $"{returnName} = {varName}.{methodName}(";
        code = args.Aggregate(code, (current, arg) => current + arg.ToPythonObject() + ",");
        code += ")";
        scope.Exec(code);

        // Save final context
        returnVariable = scope.Get<object>(returnName);
        returnedVariableFinal = scope.Get<object>(varName);
      }
    }

    return (returnVariable, returnedVariableInitial, returnedVariableFinal);
  }
}