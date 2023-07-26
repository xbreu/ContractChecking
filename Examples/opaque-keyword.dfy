// Adapted from Dafny Test programs

ghost predicate {:opaque} F(n: int)
{
  0 <= n < 100
}
method Enter(x : int)
  ensures F(x)
{
}

// Expected but did not find 1 fix
// requires 0 <= x < 100