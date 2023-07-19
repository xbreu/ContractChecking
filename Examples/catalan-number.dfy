// Translated from https://rosettacode.org/wiki/Catalan_numbers#Eiffel

method NthCatalanNumber(n : int) returns (r : real)
  // requires n >= 0
{
  if n == 0 {
    r := 1.0;
  } else {
    var t := (4 * n - 2) as real;
    var s := (n + 1) as real;
    var p := NthCatalanNumber(n - 1);
    r := t / s * p;
  }
}