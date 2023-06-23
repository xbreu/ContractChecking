using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Dafny.ContractChecking;

public static class InvariantParser {
  public static List<(string, bool, Expression)> ParseDaikonOutput(string output) {
    var points = output.Split("===========================================================================\n").ToList();
    // Information about run
    points.RemoveAt(0);

    var result = new List<(string, bool, Expression)>();

    foreach (var point in points) {
      var lines = point.Split('\n').ToList();
      var pointName = lines[0];
      var enter = pointName.EndsWith(":::ENTER");
      var methodName = pointName.Split("():::")[0];
      lines.RemoveAt(0);

      foreach (var line in lines) {
        if (line.StartsWith("Exiting") || line.Length == 0) {
          break;
        }

        result.Add((methodName, enter, ParseInvariant(line)));
      }
    }

    return result;
  }

  private static Expression ParseInvariant(string invariant) {
    // Lexing
    var words = invariant.Split(" ").ToList();
    var tokens = new List<SimplifyToken>();
    foreach (var word in words) {
      var w = word;
      var s = new Stack<SimplifyToken>();
      while (w != "") {
        if (w.StartsWith('(')) {
          tokens.Add(new SimplifyToken(SimplifyToken.SimplifyTokenType.OPEN_PAREN));
          w = w[1..];
          continue;
        }

        if (w.EndsWith(')')) {
          w = w.Remove(w.Length - 1);
          s.Push(new SimplifyToken(SimplifyToken.SimplifyTokenType.CLOSE_PAREN));
          continue;
        }

        if (w.StartsWith('|')) {
          w = w.Trim('|');
          if (w.StartsWith("@")) {
            w = w[1..];
            tokens.Add(new SimplifyToken(
              w switch {
                "true" => SimplifyToken.SimplifyTokenType.TRUE,
                "false" => SimplifyToken.SimplifyTokenType.FALSE,
                _ => throw new ArgumentOutOfRangeException($"Cannot parse constant \"{w}\"")
              }
            ));
            break;
          }

          var old = false;
          if (w.StartsWith("__orig__")) {
            w = w[8..];
            old = true;
          }

          tokens.Add(new VarSimplifyToken(w, old));

          break;
        }

        tokens.Add(w switch {
          "NOT" => new SimplifyToken(SimplifyToken.SimplifyTokenType.NOT),
          "AND" => new SimplifyToken(SimplifyToken.SimplifyTokenType.AND),
          "IFF" => new SimplifyToken(SimplifyToken.SimplifyTokenType.IFF),
          "EQ" => new SimplifyToken(SimplifyToken.SimplifyTokenType.EQ),
          "NEQ" => new SimplifyToken(SimplifyToken.SimplifyTokenType.NEQ),
          "IMPLIES" => new SimplifyToken(SimplifyToken.SimplifyTokenType.IMPLIES),
          "<=" => new SimplifyToken(SimplifyToken.SimplifyTokenType.LE),
          "<" => new SimplifyToken(SimplifyToken.SimplifyTokenType.LT),
          ">" => new SimplifyToken(SimplifyToken.SimplifyTokenType.GT),
          ">=" => new SimplifyToken(SimplifyToken.SimplifyTokenType.GE),
          "+" => new SimplifyToken(SimplifyToken.SimplifyTokenType.ADD),
          "*" => new SimplifyToken(SimplifyToken.SimplifyTokenType.MUL),
          _ => new IntSimplifyToken(int.Parse(w))
        });

        break;
      }

      tokens.AddRange(s);
    }

    // Parsing
    var (e, _) = SimplifyExpression.Parse(tokens);
    return e.ToExpression();
  }
}

public class SimplifyExpression {
  public List<SimplifyExpression> Args;
  public SimplifyToken T;

  private SimplifyExpression(SimplifyToken t, List<SimplifyExpression> args) {
    T = t;
    Args = args;
  }

  private SimplifyExpression(SimplifyToken t) {
    T = t;
    Args = new List<SimplifyExpression>();
  }

