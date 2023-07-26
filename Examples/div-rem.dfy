function abs(n : int) : int {
 if n < 0 then -n else n
}
method divRem(d : int, n : int)
 returns (q : int, r : int)
 requires d >= 0
 requires n > 0
 ensures r + q * n == d
{
 if n == 0 {
  return 1 / 0, 0;
 }
 r := d;
 var m := abs(n);
 q := 0;
 while r >= m
 invariant r + q * m == d
 invariant q >= 0
 invariant m == abs(n)
 {
 q := q + 1;
 r := r - m;
 }
 if n < 0 {
 q := -q;
 }
}
method rem(d : int, n : int)
 returns (r : int)
 requires d >= 0
 requires n != 0
{
 var s_;
 s_, r := divRem(d, n);
}

// Found 4 fixes
// divRem:
// requires (d >= 0 && n > 0) || (n != 0)
// requires (d >= 0 && n > 0) || !(n == 0)
// rem:
// requires (d >= 0) && (n > 0) && (d < n)
// requires (d >= 0) && (n > 0) && !(d >= n)