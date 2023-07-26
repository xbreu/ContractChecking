// Created example

method inverse(x : int) returns (i : int)
  requires x != 0
{
  i := 1 / x;
}

function sign(x : int) : int
{
  if x == 0
  then 0
  else if x > 0
  then 1
  else -1
}

method div(x : int, y : int) returns (d : int)
  requires y != 0
{
  var s := sign(y);
  var inv := inverse(s);
  d := x * inv;
}

method Enter(x : int)
  requires -x != 0
{
  var r := div(1, x);
}

// Found 12 fixes
// 1:
// inverse: requires (x == 1) || (x == -1)
// Enter  : requires x != 0
// div    : requires y != 0
// 2:
// inverse: requires (x == 1) || (x == -1)
// Enter  : requires x != 0
// div    : requires !(y == 0)
// 3:
// inverse: requires (x == 1) || (x == -1)
// Enter  : requires !(x == 0)
// div    : requires y != 0
// 4:
// inverse: requires (x == 1) || (x == -1)
// Enter  : requires !(x == 0)
// div    : requires !(y == 0)
// 5:
// inverse: requires x != 0 || !(x == 0)
// Enter  : requires x != 0
// div    : requires y != 0
// 6:
// inverse: requires x != 0 || !(x == 0)
// Enter  : requires x != 0
// div    : requires !(y == 0)
// 7:
// inverse: requires x != 0 || !(x == 0)
// Enter  : requires !(x == 0)
// div    : requires y != 0
// 8:
// inverse: requires x != 0 || !(x == 0)
// Enter  : requires !(x == 0)
// div    : requires !(y == 0)
// 9:
// Enter  : requires x != 0
// div    : requires y != 0
// 10:
// Enter  : requires x != 0
// div    : requires !(y == 0)
// 11:
// Enter  : requires !(x == 0)
// div    : requires y != 0
// 12:
// Enter  : requires !(x == 0)
// div    : requires !(y == 0)