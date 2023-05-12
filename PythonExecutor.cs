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

  public static (IResult, (List<IResult>, List<IResult>), IResult) RunPythonCodeAndReturn(string moduleName,
    string className, string methodName, List<string> modifies, List<IResult> args,
    bool isStatic, string objectName = "_") {
    PyObject returnVariable, trace;
    Initialize();
    const string returnName = "__python_net_ret";

    var objArgs = args.Select(arg => arg.ToPythonObject()).ToList();
    var capturedBefore = new List<IResult>();
    var capturedAfter = new List<IResult>();

    using (Py.GIL()) {
      using (var scope = Py.CreateScope()) {
        Console.WriteLine("Running Python code:");
        Console.WriteLine("------------------------------------");

        var code = $"import sys\n" +
                   $"sys.path.append('../test/test-py')\n" +
                   $"import test\n";
        Console.Write(code);
        scope.Exec(code);

        // Create object
        className ??= "default__";
        code = $"{objectName} = test.module_.{moduleName}.{className}";
        if (!isStatic) {
          code += "()";
        }

        code += "\n";
        Console.Write(code);
        scope.Exec(code);
        capturedBefore.AddRange(modifies.Select(mod => FromPyObject(scope.Get(mod))));

        // Call the method
        code = $"{returnName} = {objectName}.{methodName}(";
        code = args.Aggregate(code, (current, arg) => current + arg.ToPythonObject() + ",");
        code += ")\n";
        Console.Write(code);
        scope.Exec(code);

        // Save final context
        returnVariable = scope.Get(returnName);
        capturedAfter.AddRange(modifies.Select(mod => FromPyObject(scope.Get(mod))));

        // Get trace
        code = $"{returnName} = test.module_.{moduleName}.trace__\n";
        Console.Write(code);
        scope.Exec(code);
        trace = scope.Get(returnName);
        Console.WriteLine("------------------------------------");
      }
    }

    return (FromPyObject(returnVariable), (capturedBefore, capturedAfter), FromPyObject(trace));
  }

  private static IResult FromPyObject(PyObject obj) {
    return obj switch {
      PyIter pyIter => throw new NotImplementedException(),
      Py.KeywordArguments keywordArguments => throw new NotImplementedException(),
      PyDict pyDict => throw new NotImplementedException(),
      PyFloat pyFloat => throw new NotImplementedException(),
      PyInt pyInt => throw new NotImplementedException(),
      PyList pyList => throw new NotImplementedException(),
      PyString pyString => throw new NotImplementedException(),
      PyTuple pyTuple => throw new NotImplementedException(),
      PySequence pySequence => throw new NotImplementedException(),
      PyIterable pyIterable => throw new NotImplementedException(),
      PyModule pyModule => throw new NotImplementedException(),
      PyNumber pyNumber => throw new NotImplementedException(),
      PyType pyType => throw new NotImplementedException(),
      PyObject pyObj => throw new NotImplementedException($"{obj.Repr()}"),
      _ => throw new ArgumentOutOfRangeException($"{obj.Repr()} {obj.GetType()}")
    };
  }
}