using Microsoft.BaseTypes;
using Python.Runtime;

namespace DafnyRepair.Results;

public class RealResult : INumericResult
{
    private BigDec value;

    public RealResult(BigDec value)
    {
        this.value = value;
    }

    public INumericResult Neg()
    {
        return new RealResult(-value);
    }

    public BooleanResult Eq(IResult other)
    {
        return value.Equals(((RealResult)other).value);
    }

    public object ToPythonObject()
    {
        return new PyFloat(value.ToString());
    }

    public BooleanResult Lt(IOrderableResult other)
    {
        return value < ((RealResult)other).value;
    }

    public INumericResult Add(INumericResult other)
    {
        return new RealResult(value + ((RealResult)other).value);
    }

    public INumericResult Sub(INumericResult other)
    {
        return new RealResult(value - ((RealResult)other).value);
    }

    public INumericResult Mul(INumericResult other)
    {
        return new RealResult(value * ((RealResult)other).value);
    }

    public INumericResult Div(INumericResult other)
    {
        // TODO: Euclidean division
        return null;
        // return this.Value / ((RealResult)other).Value;
    }

    public string ToDaikonInput()
    {
        return value.ToDecimalString();
    }

    public static implicit operator RealResult(BigDec d)
    {
        return new RealResult(d);
    }

    public override string ToString()
    {
        return $"Real({value.ToString()})";
    }
}