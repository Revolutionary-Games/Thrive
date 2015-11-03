#include "ogre/membrane_generation_system.h"

#include "engine/component_factory.h"
#include "engine/engine.h"
#include "engine/game_state.h"
#include "engine/entity.h"
#include "engine/entity_filter.h"
#include "engine/entity_manager.h"
#include "scripting/luabind.h"
#include "engine/serialization.h"
#include "OgreVector2.h"
#include "util/make_unique.h"

#include <vector>
#include <math.h>

using namespace thrive;


luabind::scope
MembraneGenerationComponent::luaBindings() {
    using namespace luabind;
    return class_<MembraneGenerationComponent, Component>("MembraneGenerationComponent")
        .enum_("ID") [
            value("TYPE_ID", MembraneGenerationComponent::TYPE_ID)
        ]
        .scope [
            def("TYPE_NAME", &MembraneGenerationComponent::TYPE_NAME)
        ]
        .def(constructor<>())
        //.def("dqwwqd", &MembraneGenerationComponent::dwqdqwd)
       // .def_readonly("aasasas", &MembraneGenerationComponent::assasa)
    ;
}


void
MembraneGenerationComponent::load(
    const StorageContainer& storage
) {
    Component::load(storage);
//    asdasdfew = storage.get<Ogre::String>("adsadasdfweq");
}


StorageContainer
MembraneGenerationComponent::storage() const {
    StorageContainer storage = Component::storage();
   // storage.set<Ogre::Quaternion>("sadasd", asdasdn);
    return storage;
}

void
MembraneGenerationComponent::addArea(
        float ,
        float
) {
    //areasToAdd.push_back(Ogre::Vector2(x,y));
}


REGISTER_COMPONENT(MembraneGenerationComponent)




luabind::scope
MembraneGenerationSystem::luaBindings() {
    using namespace luabind;
    return class_<MembraneGenerationSystem, System>("MembraneGenerationSystem")
        .def(constructor<>())
    ;
}


struct MembraneGenerationSystem::Implementation {
    int** table = new int*[256];
    int* tableSizes;
    int res;
    int size;
    int boxSize;
    double side;
    //int[] vertices = nullptr;
    std::vector<int> vertices;

    EntityFilter<MembraneGenerationComponent> m_entities = {true};
};


MembraneGenerationSystem::MembraneGenerationSystem()
  : m_impl(new Implementation())
{
}


MembraneGenerationSystem::~MembraneGenerationSystem() {}


