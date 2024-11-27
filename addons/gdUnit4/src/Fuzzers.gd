## A fuzzer implementation to provide default implementation
class_name Fuzzers
extends Resource


## Generates an random string with min/max length and given charset
static func rand_str(min_length: int, max_length :int, charset := StringFuzzer.DEFAULT_CHARSET) -> Fuzzer:
	return StringFuzzer.new(min_length, max_length, charset)


## Generates an random integer in a range form to
static func rangei(from: int, to: int) -> Fuzzer:
	return IntFuzzer.new(from, to)

## Generates a randon float within in a given range
static func rangef(from: float, to: float) -> Fuzzer:
	return FloatFuzzer.new(from, to)

## Generates an random Vector2 in a range form to
static func rangev2(from: Vector2, to: Vector2) -> Fuzzer:
	return Vector2Fuzzer.new(from, to)


## Generates an random Vector3 in a range form to
static func rangev3(from: Vector3, to: Vector3) -> Fuzzer:
	return Vector3Fuzzer.new(from, to)

## Generates an integer in a range form to that can be divided exactly by 2
static func eveni(from: int, to: int) -> Fuzzer:
	return IntFuzzer.new(from, to, IntFuzzer.EVEN)

## Generates an integer in a range form to that cannot be divided exactly by 2
static func oddi(from: int, to: int) -> Fuzzer:
	return IntFuzzer.new(from, to, IntFuzzer.ODD)
