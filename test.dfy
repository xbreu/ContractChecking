function abs(n : int) : int {
 if n < 0 then -n else n
}
method Faulty(d : int, n : int)
 returns (q : int, r : int)
 requires d >= 0
 requires n > 0
 ensures r + q * n == d
{
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
method Enter(d : int, n : int)
 returns (r : int)
 requires d >= 0
 requires n != 0
{
 var x;
 x, r := Faulty(d, n);
}