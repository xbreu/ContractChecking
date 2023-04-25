using System;
using System.Collections.Generic;
using System.Linq;
using Python.Runtime;

namespace Microsoft.Dafny.ContractChecking;

public class ObjectResult : IResult {
  public readonly Dictionary<string, IResult> Attributes;
  public bool IsEmpty;

  private ObjectResult(Dictionary<string, IResult> attributes) {
    Attributes = attributes;
    IsEmpty = false;
  }

  public ObjectResult(Type t) {
    IsEmpty = true;
    Console.WriteLine(t);
    // TODO
  }

  public ObjectResult(List<MemberDecl> members, DafnyOptions options) {
    IsEmpty = true;
    Attributes = new Dictionary<string, IResult>();
    var list = new List<ObjectResult> { this };
    var baseContext = new Context(list);
    foreach (var member in members) {
      switch (member) {
        case Field:
          Attributes.Add(member.Name, null);
          break;
        case Function f: {
          var l = new LambdaResult(options, f.Body, f.Formals, baseContext);
          Attributes.Add(f.Name, l);
          break;
        }
      }
    }


    /*foreach (var member in members) {
      Console.Write("Found ");
      Console.Write(member.WhatKind);
      Console.Write(" declaration \"");
      Console.Write(member.Name);
      Console.WriteLine("\":");
      switch (member) {
        case Method m: {
          foreach (var require in m.Req) {
            var attrs = new Dictionary<string, IResult> { { "lit", new BooleanResult(false) } };
            var ctx = baseContext.AddObj(new ObjectResult(attrs));
            ctx.Add("d", IntegerResult.Zero());
            var eval = new Evaluator(options);
            Console.WriteLine(((BooleanResult)eval.Evaluate(require.E, ctx)).Value);
          }

          break;
        }
      }
    }*/
  }

  public BooleanResult Eq(IResult other) {
    return this == other;
  }

  public object ToPythonObject() {
    return Attributes.ToPython();
  }

  public override string ToString() {
    var result = Attributes.Aggregate("Object(", (current, item) => current + $"{item.Key}:={item.Value},");
    result.Remove(result.Length - 1, 1);
    result += ")";
    return result;
  }
}