void
MembraneGenerationSystem::init(
    GameState* gameState
) {
    System::init(gameState);

    m_impl->boxSize = 5;
    m_impl->res = 64;
    m_impl->side = 0.2;
    m_impl->size = 10;
    //m_impl->vertices = new float[res^3];
    m_impl->vertices.reserve(m_impl->res^3);
    int** table = m_impl->table;
    //for (int i=0b00000000; i<=0b11111111; i++){
   //     table[i] = {};
   // }
    m_impl->tableSizes = new int[256] {0,3,3,6,3,6,12,9,3,12,6,9,6,9,9,6,3,7,12,9,12,9,15,12,6,15,15,12,15,12,12,9,3,12,6,9,6,15,15,12,12,15,9,12,15,12,12,9,6,9,9,6,15,12,12,9,15,12,12,9,12,9,9,6,3,12,6,15,6,9,15,12,12,15,15,12,9,12,12,9,6,9,15,12,9,6,12,9,15,12,12,9,12,9,9,6,12,15,15,12,15,12,12,9,15,12,12,9,12,9,9,6,9,12,12,9,12,9,9,6,12,9,9,6,9,6,6,3,3,6,12,15,12,15,15,12,6,15,9,12,9,12,12,9,12,15,15,12,15,12,12,9,15,12,12,9,12,9,9,6,6,15,9,12,15,12,12,9,9,12,6,9,12,9,9,6,9,12,12,9,12,9,9,6,12,9,9,6,9,6,6,3,6,15,15,12,9,12,12,9,9,12,12,9,6,9,9,6,9,12,12,9,12,9,9,6,12,9,9,6,9,6,6,3,9,12,12,9,12,9,9,6,12,9,9,6,9,6,6,3,6,9,9,6,9,6,6,3,9,6,6,3,6,3,3,0};
    table[0b00000000] = new int[0] {};
    table[0b00000001] = new int[3] {0, 1, 2};
    table[0b00000010] = new int[3] {0, 4, 3};
    table[0b00000011] = new int[6] {1, 2, 4, 1, 4, 3};
    table[0b00000100] = new int[3] {1, 5, 6};
    table[0b00000101] = new int[6] {0, 5, 6, 0, 6, 2};
    table[0b00000110] = new int[12] {3, 5, 6, 3, 6, 4, 0, 4, 6, 0, 6, 1};
    table[0b00000111] = new int[9] {2, 4, 3, 2, 3, 5, 2, 5, 6};
    table[0b00001000] = new int[3] {3, 7, 5};
    table[0b00001001] = new int[12] {0, 3, 7, 0, 7, 2, 1, 2, 7, 1, 7, 5};
    table[0b00001010] = new int[6] {0, 4, 7, 0, 7, 5};
    table[0b00001011] = new int[9] {4, 7, 5, 4, 5, 1, 4, 1, 2};
    table[0b00001100] = new int[6] {1, 3, 7, 1, 7, 6};
    table[0b00001101] = new int[9] {6, 2, 0, 6, 0, 3, 6, 3, 7};
    table[0b00001110] = new int[9] {7, 6, 1, 7, 1, 0, 7, 0, 4};
    table[0b00001111] = new int[6] {2, 4, 7, 2, 7, 6};

    table[0b00010000] = new int[3] {2, 9, 8};
    table[0b00010001] = new int[7] {0, 1, 9, 0, 9, 8};
    table[0b00010010] = new int[12] {9, 8, 4, 9, 4, 3, 9, 3, 0, 9, 0, 2};
    table[0b00010011] = new int[9] {1, 9, 8, 1, 8, 4, 1, 4, 3};
    table[0b00010100] = new int[12] {5, 6, 9, 5, 9, 8, 5, 8, 2, 5, 2, 1};
    table[0b00010101] = new int[9] {0, 5, 6, 0, 6, 9, 0, 9, 8};
    table[0b00010110] = new int[15] {0, 2, 1, 3, 5, 6, 4, 9, 8, 3, 6, 9, 3, 9, 4};
    table[0b00010111] = new int[12] {3, 5, 6, 4, 9, 8, 3, 6, 9, 3, 9, 4};
    table[0b00011000] = new int[6] {3, 7, 5, 2, 9, 8};
    table[0b00011001] = new int[15] {7, 8, 0, 7, 0, 3, 7, 5, 1, 7, 1, 9, 7, 9, 8};
    table[0b00011010] = new int[15] {9, 8, 4, 9, 4, 7, 9, 7, 5, 9, 5, 0, 9, 0, 2};
    table[0b00011011] = new int[12] {9, 7, 5, 9, 5, 1, 7, 9, 8, 7, 8, 4};
    table[0b00011100] = new int[15] {8, 2, 1, 8, 1, 3, 8, 3, 7, 8, 7, 6, 8, 6, 9};
    table[0b00011101] = new int[12] {7, 6, 9, 7, 9, 8, 3, 7, 8, 3, 8, 0};
    table[0b00011110] = new int[12] {0, 2, 1, 7, 6, 9, 7, 9, 8, 7, 8, 4};
    table[0b00011111] = new int[9] {7, 6, 9, 7, 9, 8, 7, 8, 4};

    table[0b00100000] = new int[3] {4, 8, 10};
    table[0b00100001] = new int[12] {1, 2, 8, 1, 8, 10, 1, 10, 4, 1, 4, 0};
    table[0b00100010] = new int[6] {0, 8, 10, 0, 10, 3};
    table[0b00100011] = new int[9] {3, 1, 2, 3, 2, 8, 3, 8, 10};
    table[0b00100100] = new int[6] {4, 8, 10, 1, 5, 6};
    table[0b00100101] = new int[15] {10, 4, 0, 10, 0, 5, 10, 5, 6, 10, 6, 2, 10, 2, 8};
    table[0b00100110] = new int[15] {6, 1, 0, 6, 0, 8, 6, 8, 10, 6, 10, 3, 6, 3, 5};
    table[0b00100111] = new int[12] {10, 3, 5, 10, 5, 6, 8, 10, 6, 8, 6, 2};
    table[0b00101000] = new int[12] {5, 3, 4, 5, 4, 8, 5, 8, 10, 5, 10, 7};
    table[0b00101001] = new int[15] {0, 3, 4, 5, 1, 8, 5, 8, 10, 5, 10, 7, 1, 2, 8};
    table[0b00101010] = new int[9] {0, 8, 10, 0, 10, 7, 0, 7, 5};
    table[0b00101011] = new int[12] {5, 1, 8, 5, 8, 10, 5, 10, 7, 1, 2, 8};
    table[0b00101100] = new int[15] {8, 10, 7, 8, 7, 6, 8, 6, 1, 8, 1, 3, 8, 3, 4};
    table[0b00101101] = new int[12] {0, 3, 4, 6, 2, 8, 6, 8, 10, 6, 10, 7};
    table[0b00101110] = new int[12] {10, 7, 6, 10, 6, 8, 8, 6, 1, 8, 1, 0};
    table[0b00101111] = new int[9] {6, 2, 8, 6, 8, 10, 6, 10, 7};

    table[0b00110000] = new int[6] {2, 9, 10, 2, 10, 4};
    table[0b00110001] = new int[9] {9, 10, 4, 9, 4, 0, 9, 0, 1};
    table[0b00110010] = new int[9] {10, 3, 0, 10, 0, 2, 10, 2, 9};
    table[0b00110011] = new int[6] {1, 9, 10, 1, 10, 3};
    table[0b00110100] = new int[15] {5, 6, 9, 5, 9, 10, 5, 10, 4, 5, 4, 2, 5, 2, 1};
    table[0b00110101] = new int[12] {9, 10, 4, 9, 4, 0, 0, 5, 6, 0, 6, 9};
    table[0b00110110] = new int[12] {0, 2, 1, 10, 3, 5, 10, 5, 6, 10, 6, 9};
    table[0b00110111] = new int[9] {10, 3, 5, 10, 5, 6, 10, 6, 9};
    table[0b00111000] = new int[15] {5, 3, 4, 5, 4, 2, 5, 2, 9, 5, 9, 10, 5, 10, 7};
    table[0b00111001] = new int[12] {0, 3, 4, 9, 10, 7, 9, 7, 5, 9, 5, 1};
    table[0b00111010] = new int[12] {7, 5, 0, 7, 0, 10, 10, 0, 2, 10, 2, 9};
    table[0b00111011] = new int[9] {9, 10, 7, 9, 7, 5, 9, 5, 1};
    table[0b00111100] = new int[12] {1, 3, 4, 1, 4, 2, 6, 9, 10, 6, 10, 7};
    table[0b00111101] = new int[9] {0, 3, 4, 6, 9, 10, 6, 10, 7};
    table[0b00111110] = new int[9] {0, 2, 1, 6, 9, 10, 6, 10, 7};
    table[0b00111111] = new int[6] {6, 9, 10, 6, 10, 7};

    table[0b01000000] = new int[3] {6, 11, 9};
    table[0b01000001] = new int[12] {0, 1, 6, 0, 6, 11, 0, 11, 9, 0, 9, 2};
    table[0b01000010] = new int[6] {0, 4, 3, 6, 11, 9};
    table[0b01000011] = new int[15] {11, 9, 2, 11, 2, 4, 11, 4, 3, 11, 3, 1, 11, 1, 6};
    table[0b01000100] = new int[6] {1, 5, 11, 1, 11, 9};
    table[0b01000101] = new int[9] {5, 11, 9, 5, 9, 2, 5, 2, 0};
    table[0b01000110] = new int[15] {4, 3, 5, 4, 5, 11, 4, 11, 9, 4, 9, 1, 4, 1, 0};
    table[0b01000111] = new int[12] {5, 11, 9, 5, 9, 2, 2, 4, 3, 2, 3, 5};
    table[0b01001000] = new int[12] {3, 7, 11, 3, 11, 9, 3, 9, 6, 3, 6, 5};
    table[0b01001001] = new int[15] {1, 6, 5, 0, 3, 11, 0, 11, 9, 0, 9, 2, 3, 7, 11};
    table[0b01001010] = new int[15] {9, 6, 5, 9, 5, 0, 9, 0, 4, 9, 4, 7, 9, 7, 11};
    table[0b01001011] = new int[12] {1, 6, 5, 4, 7, 11, 4, 11, 9, 4, 9, 2};
    table[0b01001100] = new int[9] {1, 3, 7, 1, 7, 11, 1, 11, 9};
    table[0b01001101] = new int[12] {0, 3, 11, 0, 11, 9, 0, 9, 2, 3, 7, 11};
    table[0b01001110] = new int[12] {4, 7, 11, 4, 11, 9, 9, 1, 0, 9, 0, 4};
    table[0b01001111] = new int[9] {4, 7, 11, 4, 11, 9, 4, 9, 2};

    table[0b01010000] = new int[6] {2, 6, 11, 2, 11, 8};
    table[0b01010001] = new int[9] {8, 0, 1, 8, 1, 6, 8, 6, 11};
    table[0b01010010] = new int[15] {3, 0, 2, 3, 2, 6, 3, 6, 11, 3, 11, 8, 3, 8, 4};
    table[0b01010011] = new int[12] {4, 3, 1, 4, 1, 8, 8, 1, 6, 8, 6, 11};
    table[0b01010100] = new int[9] {11, 8, 2, 11, 2, 1, 11, 1, 5};
    table[0b01010101] = new int[6] {0, 5, 11, 0, 11, 8};
    table[0b01010110] = new int[12] {0, 2, 1, 11, 8, 4, 11, 4, 3, 11, 3, 5};
    table[0b01010111] = new int[9] {11, 8, 4, 11, 4, 3, 11, 3, 5};
    table[0b01011000] = new int[15] {3, 7, 11, 3, 11, 8, 3, 8, 2, 3, 2, 6, 3, 6, 5};
    table[0b01011001] = new int[12] {1, 6, 5, 8, 0, 3, 8, 3, 7, 8, 7, 11};
    table[0b01011010] = new int[12] {0, 2, 6, 0, 6, 5, 4, 7, 11, 4, 11, 8};
    table[0b01011011] = new int[9] {1, 6, 5, 4, 7, 11, 4, 11, 8};
    table[0b01011100] = new int[12] {11, 8, 2, 11, 2, 1, 7, 11, 1, 7, 1, 3};
    table[0b01011101] = new int[9] {8, 0, 3, 8, 3, 7, 8, 7, 11};
    table[0b01011110] = new int[9] {0, 2, 1, 4, 7, 11, 4, 11, 8};
    table[0b01011111] = new int[6] {4, 7, 11, 4, 11, 8};

    table[0b01100000] = new int[12] {4, 8, 9, 4, 9, 6, 4, 6, 11, 4, 11, 10};
    table[0b01100001] = new int[15] {2, 8, 9, 0, 1, 11, 0, 11, 10, 0, 10, 4, 1, 6, 11};
    table[0b01100010] = new int[15] {6, 11, 10, 6, 10, 3, 6, 3, 0, 6, 0, 8, 6, 8, 9};
    table[0b01100011] = new int[12] {2, 8, 9, 3, 1, 6, 3, 6, 11, 3, 11, 10};
    table[0b01100100] = new int[15] {4, 8, 9, 4, 9, 1, 4, 1, 5, 4, 5, 11, 4, 11, 10};
    table[0b01100101] = new int[12] {2, 8, 9, 5, 11, 10, 5, 10, 4, 5, 4, 0};
    table[0b01100110] = new int[12] {0, 8, 9, 0, 9, 1, 3, 5, 11, 3, 11, 10};
    table[0b01100111] = new int[9] {2, 8, 9, 3, 5, 11, 3, 11, 10};
    table[0b01101000] = new int[15] {7, 11, 10, 5, 3, 8, 5, 8, 9, 5, 9, 6, 3, 4, 8};
    table[0b01101001] = new int[12] {0, 3, 4, 1, 6, 5, 2, 8, 9, 7, 11, 10};
    table[0b01101010] = new int[12] {7, 11, 10, 0, 8, 9, 0, 9, 6, 0, 6, 5};
    table[0b01101011] = new int[9] {1, 6, 5, 2, 8, 9, 7, 11, 10};
    table[0b01101100] = new int[12] {7, 11, 10, 1, 3, 4, 1, 4, 8, 1, 8, 9};
    table[0b01101101] = new int[9] {0, 3, 4, 2, 8, 9, 7, 11, 10};
    table[0b01101110] = new int[9] {7, 11, 10, 0, 8, 9, 0, 9, 1};
    table[0b01101111] = new int[6] {2, 8, 9, 7, 11, 10};

    table[0b01110000] = new int[9] {2, 6, 11, 2, 11, 10, 2, 10, 4};
    table[0b01110001] = new int[12] {0, 1, 11, 0, 11, 10, 0, 10, 4, 1, 6, 11};
    table[0b01110010] = new int[12] {2, 6, 11, 2, 11, 10, 0, 2, 10, 0, 10, 3};
    table[0b01110011] = new int[9] {3, 1, 6, 3, 6, 11, 3, 11, 10};
    table[0b01110100] = new int[12] {10, 4, 2, 10, 2, 11, 11, 2, 1, 11, 1, 5};
    table[0b01110101] = new int[9] {5, 11, 10, 5, 10, 4, 5, 4, 0};
    table[0b01110110] = new int[9] {0, 2, 1, 3, 5, 11, 3, 11, 10};
    table[0b01110111] = new int[6] {3, 5, 11, 3, 11, 10};
    table[0b01111000] = new int[12] {7, 11, 10, 2, 6, 5, 2, 5, 3, 2, 3, 4};
    table[0b01111001] = new int[9] {0, 3, 4, 1, 6, 5, 7, 11, 10};
    table[0b01111010] = new int[9] {0, 2, 6, 0, 6, 5, 7, 11, 10};
    table[0b01111011] = new int[6] {1, 6, 5, 7, 11, 10};
    table[0b01111100] = new int[9] {1, 3, 4, 1, 4, 2, 7, 11, 10};
    table[0b01111101] = new int[6] {0, 3, 4, 7, 11, 10};
    table[0b01111110] = new int[6] {0, 2, 1, 7, 11, 10};
    table[0b01111111] = new int[3] {7, 11, 10};

    table[0b10000000] = new int[3] {7, 10, 11};
    table[0b10000001] = new int[6] {0, 1, 2, 7, 10, 11};
    table[0b10000010] = new int[12] {0, 4, 10, 0, 10, 11, 0, 11, 7, 0, 7, 3};
    table[0b10000011] = new int[15] {11, 7, 3, 11, 3, 1, 11, 1, 2, 11, 2, 4, 11, 4, 10};
    table[0b10000100] = new int[12] {1, 5, 7, 1, 7, 10, 1, 10, 11, 1, 11, 6};
    table[0b10000101] = new int[15] {10, 11, 6, 10, 6, 2, 10, 2, 0, 10, 0, 5, 10, 5, 7};
    table[0b10000110] = new int[15] {3, 5, 7, 1, 0, 10, 1, 10, 11, 1, 11, 6, 0, 4, 10};
    table[0b10000111] = new int[12] {3, 5, 7, 2, 4, 10, 2, 10, 11, 2, 11, 6};
    table[0b10001000] = new int[6] {3, 10, 11, 3, 11, 5};
    table[0b10001001] = new int[15] {2, 0, 3, 2, 3, 10, 2, 10, 11, 2, 11, 5, 2, 5, 1};
    table[0b10001010] = new int[9] {5, 0, 4, 5, 4, 10, 5, 10, 11};
    table[0b10001011] = new int[12] {2, 4, 10, 2, 10, 11, 1, 2, 11, 1, 11, 5};
    table[0b10001100] = new int[9] {3, 10, 11, 3, 11, 6, 3, 6, 1};
    table[0b10001101] = new int[12] {10, 2, 0, 10, 0, 3, 2, 10, 11, 2, 11, 6};
    table[0b10001110] = new int[12] {1, 0, 10, 1, 10, 11, 1, 11, 6, 0, 4, 10};
    table[0b10001111] = new int[9] {2, 4, 10, 2, 10, 11, 2, 11, 6};

    table[0b10010000] = new int[12] {2, 9, 11, 2, 11, 7, 2, 7, 10, 2, 10, 8};
    table[0b10010001] = new int[15] {7, 10, 8, 7, 8, 0, 7, 0, 1, 7, 1, 9, 7, 9, 11};
    table[0b10010010] = new int[15] {4, 10, 8, 3, 0, 9, 3, 9, 11, 3, 11, 7, 0, 2, 9};
    table[0b10010011] = new int[12] {4, 10, 8, 1, 9, 11, 1, 11, 7, 1, 7, 3};
    table[0b10010100] = new int[15] {6, 9, 11, 1, 5, 10, 1, 10, 8, 1, 8, 2, 5, 7, 10};
    table[0b10010101] = new int[12] {6, 9, 11, 0, 5, 7, 0, 7, 10, 0, 10, 8};
    table[0b10010110] = new int[12] {0, 2, 1, 3, 5, 7, 4, 10, 8, 6, 9, 11};
    table[0b10010111] = new int[9] {3, 5, 7, 4, 10, 8, 6, 9, 11};
    table[0b10011000] = new int[15] {2, 9, 11, 2, 11, 5, 2, 5, 3, 2, 3, 10, 2, 10, 8};
    table[0b10011001] = new int[12] {0, 3, 10, 0, 10, 8, 1, 9, 11, 1, 11, 5};
    table[0b10011010] = new int[12] {4, 10, 8, 5, 0, 2, 5, 2, 9, 5, 9, 11};
    table[0b10011011] = new int[9] {4, 10, 8, 1, 9, 11, 1, 11, 5};
    table[0b10011100] = new int[12] {6, 9, 11, 3, 10, 8, 3, 8, 2, 3, 2, 1};
    table[0b10011101] = new int[9] {6, 9, 11, 0, 3, 10, 0, 10, 8};
    table[0b10011110] = new int[9] {0, 2, 1, 4, 10, 8, 6, 9, 11};
    table[0b10011111] = new int[6] {4, 10, 8, 6, 9, 11};

    table[0b10100000] = new int[6] {4, 8, 11, 4, 11, 7};
    table[0b10100001] = new int[15] {1, 2, 8, 1, 8, 11, 1, 11, 7, 1, 7, 4, 1, 4, 0};
    table[0b10100010] = new int[9] {8, 11, 7, 8, 7, 3, 8, 3, 0};
    table[0b10100011] = new int[12] {11, 1, 2, 11, 2, 8, 7, 3, 1, 7, 1, 11};
    table[0b10100100] = new int[15] {1, 5, 7, 1, 7, 4, 1, 4, 8, 1, 8, 11, 1, 11, 6};
    table[0b10100101] = new int[12] {0, 5, 7, 0, 7, 4, 2, 8, 11, 2, 11, 6};
    table[0b10100110] = new int[12] {3, 5, 7, 8, 11, 6, 8, 6, 1, 8, 1, 0};
    table[0b10100111] = new int[9] {3, 5, 7, 2, 8, 11, 2, 11, 6};
    table[0b10101000] = new int[9] {11, 5, 3, 11, 3, 4, 11, 4, 8};
    table[0b10101001] = new int[12] {0, 3, 4, 11, 5, 1, 11, 1, 2, 11, 2, 8};
    table[0b10101010] = new int[6] {0, 8, 11, 0, 11, 5};
    table[0b10101011] = new int[9] {11, 5, 1, 11, 1, 2, 11, 2, 8};
    table[0b10101100] = new int[12] {1, 3, 4, 1, 4, 8, 8, 11, 6, 8, 6, 1};
    table[0b10101101] = new int[9] {0, 3, 4, 2, 8, 11, 2, 11, 6};
    table[0b10101110] = new int[9] {8, 11, 6, 8, 6, 1, 8, 1, 0};
    table[0b10101111] = new int[6] {2, 8, 11, 2, 11, 6};

    table[0b10110000] = new int[9] {4, 2, 9, 4, 9, 11, 4, 11, 7};
    table[0b10110001] = new int[12] {7, 4, 0, 7, 0, 1, 1, 9, 11, 1, 11, 7};
    table[0b10110010] = new int[12] {3, 0, 9, 3, 9, 11, 3, 11, 7, 0, 2, 9};
    table[0b10110011] = new int[9] {1, 9, 11, 1, 11, 7, 1, 7, 3};
    table[0b10110100] = new int[12] {6, 9, 11, 4, 2, 1, 4, 1, 5, 4, 5, 7};
    table[0b10110101] = new int[9] {6, 9, 11, 0, 5, 7, 0, 7, 4};
    table[0b10110110] = new int[9] {0, 2, 1, 3, 5, 7, 6, 9, 11};
    table[0b10110111] = new int[6] {3, 5, 7, 6, 9, 11};
    table[0b10111000] = new int[12] {3, 4, 2, 3, 2, 5, 9, 11, 5, 9, 5, 2};
    table[0b10111001] = new int[9] {0, 3, 4, 1, 9, 11, 1, 11, 5};
    table[0b10111010] = new int[9] {5, 0, 2, 5, 2, 9, 5, 9, 11};
    table[0b10111011] = new int[6] {1, 9, 11, 1, 11, 5};
    table[0b10111100] = new int[9] {6, 9, 11, 1, 3, 4, 1, 4, 2};
    table[0b10111101] = new int[6] {0, 3, 4, 6, 9, 11};
    table[0b10111110] = new int[6] {0, 2, 1, 6, 9, 11};
    table[0b10111111] = new int[3] {6, 9, 11};

    table[0b11000000] = new int[6] {6, 7, 10, 6, 10, 9};
    table[0b11000001] = new int[15] {0, 1, 6, 0, 6, 7, 0, 7, 10, 0, 10, 9, 0, 9, 2};
    table[0b11000010] = new int[15] {0, 4, 10, 0, 10, 9, 0, 9, 6, 0, 6, 7, 0, 7, 3};
    table[0b11000011] = new int[12] {1, 6, 7, 1, 7, 3, 2, 4, 10, 2, 10, 9};
    table[0b11000100] = new int[9] {9, 1, 5, 9, 5, 7, 9, 7, 10};
    table[0b11000101] = new int[12] {10, 9, 2, 10, 2, 0, 0, 5, 7, 0, 7, 10};
    table[0b11000110] = new int[12] {3, 5, 7, 9, 1, 0, 9, 0, 4, 9, 4, 10};
    table[0b11000111] = new int[9] {3, 5, 7, 2, 4, 10, 2, 10, 9};
    table[0b11001000] = new int[9] {10, 9, 6, 10, 6, 5, 10, 5, 3};
    table[0b11001001] = new int[12] {1, 6, 5, 10, 9, 2, 10, 2, 0, 10, 0, 3};
    table[0b11001010] = new int[12] {10, 9, 6, 10, 6, 5, 5, 0, 4, 5, 4, 10};
    table[0b11001011] = new int[9] {1, 6, 5, 2, 4, 10, 2, 10, 9};
    table[0b11001100] = new int[6] {1, 3, 10, 1, 10, 9};
    table[0b11001101] = new int[9] {10, 9, 2, 10, 2, 0, 10, 0, 3};
    table[0b11001110] = new int[9] {9, 1, 0, 9, 0, 4, 9, 4, 10};
    table[0b11001111] = new int[6] {2, 4, 10, 2, 10, 9};

    table[0b11010000] = new int[9] {6, 7, 10, 6, 10, 8, 6, 8, 2};
    table[0b11010001] = new int[12] {8, 0, 1, 8, 1, 6, 10, 8, 6, 10, 6, 7};
    table[0b11010010] = new int[12] {4, 10, 8, 6, 7, 3, 6, 3, 0, 6, 0, 2};
    table[0b11010011] = new int[9] {4, 10, 8, 1, 6, 7, 1, 7, 3};
    table[0b11010100] = new int[12] {1, 5, 10, 1, 10, 8, 1, 8, 2, 5, 7, 10};
    table[0b11010101] = new int[9] {0, 5, 7, 0, 7, 10, 0, 10, 8};
    table[0b11010110] = new int[9] {0, 2, 1, 3, 5, 7, 4, 10, 8};
    table[0b11010111] = new int[6] {3, 5, 7, 4, 10, 8};
    table[0b11011000] = new int[12] {8, 2, 6, 8, 6, 10, 10, 6, 5, 10, 5, 3};
    table[0b11011001] = new int[9] {1, 6, 5, 0, 3, 10, 0, 10, 8};
    table[0b11011010] = new int[9] {4, 10, 8, 0, 2, 6, 0, 6, 5};
    table[0b11011011] = new int[6] {1, 6, 5, 4, 10, 8};
    table[0b11011100] = new int[9] {3, 10, 8, 3, 8, 2, 3, 2, 1};
    table[0b11011101] = new int[6] {0, 3, 10, 0, 10, 8};
    table[0b11011110] = new int[6] {0, 2, 1, 4, 10, 8};
    table[0b11011111] = new int[3] {4, 10, 8};

    table[0b11100000] = new int[9] {7, 4, 8, 7, 8, 9, 7, 9, 6};
    table[0b11100001] = new int[12] {2, 8, 9, 7, 4, 0, 7, 0, 1, 7, 1, 6};
    table[0b11100010] = new int[12] {9, 6, 7, 9, 7, 8, 8, 7, 3, 8, 3, 0};
    table[0b11100011] = new int[9] {2, 8, 9, 1, 6, 7, 1, 7, 3};
    table[0b11100100] = new int[12] {8, 9, 1, 8, 1, 4, 4, 1, 5, 4, 5, 7};
    table[0b11100101] = new int[9] {2, 8, 9, 0, 5, 7, 0, 7, 4};
    table[0b11100110] = new int[9] {3, 5, 7, 0, 8, 9, 0, 9, 1};
    table[0b11100111] = new int[6] {2, 8, 9, 3, 5, 7};
    table[0b11101000] = new int[12] {5, 3, 8, 5, 8, 9, 5, 9, 6, 3, 4, 8};
    table[0b11101001] = new int[9] {0, 3, 4, 1, 6, 5, 2, 8, 9};
    table[0b11101010] = new int[9] {0, 8, 9, 0, 9, 6, 0, 6, 5};
    table[0b11101011] = new int[6] {1, 6, 5, 2, 8, 9};
    table[0b11101100] = new int[9] {1, 3, 4, 1, 4, 8, 1, 8, 9};
    table[0b11101101] = new int[6] {0, 3, 4, 2, 8, 9};
    table[0b11101110] = new int[6] {0, 8, 9, 0, 9, 1};
    table[0b11101111] = new int[3] {2, 8, 9};

    table[0b11110000] = new int[6] {2, 6, 7, 2, 7, 4};
    table[0b11110001] = new int[9] {7, 4, 0, 7, 0, 1, 7, 1, 6};
    table[0b11110010] = new int[9] {6, 7, 3, 6, 3, 0, 6, 0, 2};
    table[0b11110011] = new int[6] {1, 6, 7, 1, 7, 3};
    table[0b11110100] = new int[9] {4, 2, 1, 4, 1, 5, 4, 5, 7};
    table[0b11110101] = new int[6] {0, 5, 7, 0, 7, 4};
    table[0b11110110] = new int[6] {0, 2, 1, 3, 5, 7};
    table[0b11110111] = new int[3] {3, 5, 7};
    table[0b11111000] = new int[9] {2, 6, 5, 2, 5, 3, 2, 3, 4};
    table[0b11111001] = new int[6] {0, 3, 4, 1, 6, 5};
    table[0b11111010] = new int[6] {0, 2, 6, 0, 6, 5};
    table[0b11111011] = new int[3] {1, 6, 5};
    table[0b11111100] = new int[6] {1, 3, 4, 1, 4, 2};
    table[0b11111101] = new int[3] {0, 3, 4};
    table[0b11111110] = new int[3] {0, 2, 1};
    table[0b11111111] = new int[0] {};



}


