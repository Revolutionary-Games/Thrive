// Common functions
shared const string& randomChoice(const array<string> &in source)
{
    return source[GetEngine().GetRandom().GetNumber(0,
            source.length() - 1)];
}

shared float sumTotalValuesInDictionary(const dictionary &in obj)
{
    const auto@ keys = obj.getKeys();

    float sum = 0;

    for(uint i = 0; i < keys.length(); ++i){
        float value;

        if(obj.get(keys[i], value)){
            sum += value;
        }
    }

    return sum;
}
