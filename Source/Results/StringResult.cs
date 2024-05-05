using Python.Runtime;

namespace DafnyRepair.Results;

public class StringResult : IResult
{
    public readonly string Value;

    public StringResult(string value)
    {
        Value = value;
    }

    public BooleanResult Eq(IResult other)
    {
        return string.Compare(Value, ((StringResult)other).Value, StringComparison.Ordinal) == 0;
    }

    public object ToPythonObject()
    {
        return new PyString(Value);
    }

    public string ToDaikonInput()
    {
        return Value.Trim('\'');
    }

    public override string ToString()
    {
        return $"String({Value})";
    }
}