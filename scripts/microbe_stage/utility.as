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

shared void mergeDictionaries(dictionary &inout target, dictionary &in source)
{
    const auto@ keys = source.getKeys();

    for(uint i = 0; i < keys.length(); ++i){

        const string key = keys[i];

        float existing;

        const auto current = float(source[key]);

        if(!target.get(key, existing)){
            existing = 0;
        }

        target.set(key, existing + current);
    }
}

shared Quaternion createRotationForOrganelle(float rotation)
{
    return Quaternion(Float3(0, 1, 0), Degree(180))
        * Quaternion(Float3(0, -1, 0), Degree(rotation));
}

shared Quaternion createRotationForExternal(float angle)
{
    return Quaternion(Float3(0, 1, 0), Degree(180)) *
        Quaternion(Float3(0, 1, 0), Degree(angle));
}
