predicate isSorted(a: seq<int>) {
  forall i,j :: 0 <= i < j < |a| ==> a[i] <= a[j]
}

method BinarySearch(a: seq<int>, key: int) returns (r: int)
  requires isSorted(a)
  ensures 0 <= r ==> r < |a| && a[r] == key
  ensures r < 0 ==> key !in a[..]
{
  var lo, hi := 0, |a|;
  while lo < hi
    invariant 0 <= lo <= hi <= |a|
    invariant key !in a[..lo] && key !in a[hi..]
  {
    var mid := (lo + hi) / 2;
    if key < a[mid] {
      hi := mid;
    } else if a[mid] < key {
      lo := mid + 1;
    } else {
      return mid;
    }
  }
  return -1;
}

method Contains0(a: seq<int>) returns (r: bool)
  requires isSorted(a)
{
  var pos := BinarySearch(a, 0);
  r := pos >= 0;
}