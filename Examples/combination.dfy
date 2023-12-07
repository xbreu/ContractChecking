method Factorial(n : int) returns (f : int)
  requires n >= 1 // 0
  ensures f > 0
{
  if n == 0 {
    return 1;
  }
  f := 1;
  var i := 2;
  while i <= n
    invariant f > 0
  {
    f := f * i;
    i := i + 1;
  }
}

method Combination(n : int, k : int) returns (c : int)
  requires n > 0
  requires k >= 0 // >
  requires n > k
{
  var facN := Factorial(n);
  var facK := Factorial(k);
  var facNK := Factorial(n - k);
  c := facN / (facK * facNK);
}