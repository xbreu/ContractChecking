using System;
using System.Collections.Generic;
using System.Linq;
using SID = Microsoft.Dafny.SpecialField.ID;
using BEO = Microsoft.Dafny.BinaryExpr.ResolvedOpcode;
using UEO = Microsoft.Dafny.UnaryOpExpr.ResolvedOpcode;

namespace Microsoft.Dafny.ContractChecking;

public class Evaluator {
  private readonly DafnyOptions options;
  private readonly Program program;

  public Evaluator(Program program, DafnyOptions options) {
    this.options = options;
    this.program = program;
  }

  public IResult Evaluate(Expression eu, Context context) {
    var e = eu.Resolved;
    // Console.WriteLine(e.ToString());
    // Console.WriteLine($"{e.GetType()}");

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
        return expr.ResolvedOp switch {
          UEO.BVNot => null, // TODO: bitvectors
          UEO.BoolNot => ((BooleanResult)e0).Not(),
          UEO.SeqLength or UEO.SetCard or UEO.MultiSetCard or UEO.MapCard => ((ICollectionResult)e0).Length(),
          UEO.Fresh => null, // TODO
          UEO.Allocated => null, // TODO
          UEO.Lit => e0,
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
        return expr.ResolvedOp switch {
          // booleans
          BEO.Iff => ((BooleanResult)e0).Iff((BooleanResult)e1),
          BEO.Imp => ((BooleanResult)e0).Imp((BooleanResult)e1),
          BEO.And => ((BooleanResult)e0).And((BooleanResult)e1),
          BEO.Or => ((BooleanResult)e0).Or((BooleanResult)e1),
          // non-collection types
          BEO.EqCommon or BEO.SeqEq or BEO.SetEq or BEO.MultiSetEq or BEO.MapEq => e0.Eq(e1),
          BEO.NeqCommon or BEO.SeqNeq or BEO.SetNeq or BEO.MultiSetNeq or BEO.MapNeq => e0.Neq(e1),
          // integers, reals, bitvectors, char
          BEO.Lt or BEO.LtChar => ((IOrderableResult)e0).Lt((IOrderableResult)e1),
          BEO.Le or BEO.LeChar => ((IOrderableResult)e0).Le((IOrderableResult)e1),
          BEO.Ge or BEO.GeChar => ((IOrderableResult)e0).Ge((IOrderableResult)e1),
          BEO.Gt or BEO.GtChar => ((IOrderableResult)e0).Gt((IOrderableResult)e1),
          // integers, reals, bitvectors
          BEO.Add => ((INumericResult)e0).Add((INumericResult)e1),
          BEO.Sub => ((INumericResult)e0).Sub((INumericResult)e1),
          BEO.Mul => ((INumericResult)e0).Mul((INumericResult)e1),
          BEO.Div => ((INumericResult)e0).Div((INumericResult)e1),
          // integer
          BEO.Mod => ((IntegerResult)e0).Mod((IntegerResult)e1),
          // TODO: bitvectors
          BEO.LeftShift => null,
          BEO.RightShift => null,
          BEO.BitwiseAnd => null,
          BEO.BitwiseOr => null,
          BEO.BitwiseXor => null,
          // sets, multi-sets
          BEO.ProperSubset or BEO.ProperMultiSubset => ((IBagResult)e0).ProperSubset((IBagResult)e1),
          BEO.Subset or BEO.MultiSubset => ((IBagResult)e0).Subset((IBagResult)e1),
          BEO.Superset or BEO.MultiSuperset => ((IBagResult)e0).Superset((IBagResult)e1),
          BEO.ProperSuperset or BEO.ProperMultiSuperset => ((IBagResult)e0).ProperSuperset((IBagResult)e1),
          BEO.Disjoint or BEO.MultiSetDisjoint => ((IBagResult)e0).Disjoint((IBagResult)e1),
          BEO.Union or BEO.MultiSetUnion => ((IBagResult)e0).Union((IBagResult)e1),
          BEO.Intersection or BEO.MultiSetIntersection => ((IBagResult)e0).Intersection((IBagResult)e1),
          BEO.SetDifference or BEO.MultiSetDifference => ((IBagResult)e0).Difference((IBagResult)e1),
          // sets, multi-sets, sequences, maps
          BEO.InSet or BEO.InMultiSet or BEO.InSeq or BEO.InMap => ((ICollectionResult)e1).Contains(e0),
          BEO.NotInSet or BEO.NotInMultiSet or BEO.NotInSeq or BEO.NotInMap =>
            ((ICollectionResult)e1).DoesNotContain(e0),
          // sequences
          BEO.ProperPrefix => ((SequenceResult)e0).ProperPrefix((SequenceResult)e1),
          BEO.Prefix => ((SequenceResult)e0).Prefix((SequenceResult)e1),
          BEO.Concat => ((SequenceResult)e0).Concat((SequenceResult)e1),
          // maps
          BEO.MapMerge => ((MapResult)e0).Merge((MapResult)e1),
          BEO.MapSubtraction => ((MapResult)e0).Difference((SetResult)e1),
          // TODO: datatypes
          BEO.RankLt => null,
          BEO.RankGt => null,
          // not supposed to get here after resolution
          _ => throw new ArgumentOutOfRangeException()
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
        var receiver = Evaluate(expr.Receiver, context);
        var args = expr.Args.Select(arg => Evaluate(arg, context)).ToList();
        break;
      }
      case LambdaExpr expr: {
        return new LambdaResult(options, expr.Body, expr.AllBoundVars);
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