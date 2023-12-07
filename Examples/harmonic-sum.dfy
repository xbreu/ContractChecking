// Created example

method NthHarmonicTerm(x : int) returns (c : int)
  // Calculates the nth harmonic term, equivalent
  // to the reciprocal of 1 / (x + 1)
  requires x >= 1
{
  if x < 0 {
    return 1 / 0;
  }
  return 1 / (x + 1);
}

method NthHarmonicNumber(n : int) returns (sum : int)
  requires n >= 0
{
  var i := 0;
  sum := 0;
  while i < n {
    var ith := NthHarmonicTerm(i);
    sum := sum + ith;
    i := i + 1;
  }
}