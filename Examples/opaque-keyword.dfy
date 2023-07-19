// Adapted from Dafny Test programs

ghost predicate {:opaque} F(n: int)
{
  0 <= n < 100
}
method Enter(x : int)
  ensures F(x)
{
}