void
MembraneGenerationSystem::shutdown() {
    System::shutdown();
}

static double sqrt34 = sqrt(3)/2;
//Helper functions defined below
double getVal(double, double, double, int, double);
void includeVertex(std::vector<int>&, double*** , double, int, int, int, int, int, double, double, double, double);
void getDeriv(double, int, double, double, double, double*);
void vert(std::vector<int>&, double, int, double, double, double, double, double, double, double);

void
MembraneGenerationSystem::update(
    int ,
    int
) {
    // for (EntityId entityId : m_impl->m_entities.removedEntities()) {

 //   }
  //  for (const auto& added : m_impl->m_entities.addedEntities()) {

 //   }
    double*** density;
    for (const auto& entry : m_impl->m_entities) {
        MembraneGenerationComponent* cembraneComponent = std::get<0>(entry.second);
        //Now add areas to the membranes
        for (auto coord : cembraneComponent->areasToAdd)
        {
            std::cout << coord << std::endl;
            //Get ready to generate mesh
            int res = m_impl->res;
            int boxSize = m_impl->boxSize;
            //The awkward way we have to dynamically allocate multidimensional arrays in c++
            density = new double**[res+1];
            for (int i = 0; i < res+1; ++i){
                density[i] = new double*[res+1];
                for (int j = 0; j < res+1; ++j){
                    density[i][j] = new double[res+1];
                }
            }

            for (int i=0; i<=res; i++) {
                for (int j=0; j<=res; j++){
                    for (int k=0; k<=res; k++)
                    {
                        density[i][j][k] = getVal((static_cast<double>(i)/res*2-1)*boxSize, (static_cast<double>(j)/res*2-1)*boxSize, (static_cast<double>(k)/res*2-1)*boxSize, m_impl->size, m_impl->side);
                    }
                }
            }

            double x1, y1, z1, s2;
            int* type;

            for (int i=0; i<res; i++)
            {
                for (int j=0; j<res; j++)
                    for (int k=0; k<res; k++)
                    {
                        x1 = (static_cast<double>(i)/res*2-1)*boxSize;
                        y1 = (static_cast<double>(j)/res*2-1)*boxSize;
                        z1 = (static_cast<double>(k)/res*2-1)*boxSize;
                        s2 = 2.0/res*boxSize;

                        int verticeCount = 0;
                        if (density[i][j][k] > 0) verticeCount += 1;
                        if (density[i+1][j][k] > 0) verticeCount += 2;
                        if (density[i][j+1][k] > 0) verticeCount += 4;
                        if (density[i+1][j+1][k] > 0) verticeCount += 8;
                        if (density[i][j][k+1] > 0) verticeCount += 16;
                        if (density[i+1][j][k+1] > 0) verticeCount += 32;
                        if (density[i][j+1][k+1] > 0) verticeCount += 64;
                        if (density[i+1][j+1][k+1] > 0) verticeCount += 128;

                        type = m_impl->table[verticeCount];

                        for (int l=0; l < m_impl->tableSizes[verticeCount]; l++) {
                            includeVertex(m_impl->vertices, density, m_impl->side, m_impl->size, type[l], i, j, k, x1, y1, z1, s2);
                        }
                    }
            }

        }
        /*if (component->m_visible.hasChanges()) {
            component->m_visible.untouch();
        }*/

    }
}



