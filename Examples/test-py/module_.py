import sys
from typing import Callable, Any, TypeVar, NamedTuple
from math import floor
from itertools import count

import module_
import _dafny
import System_

assert "module_" == __name__
module_ = sys.modules[__name__]

class default__:
    def  __init__(self):
        pass

    def __dafnystr__(self) -> str:
        return "_module._default"
    @staticmethod
    def biggestOddDivisor(x):
        d_0_current_: int
        d_0_current_ = x
        while (_dafny.euclidian_modulus(d_0_current_, 2)) == (0):
            d_0_current_ = _dafny.euclidian_division(d_0_current_, 2)
        _dafny.print((_dafny.SeqWithoutIsStrInference(map(_dafny.CodePoint, "The input is a multiple of "))).VerbatimString(False))
        _dafny.print(_dafny.string_of(d_0_current_))
        _dafny.print((_dafny.SeqWithoutIsStrInference(map(_dafny.CodePoint, "\n"))).VerbatimString(False))

    @staticmethod
    def Main(noArgsParameter__):
        module_.default__.biggestOddDivisor((179) * (32))

