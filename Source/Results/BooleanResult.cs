using Python.Runtime;

namespace DafnyRepair.Results;

public class BooleanResult : IResult
{
    public readonly bool Value;

    public BooleanResult(bool value)
    {
        Value = value;
    }

    public BooleanResult Eq(IResult other)
    {
        return Value.Equals(((BooleanResult)other).Value);
    }

    public object ToPythonObject()
    {
        return Value.ToPython();
    }

    public string ToDaikonInput()
    {
        return this ? "1" : "0";
    }

    public static implicit operator BooleanResult(bool b)
    {
        return new BooleanResult(b);
    }

    public static bool operator true(BooleanResult x)
    {
        return x.Value;
    }

    public static bool operator false(BooleanResult x)
    {
        return x.Value == false;
    }

    public static BooleanResult operator &(BooleanResult first, BooleanResult second)
    {
        return first.Value && second.Value;
    }

    public static BooleanResult operator |(BooleanResult first, BooleanResult second)
    {
        return first.Value || second.Value;
    }

    public static BooleanResult operator !(BooleanResult first)
    {
        return !first.Value;
    }

    public BooleanResult Not()
    {
        return !Value;
    }

    public BooleanResult Iff(BooleanResult other)
    {
        return Value == other.Value;
    }

    public BooleanResult Imp(BooleanResult other)
    {
        return !Value || other.Value;
    }

    public BooleanResult And(BooleanResult other)
    {
        return Value && other.Value;
    }

    public BooleanResult Or(BooleanResult other)
    {
        return Value || other.Value;
    }

    public override string ToString()
    {
        return $"Boolean({Value.ToString()})";
    }
}