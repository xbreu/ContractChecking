using Python.Runtime;

namespace DafnyRepair.Results;

public class CharResult : IOrderableResult
{
    private readonly string value;

    public CharResult(string value)
    {
        this.value = value;
    }

    public BooleanResult Eq(IResult other)
    {
        return string.Compare(value, ((CharResult)other).value, StringComparison.Ordinal) == 0;
    }

    public object ToPythonObject()
    {
        return new PyString(value);
    }

    public string ToDaikonInput()
    {
        return value;
    }

    public BooleanResult Lt(IOrderableResult other)
    {
        return false;
    }

    public override string ToString()
    {
        return $"Char({value})";
    }
}