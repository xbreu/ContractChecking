// Inspired by the Hanoi Tower problem

method move(n: int, from: int, dest: int, via: int)
  decreases n
  // requires n > 0
{
  if n != 1 {
    move(n - 1, from, via, dest);
    move(1, from, dest, via);
    move(n - 1, via, dest, from);
  }
}