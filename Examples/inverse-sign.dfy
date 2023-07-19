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
  // requires y > 0
  ensures y > 0
{
  if (y <= 0) {
    return 0;
  }
  var s := sign(y);
  var inv := inverse(s);
  d := x * inv;
}

method Enter(x : int)
{
  var r := div(1, x);
}