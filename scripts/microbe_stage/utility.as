// Common functions
shared const string& randomChoice(const array<string> &in source) {
    return source[GetEngine().GetRandom().GetNumber(0,
            source.length() - 1)];
}
