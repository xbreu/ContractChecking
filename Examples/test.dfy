method biggestOddDivisor(x : int)
  requires x > 0
{
  var current := x;
  while current % 2 == 0
    invariant current > 0
    decreases current
  {
    current := current / 2;
  }
  print "The input is a multiple of ", current, "\n";
}

method Main()
{
  biggestOddDivisor(179 * 32);
}