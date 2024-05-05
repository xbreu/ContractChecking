using DafnyRepair.Results;

namespace DafnyRepair;

public class Context
{
    public readonly Dictionary<string, IResult> Arguments;
    private readonly List<ObjectResult> obj;

    public Context(List<ObjectResult> obj, Dictionary<string, IResult> args = null)
    {
        this.obj = obj;
        Arguments = args ?? new Dictionary<string, IResult>();
    }

    public void Add(string name, IResult value)
    {
        Arguments[name] = value;
    }

    public Context Copy()
    {
        var newArgs = Arguments.ToDictionary(x => x.Key, x => x.Value);
        return new Context(obj, newArgs);
    }

    public IResult GetValue(string name)
    {
        IResult val;
        if (obj != null)
            foreach (var item in obj)
                item.Attributes.TryGetValue(name, out val);

        Arguments.TryGetValue(name, out val);
        return val;
    }

    public Context AddObj(ObjectResult o)
    {
        var result = Copy();
        result.obj.Add(o);
        return result;
    }

    public Context Merge(Context other)
    {
        var result = Arguments.ToDictionary(x => x.Key, x => x.Value);
        foreach (var y in other.Arguments) result[y.Key] = y.Value;

        return new Context(obj.Concat(other.obj).ToList(), result);
    }
}