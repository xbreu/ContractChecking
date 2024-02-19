using System;
using System.Collections.Generic;
using System.Linq;
using SID = Microsoft.Dafny.SpecialField.ID;
using BEO = Microsoft.Dafny.BinaryExpr.ResolvedOpcode;
using BEOU = Microsoft.Dafny.BinaryExpr.Opcode;
using UEO = Microsoft.Dafny.UnaryOpExpr.ResolvedOpcode;
using UEOU = Microsoft.Dafny.UnaryOpExpr.Opcode;

namespace Microsoft.Dafny.ContractChecking;

public class Evaluator {
  public IResult Evaluate(Expression eu, Context context) {
    var e = eu.WasResolved() ? eu.Resolved : eu;
    // Console.WriteLine($"- {e} ({e.GetType()})");

    switch (e) {
      // Final expressions
      case StaticReceiverExpr se:
        return new ObjectResult(se.UnresolvedType);
      case LiteralExpr expr:
        return IResult.FromLiteralExpression(expr);
      case DisplayExpression expression: {
        // Evaluate all the elements of the collection
        var resultElements = expression.Elements.Select(element => Evaluate(element, context)).ToList();

        switch (expression) {
          // Call the respective constructor
          case SeqDisplayExpr:
            return new SequenceResult(resultElements);
          case SetDisplayExpr:
            return new SetResult(resultElements);
          case MultiSetDisplayExpr:
            return new MultisetResult(resultElements);
        }

        break;
      }
      case MapDisplayExpr expr: {
        var resultElements = new Dictionary<IResult, IResult>(ResultComparer.Get());
        foreach (var element in expr.Elements) {
          resultElements[Evaluate(element.A, context)] = Evaluate(element.B, context);
        }

        return new MapResult(resultElements);
      }
      case IdentifierExpr expr:
        return context.GetValue(expr.Name);
      // Comprehension expressions
      case SeqConstructionExpr expr: {
        var n = (IntegerResult)Evaluate(expr.N, context);
        var init = (LambdaResult)Evaluate(expr.Initializer, context);
        var v = init.Args[0];
        var result = new List<IResult>();
        for (var i = 0; i < n.Value; i++) {
          var ctx = context.Copy();
          ctx.Add(v, new IntegerResult(i));
          result.Add(init.Evaluate(ctx));
        }

        return new SequenceResult(result);
      }
      // Object expressions
      case ThisExpr: {
        // TODO
        return null;
      }
      case MemberSelectExpr expr: {
        var obj = Evaluate(expr.Obj, context);
        if (expr.Member is SpecialField se) {
          return se.SpecialId switch {
            SID.UseIdParam => null, // TODO
            SID.ArrayLength => null, // TODO: Arrays
            SID.ArrayLengthInt => null, // TODO: Arrays
            SID.Floor => null, // TODO
            SID.IsLimit => null, // TODO
            SID.IsSucc => null, // TODO
            SID.Offset => null, // TODO
            SID.IsNat => null, // TODO
            SID.Keys => new SetResult(((MapResult)obj).Value.Keys),
            SID.Values => new SetResult(((MapResult)obj).Value.Values),
            SID.Items => null, // TODO: Tuples
            SID.Reads => null, // TODO
            SID.Modifies => null, // TODO
            SID.New => null, // TODO
            _ => throw new ArgumentOutOfRangeException()
          };
        }

        break;
      }
      // Wrapper expressions
      case ParensExpression expression: {
        return Evaluate(expression.E, context);
      }
      // Conversion expressions
      case MultiSetFormingExpr expr: {
        var e0 = Evaluate(expr.E, context);
        return e0 is SequenceResult se ? new MultisetResult(se.Value) : new MultisetResult(((SetResult)e0).Value);
      }
      // Unary expressions
      case UnaryOpExpr expr: {
        var e0 = Evaluate(expr.E, context);
        var rOp = expr.WasResolved() ? expr.ResolvedOp : UEO.Lit;
        return (expr.WasResolved(), rOp, expr.Op) switch {
          (true, UEO.BVNot, _) => null, // TODO: bitvectors
          (true, UEO.BoolNot, _) or (false, _, UEOU.Not) => ((BooleanResult)e0).Not(),
          (true, UEO.SeqLength, _) or (true, UEO.SetCard, _) or (true, UEO.MultiSetCard, _) or (true, UEO.MapCard, _)
            or (false, _, UEOU.Cardinality) => ((ICollectionResult)e0).Length(),
          (true, UEO.Fresh, _) => null, // TODO
          (true, UEO.Allocated, _) => null, // TODO
          (true, UEO.Lit, _) => e0,
          // not supposed to get here after resolution
          _ => throw new ArgumentOutOfRangeException()
        };
      }
      case NegationExpression expression: {
        var e0 = (INumericResult)Evaluate(expression.E, context);
        return e0.Neg();
      }
      // Binary expressions
      case BinaryExpr expr: {
        var e0 = Evaluate(expr.E0, context);
        var e1 = Evaluate(expr.E1, context);
        var rOp = expr.WasResolved() ? expr.ResolvedOp : BEO.Add;
        return (expr.WasResolved(), rOp, expr.Op) switch {
          // booleans
          (true, BEO.Iff, _) or (false, _, BEOU.Iff) => ((BooleanResult)e0).Iff((BooleanResult)e1),
          (true, BEO.Imp, _) or (false, _, BEOU.Imp) => ((BooleanResult)e0).Imp((BooleanResult)e1),
          (true, BEO.And, _) or (false, _, BEOU.And) => ((BooleanResult)e0).And((BooleanResult)e1),
          (true, BEO.Or, _) or (false, _, BEOU.Or) => ((BooleanResult)e0).Or((BooleanResult)e1),
          // non-collection types
          (true, BEO.EqCommon or BEO.SeqEq or BEO.SetEq or BEO.MultiSetEq or BEO.MapEq, _)
            or (false, _, BEOU.Eq) => e0.Eq(e1),
          (true, BEO.NeqCommon or BEO.SeqNeq or BEO.SetNeq or BEO.MultiSetNeq or BEO.MapNeq, _)
            or (false, _, BEOU.Neq) => e0.Neq(e1),
          // integers, reals, bitvectors, char
          (true, BEO.Lt or BEO.LtChar, _) or (false, _, BEOU.Lt) => ((IOrderableResult)e0).Lt((IOrderableResult)e1),
          (true, BEO.Le or BEO.LeChar, _) or (false, _, BEOU.Le) => ((IOrderableResult)e0).Le((IOrderableResult)e1),
          (true, BEO.Ge or BEO.GeChar, _) or (false, _, BEOU.Ge) => ((IOrderableResult)e0).Ge((IOrderableResult)e1),
          (true, BEO.Gt or BEO.GtChar, _) or (false, _, BEOU.Gt) => ((IOrderableResult)e0).Gt((IOrderableResult)e1),
          // integers, reals, bitvectors
          (true, BEO.Add, _) or (false, _, BEOU.Add) => ((INumericResult)e0).Add((INumericResult)e1),
          (true, BEO.Sub, _) or (false, _, BEOU.Sub) => ((INumericResult)e0).Sub((INumericResult)e1),
          (true, BEO.Mul, _) or (false, _, BEOU.Mul) => ((INumericResult)e0).Mul((INumericResult)e1),
          (true, BEO.Div, _) or (false, _, BEOU.Div) => ((INumericResult)e0).Div((INumericResult)e1),
          // integer
          (true, BEO.Mod, _) or (false, _, BEOU.Mod) => ((IntegerResult)e0).Mod((IntegerResult)e1),
          // TODO: bitvectors
          (true, BEO.LeftShift, _) => null,
          (true, BEO.RightShift, _) => null,
          (true, BEO.BitwiseAnd, _) => null,
          (true, BEO.BitwiseOr, _) => null,
          (true, BEO.BitwiseXor, _) => null,
          // sets, multi-sets
          (true, BEO.ProperSubset or BEO.ProperMultiSubset, _) => ((IBagResult)e0).ProperSubset((IBagResult)e1),
          (true, BEO.Subset or BEO.MultiSubset, _) => ((IBagResult)e0).Subset((IBagResult)e1),
          (true, BEO.Superset or BEO.MultiSuperset, _) => ((IBagResult)e0).Superset((IBagResult)e1),
          (true, BEO.ProperSuperset or BEO.ProperMultiSuperset, _) => ((IBagResult)e0).ProperSuperset((IBagResult)e1),
          (true, BEO.Disjoint or BEO.MultiSetDisjoint, _) => ((IBagResult)e0).Disjoint((IBagResult)e1),
          (true, BEO.Union or BEO.MultiSetUnion, _) => ((IBagResult)e0).Union((IBagResult)e1),
          (true, BEO.Intersection or BEO.MultiSetIntersection, _) => ((IBagResult)e0).Intersection((IBagResult)e1),
          (true, BEO.SetDifference or BEO.MultiSetDifference, _) => ((IBagResult)e0).Difference((IBagResult)e1),
          // sets, multi-sets, sequences, maps
          (true, BEO.InSet or BEO.InMultiSet or BEO.InSeq or BEO.InMap, _) => ((ICollectionResult)e1).Contains(e0),
          (true, BEO.NotInSet or BEO.NotInMultiSet or BEO.NotInSeq or BEO.NotInMap, _) =>
            ((ICollectionResult)e1).DoesNotContain(e0),
          // sequences
          (true, BEO.ProperPrefix, _) => ((SequenceResult)e0).ProperPrefix((SequenceResult)e1),
          (true, BEO.Prefix, _) => ((SequenceResult)e0).Prefix((SequenceResult)e1),
          (true, BEO.Concat, _) => ((SequenceResult)e0).Concat((SequenceResult)e1),
          // maps
          (true, BEO.MapMerge, _) => ((MapResult)e0).Merge((MapResult)e1),
          (true, BEO.MapSubtraction, _) => ((MapResult)e0).Difference((SetResult)e1),
          // TODO: datatypes
          (true, BEO.RankLt, _) => null,
          (true, BEO.RankGt, _) => null,
          // not supposed to get here after resolution
          (true, _, _) => throw new ArgumentOutOfRangeException("Not supposed to get here"),
          (false, _, var t) => throw new ArgumentOutOfRangeException($"Type {t}")
        };
      }
      // Collection expressions
      case SeqSelectExpr expr: {
        var se = expr;
        var seq = (ICollectionResult)Evaluate(se.Seq, context);
        var e0 = IntegerResult.Zero();
        if (se.E0 != null) {
          e0 = (IntegerResult)Evaluate(se.E0, context);
          switch (seq) {
            case MultisetResult mr:
              return mr.Multiplicity(e0);
            case MapResult mr:
              return mr.Get(e0);
          }

          if (se.SelectOne) {
            return ((SequenceResult)seq).At(e0);
          }
        }

        var e1 = (IntegerResult)seq.Length().Sub(e0);
        if (se.E1 != null) {
          e1 = (IntegerResult)Evaluate(se.E1, context);
        }

        return ((SequenceResult)seq).Range(e0, e1);
      }
      case SeqUpdateExpr expr: {
        var seq = Evaluate(expr.Seq, context);
        var index = Evaluate(expr.Index, context);
        var value = Evaluate(expr.Value, context);
        return seq switch {
          MultisetResult result => result.UpdateMultiplicity(index, (IntegerResult)value),
          MapResult result => result.Update(index, value),
          _ => ((SequenceResult)seq).UpdateAt((IntegerResult)index, value)
        };
      }
      // Function expressions
      case FunctionCallExpr expr: {
        var name = expr.Name;
        // TODO: var receiver = Evaluate(expr.Receiver, context);
        var args = expr.Args.Select(arg => Evaluate(arg, context)).ToList();
        var func = ContractChecker.FindFunction(name);
        var names = func.Formals.Select(formal => formal.Name).ToList();
        var dict = new Dictionary<string, IResult>();
        var i = 0;
        foreach (var argName in names) {
          dict[argName] = args[i++];
        }

        var funContext = new Context(new List<ObjectResult>(), dict);
        return Evaluate(func.Body, funContext);
      }
      case LambdaExpr expr: {
        return new LambdaResult(expr.Body, expr.AllBoundVars);
      }
      case ITEExpr expr: {
        var testR = (BooleanResult)Evaluate(expr.Test, context);
        return Evaluate(testR ? expr.Thn : expr.Els, context);
      }
      // TODO
      case SetComprehension:
        break;
      case MapComprehension:
        break;
      case DatatypeValue:
        break;
      case MultiSelectExpr:
        break;
      case ApplyExpr:
        break;
      case TernaryExpr:
        break;
      case LetOrFailExpr:
        break;
      case WildcardExpr:
        break;
      case StmtExpr:
        break;
      case DatatypeUpdateExpr:
        break;
      case DefaultValueExpression:
        break;
      case ChainingExpression:
        break;
      case SuffixExpr:
        break;
      case NameSegment:
        break;
      // TODO: Types
      case ConversionExpr:
        break;
      case TypeTestExpr:
        break;
    }

    // Not implemented, or invalid, expression type
    throw new InvalidOperationException($"{e} : {e.GetType()}");
  }
}