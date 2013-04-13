#pragma once

namespace thrive {

class Variant {

public:

    enum Type : unsigned int {
        Null,
        Boolean,
        Integer,
        Double,
        Map,
        List,
        String
    };

};


template<Variant::Type TypeId>
struct VariantHelper {

    static const bool undefined = true;

    static const char* name = "";

    
};

}
