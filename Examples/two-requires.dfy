// Adapted from Dafny Test programs

method A(x: int)
  requires x >= 0
  requires x <= 0
  ensures true
{}

method Enter(x: int) {
  A(x);
}

// Found 2 fixes
// Enter:
// requires x == 0
// requires !(x != 0)