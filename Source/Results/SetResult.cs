using System.Text;
using Python.Runtime;

namespace DafnyRepair.Results;

// TODO: Infinite sets
public class SetResult : IBagResult
{
    public readonly HashSet<IResult> Value;

    public SetResult(IEnumerable<IResult> value)
    {
        Value = new HashSet<IResult>(value, ResultComparer.Get());
    }

    public IntegerResult Length()
    {
        return Value.Count;
    }

    public BooleanResult Contains(IResult element)
    {
        return Value.Contains(element);
    }

    public IBagResult Union(IBagResult otherB)
    {
        var other = (SetResult)otherB;
        return new SetResult(Value.Union(other.Value, ResultComparer.Get()));
    }

    public IBagResult Intersection(IBagResult otherB)
    {
        var other = (SetResult)otherB;
        return new SetResult(Value.Intersect(other.Value, ResultComparer.Get()));
    }

    public IBagResult Difference(IBagResult otherB)
    {
        var other = (SetResult)otherB;
        return new SetResult(Value.Except(other.Value, ResultComparer.Get()));
    }

    public BooleanResult Eq(IResult other)
    {
        return Value.SetEquals(((SetResult)other).Value);
    }

    public object ToPythonObject()
    {
        return Value.ToPython();
    }

    public string ToDaikonInput()
    {
        var result = new StringBuilder();
        result.Append("[ ");
        foreach (var v in Value) result.Append($"{v.ToDaikonInput()} ");

        result.Append(']');
        return result.ToString();
    }

    public override string ToString()
    {
        var result = Value.Aggregate("Set(", (current, item) => current + $"{item},");
        result.Remove(result.Length - 1, 1);
        result += ")";
        return result;
    }
}