// Helper functions
double getVal(
    double x,
    double y,
    double z,
    int size,
    double side
){
    double val = 0;
    double cutoff = side;
    double sqrCutoff = cutoff*cutoff;
    double sqrSeparate = 3*side*side;
    double separate = sqrt(sqrSeparate);

    for (int q=-size; q<=size; q++)
        for (int r=-size; r<=size; r++)
        {
          //  if (hex[q+size][r+size])
         //   {
                double xx = q*1.5*side, yy = (2*r+q)*side*sqrt34;
                double dist = pow(x-xx,2) + pow(y-yy,2) + pow(z,2) - sqrCutoff;
                if (dist < sqrSeparate/4)
                    val += 0.5 + (sqrSeparate/4 - dist) / (sqrSeparate/2);
                else if (dist < sqrSeparate)
                {
                    dist = pow(separate-sqrt(dist),2);
                    val += 0.5 - (sqrSeparate/4 - dist) / (sqrSeparate/2);
                }
          //  }
        }
    return val-1;
}

void getDeriv(double side, int size, double x, double y, double z, double* deriv)
{
    deriv[0] = 0; deriv[1] = 0; deriv[2] = 0;
    deriv[0] = getVal(x, y, z, size, side) - getVal(x+0.01, y, z, size, side);
    deriv[1] = getVal(x, y, z, size, side) - getVal(x, y+0.01, z, size, side);
    deriv[2] = getVal(x, y, z, size, side) - getVal(x, y, z+0.01, size, side);
}