  public Expression ToExpression() {
    var argE = Args.Select(arg => arg.ToExpression()).ToList();
    return T.Type switch {
      SimplifyToken.SimplifyTokenType.NOT => new UnaryOpExpr(null, UnaryOpExpr.Opcode.Not, argE[0]),
      SimplifyToken.SimplifyTokenType.AND => new BinaryExpr(null, BinaryExpr.Opcode.And, argE[0], argE[1]),
      SimplifyToken.SimplifyTokenType.IFF => new BinaryExpr(null, BinaryExpr.Opcode.Iff, argE[0], argE[1]),
      SimplifyToken.SimplifyTokenType.EQ => new BinaryExpr(null, BinaryExpr.Opcode.Eq, argE[0], argE[1]),
      SimplifyToken.SimplifyTokenType.NEQ => new BinaryExpr(null, BinaryExpr.Opcode.Neq, argE[0], argE[1]),
      SimplifyToken.SimplifyTokenType.LE => new BinaryExpr(null, BinaryExpr.Opcode.Le, argE[0], argE[1]),
      SimplifyToken.SimplifyTokenType.LT => new BinaryExpr(null, BinaryExpr.Opcode.Lt, argE[0], argE[1]),
      SimplifyToken.SimplifyTokenType.GT => new BinaryExpr(null, BinaryExpr.Opcode.Gt, argE[0], argE[1]),
      SimplifyToken.SimplifyTokenType.GE => new BinaryExpr(null, BinaryExpr.Opcode.Ge, argE[0], argE[1]),
      SimplifyToken.SimplifyTokenType.IMPLIES => new BinaryExpr(null, BinaryExpr.Opcode.Imp, argE[0], argE[1]),
      SimplifyToken.SimplifyTokenType.ADD => new BinaryExpr(null, BinaryExpr.Opcode.Add, argE[0], argE[1]),
      SimplifyToken.SimplifyTokenType.MUL => new BinaryExpr(null, BinaryExpr.Opcode.Mul, argE[0], argE[1]),
      SimplifyToken.SimplifyTokenType.VAR => ((VarSimplifyToken)T).Old
        ? new OldExpr(null, new IdentifierExpr(null, ((VarSimplifyToken)T).Name))
        : new IdentifierExpr(null, ((VarSimplifyToken)T).Name),
      SimplifyToken.SimplifyTokenType.CONST => new LiteralExpr(null, ((IntSimplifyToken)T).Value),
      SimplifyToken.SimplifyTokenType.TRUE => new LiteralExpr(null, true),
      SimplifyToken.SimplifyTokenType.FALSE => new LiteralExpr(null, false),
      _ => throw new ArgumentOutOfRangeException($"{T.Type}")
    };
  }

  public static (SimplifyExpression, List<SimplifyToken>) Parse(List<SimplifyToken> tokens) {
    SimplifyExpression result;
    var l = new List<SimplifyExpression>();
    if (tokens.First().Type == SimplifyToken.SimplifyTokenType.OPEN_PAREN) {
      // Our value begins with '(', it must end with a ')'
      tokens.RemoveAt(0);
      var depth = 1;
      var index = -1;
      do {
        index += 1;
        depth += tokens[index].Type switch {
          SimplifyToken.SimplifyTokenType.OPEN_PAREN => 1,
          SimplifyToken.SimplifyTokenType.CLOSE_PAREN => -1,
          _ => 0
        };
      } while (depth != 0);

      var ourTokens = tokens.GetRange(0, index);
      tokens.RemoveRange(0, index + 1);

      var fun = ourTokens[0];
      ourTokens.RemoveAt(0);
      var rest = ourTokens;
      while (rest.Count > 0) {
        (var arg, rest) = Parse(rest);
        l.Add(arg);
      }

      result = new SimplifyExpression(fun, l);
    } else {
      result = new SimplifyExpression(tokens.First());
      tokens.RemoveAt(0);
    }

    return (result, tokens);
  }
}

public class SimplifyToken {
  public enum SimplifyTokenType {
    OPEN_PAREN,
    CLOSE_PAREN,
    AND,
    IFF,
    EQ,
    NEQ,
    LE,
    LT,
    GT,
    GE,
    IMPLIES,
    VAR,
    CONST,
    TRUE,
    FALSE,
    ADD,
    MUL,
    NOT
  }

  public SimplifyTokenType Type;

  public SimplifyToken(SimplifyTokenType type) {
    Type = type;
  }
}

internal class VarSimplifyToken : SimplifyToken {
  public string Name;
  public bool Old;

  public VarSimplifyToken(string name) : base(SimplifyTokenType.VAR) {
    Name = name;
    Old = false;
  }

  public VarSimplifyToken(string name, bool old) : base(SimplifyTokenType.VAR) {
    Name = name;
    Old = old;
  }
}

internal class IntSimplifyToken : SimplifyToken {
  public int Value;

  public IntSimplifyToken(int value) : base(SimplifyTokenType.CONST) {
    Value = value;
  }
}