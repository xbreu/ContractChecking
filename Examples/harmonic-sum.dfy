// Created example

method NthHarmonic(x : int) returns (c : int)
  requires x >= 1
{
  c := 1;
  if x < 0 {
    c := -1;
  }
  return 1 / (c + 1);
}

method HarmonicSum(n : int) returns (r : int)
  requires n >= 0
{
  var n0 := NthHarmonic(n);
  var n1 := NthHarmonic(n + 1);
  r := n0 + n1;
}

// Found 4 fixes
// NthHarmonic:
// requires x >= 0
// requires !(x <= -1)
// HarmonicSum:
// requires n >= 1
// requires !(n <= 0)