void vert(std::vector<int>& vertices, double side, int size, double x, double y, double z, double xmod, double ymod, double zmod, double s2){
    if (xmod < 0 || xmod > 1 || ymod < 0 || ymod > 1 || zmod < 0 || zmod > 1)
        throw std::invalid_argument("Out of range");
    double* deriv = new double[3];
    getDeriv(side, size, x + s2*xmod, y + s2*ymod, z + s2*zmod, deriv);
    double nx = deriv[0], ny = deriv[1], nz = deriv[2], n = sqrt(nx*nx + ny*ny + nz*nz);
    if (n <= 0){
        n = 1;
    }

    vertices.push_back(static_cast<float>(x + s2*xmod));   // posx
    vertices.push_back(static_cast<float>(y + s2*ymod));   // posy
    vertices.push_back(static_cast<float>(z + s2*zmod));   // posz
    vertices.push_back(static_cast<float>(nx/n));          // normal
    vertices.push_back(static_cast<float>(ny/n));          // normal
    vertices.push_back(static_cast<float>(nz/n));          // normal

}

void includeVertex(std::vector<int>& vertices, double*** density, double side, int size, int t, int i, int j, int k, double x1, double y1, double z1, double s2){
    if (t == 0)
        vert(vertices, side, size, x1, y1, z1, density[i][j][k] / (density[i][j][k] - density[i+1][j][k]), 0, 0, s2);
    else if (t == 1)
        vert(vertices, side, size, x1, y1, z1, 0, density[i][j][k] / (density[i][j][k] - density[i][j+1][k]), 0, s2);
    else if (t == 2)
        vert(vertices, side, size, x1, y1, z1, 0, 0, density[i][j][k] / (density[i][j][k] - density[i][j][k+1]), s2);
    else if (t == 3)
        vert(vertices, side,size, x1+s2, y1, z1, 0, density[i+1][j][k] / (density[i+1][j][k] - density[i+1][j+1][k]), 0, s2);
    else if (t == 4)
        vert(vertices, side,size, x1+s2, y1, z1, 0, 0, density[i+1][j][k] / (density[i+1][j][k] - density[i+1][j][k+1]), s2);
    else if (t == 5)
        vert(vertices, side,size, x1, y1+s2, z1, density[i][j+1][k] / (density[i][j+1][k] - density[i+1][j+1][k]), 0, 0, s2);
    else if (t == 6)
        vert(vertices, side,size, x1, y1+s2, z1, 0, 0, density[i][j+1][k] / (density[i][j+1][k] - density[i][j+1][k+1]), s2);
    else if (t == 7)
        vert(vertices, side,size, x1+s2, y1+s2, z1, 0, 0, density[i+1][j+1][k] / (density[i+1][j+1][k] - density[i+1][j+1][k+1]), s2);
    else if (t == 8)
        vert(vertices, side,size, x1, y1, z1+s2, density[i][j][k+1] / (density[i][j][k+1] - density[i+1][j][k+1]), 0, 0, s2);
    else if (t == 9)
        vert(vertices, side,size, x1, y1, z1+s2, 0, density[i][j][k+1] / (density[i][j][k+1] - density[i][j+1][k+1]), 0, s2);
    else if (t == 10)
        vert(vertices, side,size, x1+s2, y1, z1+s2, 0, density[i+1][j][k+1] / (density[i+1][j][k+1] - density[i+1][j+1][k+1]), 0, s2);
    else if (t == 11)
        vert(vertices, side,size, x1, y1+s2, z1+s2, density[i][j+1][k+1] / (density[i][j+1][k+1] - density[i+1][j+1][k+1]), 0, 0, s2);
}




