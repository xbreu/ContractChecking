namespace DafnyRepair.Results;

public interface ICollectionResult : IResult
{
    public IntegerResult Length();
    public BooleanResult Contains(IResult element);

    public BooleanResult DoesNotContain(IResult element)
    {
        return !Contains(element);
    }
}