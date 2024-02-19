using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DafnyDriver.ContractChecking.Fixes;
using Microsoft.BaseTypes;
using Python.Runtime;

namespace Microsoft.Dafny.ContractChecking;

internal static class PythonExecutor {
  private static readonly bool DebugPrint = FixConfiguration.ShouldDebug(DebugInformation.PYTHON_EXECUTIONS);

  private static void Initialize() {
    const string pythonDll = @"/usr/lib/libpython3.11.so";
    Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", pythonDll);
    PythonEngine.Initialize();
    // PythonEngine.Initialize(Enumerable.Empty<string>(), true, false);
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

  public static IResult RunPythonCodeAndReturn(string moduleName,
    string className, string methodName, List<List<IResult>> args,
    bool isStatic, string objectName = "_") {
    Console.Error.WriteLine("2");
    PyObject trace;
    Initialize();
    const string returnName = "__python_net_ret";

    using (Py.GIL()) {
      using (var scope = Py.CreateScope()) {
        if (DebugPrint) {
          Console.WriteLine("Running Python code:");
        }

        var code = $"import sys\n" +
                   $"sys.path.append('../test/test-py')\n" +
                   $"import test\n";
        if (DebugPrint) {
          Console.Write(code);
        }

        scope.Exec(code);

        // Create object
        className ??= "default__";
        code = $"{objectName} = test.module_.{moduleName}.{className}";
        if (!isStatic) {
          code += "()";
        }

        code += "\n";
        if (DebugPrint) {
          Console.Write(code);
        }

        scope.Exec(code);

        // Call the method
        foreach (var parameters in args) {
          code = $"{returnName} = {objectName}.{methodName}(";
          code = parameters.Aggregate(code, (current, arg) => current + arg.ToPythonObject() + ",");
          code += ")\n";
          if (DebugPrint) {
            Console.Write(code);
          }

          scope.Exec(code);
        }

        // Get trace
        code = $"{returnName} = test.module_.{moduleName}.trace__\n";
        if (DebugPrint) {
          Console.Write(code);
        }

        scope.Exec(code);
        trace = scope.Get(returnName);
      }
    }

    return FromPyObject(trace);
  }

  private static IResult FromPyObject(PyObject obj) {
    return obj switch {
      PyIter pyIter => throw new NotImplementedException(),
      Py.KeywordArguments keywordArguments => throw new NotImplementedException(),
      PyDict pyDict => throw new NotImplementedException(),
      PyFloat pyFloat => throw new NotImplementedException(),
      PyInt pyInt => new IntegerResult(pyInt.ToInt64()),
      PyList pyList => new SequenceResult(pyList.ToList().Select(FromPyObject)),
      PyString pyString => new StringResult(pyString.Repr()),
      PyTuple pyTuple => new SequenceResult(pyTuple.ToList().Select(FromPyObject)),
      PySequence pySequence => throw new NotImplementedException(),
      PyIterable pyIterable => throw new NotImplementedException(),
      PyModule pyModule => throw new NotImplementedException(),
      PyNumber pyNumber => throw new NotImplementedException(),
      PyType pyType => throw new NotImplementedException(),
      PyObject pyObj => pyObj.GetPythonType().Name switch {
        "int" => FromPyObject(new PyInt(pyObj)),
        "list" => FromPyObject(new PyList(pyObj)),
        "tuple" => FromPyObject(new PyTuple(pyObj)),
        "str" => FromPyObject(new PyString(pyObj)),
        "bool" => new BooleanResult($"{pyObj}" == "True"),
        "BigRational" => new RealResult(BigDec.FromString($"{pyObj}")),
        "NoneType" => null,
        _ => throw new NotImplementedException($"Cannot convert \"{pyObj.GetPythonType().Name}\" to result: {pyObj}")
      },
      _ => throw new ArgumentOutOfRangeException($"{obj.Repr()} {obj.GetType()}")
    };
  }
}