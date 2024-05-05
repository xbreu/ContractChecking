namespace DafnyRepair.Results;

public interface IBagResult : ICollectionResult
{
    public IBagResult Union(IBagResult other);

    public IBagResult Intersection(IBagResult other);

    public IBagResult Difference(IBagResult other);

    public BooleanResult IsEmpty()
    {
        return Length().IsZero();
    }

    public BooleanResult Subset(IBagResult other)
    {
        return Intersection(other).Length() == Length();
    }

    public BooleanResult ProperSubset(IBagResult other)
    {
        return Subset(other) & Neq(other);
    }

    public BooleanResult Superset(IBagResult other)
    {
        return other.Subset(this);
    }

    public BooleanResult ProperSuperset(IBagResult other)
    {
        return Superset(other) & Neq(other);
    }

    public BooleanResult Disjoint(IBagResult other)
    {
        return Intersection(other).IsEmpty();
    }
}