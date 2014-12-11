#include "ogre/membrane_generation_system.h"

#include "engine/engine.h"
#include "engine/game_state.h"
#include "scripting/luabind.h"


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
        .def("dqwwqd", &MembraneGenerationComponent::dwqdqwd)
        .def_readonly("aasasas", &MembraneGenerationComponent::assasa)
    ;
}


void
MembraneGenerationComponent::load(
    const StorageContainer& storage
) {
    Component::load(storage);
    asdasdfew = storage.get<Ogre::String>("adsadasdfweq");
}


StorageContainer
MembraneGenerationComponent::storage() const {
    StorageContainer storage = Component::storage();
    storage.set<Ogre::Quaternion>("sadasd", asdasdn);
    return storage;
}

void
MembraneGenerationComponent::asdasdas(
    float sadasdas
) {
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
    private int[][] table;
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






	public static void init()
	{
	    int[][] table = m_impl->table;
		for (int i=0b00000000; i<=0b11111111; i++)
			table[i] = new int[] {};

		table[0b00000000] = new int[] {};
		table[0b00000001] = new int[] {0, 1, 2};
		table[0b00000010] = new int[] {0, 4, 3};
		table[0b00000011] = new int[] {1, 2, 4, 1, 4, 3};
		table[0b00000100] = new int[] {1, 5, 6};
		table[0b00000101] = new int[] {0, 5, 6, 0, 6, 2};
		table[0b00000110] = new int[] {3, 5, 6, 3, 6, 4, 0, 4, 6, 0, 6, 1};
		table[0b00000111] = new int[] {2, 4, 3, 2, 3, 5, 2, 5, 6};
		table[0b00001000] = new int[] {3, 7, 5};
		table[0b00001001] = new int[] {0, 3, 7, 0, 7, 2, 1, 2, 7, 1, 7, 5};
		table[0b00001010] = new int[] {0, 4, 7, 0, 7, 5};
		table[0b00001011] = new int[] {4, 7, 5, 4, 5, 1, 4, 1, 2};
		table[0b00001100] = new int[] {1, 3, 7, 1, 7, 6};
		table[0b00001101] = new int[] {6, 2, 0, 6, 0, 3, 6, 3, 7};
		table[0b00001110] = new int[] {7, 6, 1, 7, 1, 0, 7, 0, 4};
		table[0b00001111] = new int[] {2, 4, 7, 2, 7, 6};

		table[0b00010000] = new int[] {2, 9, 8};
		table[0b00010001] = new int[] {0, 1, 9, 0, 9, 8};
		table[0b00010010] = new int[] {9, 8, 4, 9, 4, 3, 9, 3, 0, 9, 0, 2};
		table[0b00010011] = new int[] {1, 9, 8, 1, 8, 4, 1, 4, 3};
		table[0b00010100] = new int[] {5, 6, 9, 5, 9, 8, 5, 8, 2, 5, 2, 1};
		table[0b00010101] = new int[] {0, 5, 6, 0, 6, 9, 0, 9, 8};
		table[0b00010110] = new int[] {0, 2, 1, 3, 5, 6, 4, 9, 8, 3, 6, 9, 3, 9, 4};
		table[0b00010111] = new int[] {3, 5, 6, 4, 9, 8, 3, 6, 9, 3, 9, 4};
		table[0b00011000] = new int[] {3, 7, 5, 2, 9, 8};
		table[0b00011001] = new int[] {7, 8, 0, 7, 0, 3, 7, 5, 1, 7, 1, 9, 7, 9, 8};
		table[0b00011010] = new int[] {9, 8, 4, 9, 4, 7, 9, 7, 5, 9, 5, 0, 9, 0, 2};
		table[0b00011011] = new int[] {9, 7, 5, 9, 5, 1, 7, 9, 8, 7, 8, 4};
		table[0b00011100] = new int[] {8, 2, 1, 8, 1, 3, 8, 3, 7, 8, 7, 6, 8, 6, 9};
		table[0b00011101] = new int[] {7, 6, 9, 7, 9, 8, 3, 7, 8, 3, 8, 0};
		table[0b00011110] = new int[] {0, 2, 1, 7, 6, 9, 7, 9, 8, 7, 8, 4};
		table[0b00011111] = new int[] {7, 6, 9, 7, 9, 8, 7, 8, 4};

		table[0b00100000] = new int[] {4, 8, 10};
		table[0b00100001] = new int[] {1, 2, 8, 1, 8, 10, 1, 10, 4, 1, 4, 0};
		table[0b00100010] = new int[] {0, 8, 10, 0, 10, 3};
		table[0b00100011] = new int[] {3, 1, 2, 3, 2, 8, 3, 8, 10};
		table[0b00100100] = new int[] {4, 8, 10, 1, 5, 6};
		table[0b00100101] = new int[] {10, 4, 0, 10, 0, 5, 10, 5, 6, 10, 6, 2, 10, 2, 8};
		table[0b00100110] = new int[] {6, 1, 0, 6, 0, 8, 6, 8, 10, 6, 10, 3, 6, 3, 5};
		table[0b00100111] = new int[] {10, 3, 5, 10, 5, 6, 8, 10, 6, 8, 6, 2};
		table[0b00101000] = new int[] {5, 3, 4, 5, 4, 8, 5, 8, 10, 5, 10, 7};
		table[0b00101001] = new int[] {0, 3, 4, 5, 1, 8, 5, 8, 10, 5, 10, 7, 1, 2, 8};
		table[0b00101010] = new int[] {0, 8, 10, 0, 10, 7, 0, 7, 5};
		table[0b00101011] = new int[] {5, 1, 8, 5, 8, 10, 5, 10, 7, 1, 2, 8};
		table[0b00101100] = new int[] {8, 10, 7, 8, 7, 6, 8, 6, 1, 8, 1, 3, 8, 3, 4};
		table[0b00101101] = new int[] {0, 3, 4, 6, 2, 8, 6, 8, 10, 6, 10, 7};
		table[0b00101110] = new int[] {10, 7, 6, 10, 6, 8, 8, 6, 1, 8, 1, 0};
		table[0b00101111] = new int[] {6, 2, 8, 6, 8, 10, 6, 10, 7};

		table[0b00110000] = new int[] {2, 9, 10, 2, 10, 4};
		table[0b00110001] = new int[] {9, 10, 4, 9, 4, 0, 9, 0, 1};
		table[0b00110010] = new int[] {10, 3, 0, 10, 0, 2, 10, 2, 9};
		table[0b00110011] = new int[] {1, 9, 10, 1, 10, 3};
		table[0b00110100] = new int[] {5, 6, 9, 5, 9, 10, 5, 10, 4, 5, 4, 2, 5, 2, 1};
		table[0b00110101] = new int[] {9, 10, 4, 9, 4, 0, 0, 5, 6, 0, 6, 9};
		table[0b00110110] = new int[] {0, 2, 1, 10, 3, 5, 10, 5, 6, 10, 6, 9};
		table[0b00110111] = new int[] {10, 3, 5, 10, 5, 6, 10, 6, 9};
		table[0b00111000] = new int[] {5, 3, 4, 5, 4, 2, 5, 2, 9, 5, 9, 10, 5, 10, 7};
		table[0b00111001] = new int[] {0, 3, 4, 9, 10, 7, 9, 7, 5, 9, 5, 1};
		table[0b00111010] = new int[] {7, 5, 0, 7, 0, 10, 10, 0, 2, 10, 2, 9};
		table[0b00111011] = new int[] {9, 10, 7, 9, 7, 5, 9, 5, 1};
		table[0b00111100] = new int[] {1, 3, 4, 1, 4, 2, 6, 9, 10, 6, 10, 7};
		table[0b00111101] = new int[] {0, 3, 4, 6, 9, 10, 6, 10, 7};
		table[0b00111110] = new int[] {0, 2, 1, 6, 9, 10, 6, 10, 7};
		table[0b00111111] = new int[] {6, 9, 10, 6, 10, 7};

		table[0b01000000] = new int[] {6, 11, 9};
		table[0b01000001] = new int[] {0, 1, 6, 0, 6, 11, 0, 11, 9, 0, 9, 2};
		table[0b01000010] = new int[] {0, 4, 3, 6, 11, 9};
		table[0b01000011] = new int[] {11, 9, 2, 11, 2, 4, 11, 4, 3, 11, 3, 1, 11, 1, 6};
		table[0b01000100] = new int[] {1, 5, 11, 1, 11, 9};
		table[0b01000101] = new int[] {5, 11, 9, 5, 9, 2, 5, 2, 0};
		table[0b01000110] = new int[] {4, 3, 5, 4, 5, 11, 4, 11, 9, 4, 9, 1, 4, 1, 0};
		table[0b01000111] = new int[] {5, 11, 9, 5, 9, 2, 2, 4, 3, 2, 3, 5};
		table[0b01001000] = new int[] {3, 7, 11, 3, 11, 9, 3, 9, 6, 3, 6, 5};
		table[0b01001001] = new int[] {1, 6, 5, 0, 3, 11, 0, 11, 9, 0, 9, 2, 3, 7, 11};
		table[0b01001010] = new int[] {9, 6, 5, 9, 5, 0, 9, 0, 4, 9, 4, 7, 9, 7, 11};
		table[0b01001011] = new int[] {1, 6, 5, 4, 7, 11, 4, 11, 9, 4, 9, 2};
		table[0b01001100] = new int[] {1, 3, 7, 1, 7, 11, 1, 11, 9};
		table[0b01001101] = new int[] {0, 3, 11, 0, 11, 9, 0, 9, 2, 3, 7, 11};
		table[0b01001110] = new int[] {4, 7, 11, 4, 11, 9, 9, 1, 0, 9, 0, 4};
		table[0b01001111] = new int[] {4, 7, 11, 4, 11, 9, 4, 9, 2};

		table[0b01010000] = new int[] {2, 6, 11, 2, 11, 8};
		table[0b01010001] = new int[] {8, 0, 1, 8, 1, 6, 8, 6, 11};
		table[0b01010010] = new int[] {3, 0, 2, 3, 2, 6, 3, 6, 11, 3, 11, 8, 3, 8, 4};
		table[0b01010011] = new int[] {4, 3, 1, 4, 1, 8, 8, 1, 6, 8, 6, 11};
		table[0b01010100] = new int[] {11, 8, 2, 11, 2, 1, 11, 1, 5};
		table[0b01010101] = new int[] {0, 5, 11, 0, 11, 8};
		table[0b01010110] = new int[] {0, 2, 1, 11, 8, 4, 11, 4, 3, 11, 3, 5};
		table[0b01010111] = new int[] {11, 8, 4, 11, 4, 3, 11, 3, 5};
		table[0b01011000] = new int[] {3, 7, 11, 3, 11, 8, 3, 8, 2, 3, 2, 6, 3, 6, 5};
		table[0b01011001] = new int[] {1, 6, 5, 8, 0, 3, 8, 3, 7, 8, 7, 11};
		table[0b01011010] = new int[] {0, 2, 6, 0, 6, 5, 4, 7, 11, 4, 11, 8};
		table[0b01011011] = new int[] {1, 6, 5, 4, 7, 11, 4, 11, 8};
		table[0b01011100] = new int[] {11, 8, 2, 11, 2, 1, 7, 11, 1, 7, 1, 3};
		table[0b01011101] = new int[] {8, 0, 3, 8, 3, 7, 8, 7, 11};
		table[0b01011110] = new int[] {0, 2, 1, 4, 7, 11, 4, 11, 8};
		table[0b01011111] = new int[] {4, 7, 11, 4, 11, 8};

		table[0b01100000] = new int[] {4, 8, 9, 4, 9, 6, 4, 6, 11, 4, 11, 10};
		table[0b01100001] = new int[] {2, 8, 9, 0, 1, 11, 0, 11, 10, 0, 10, 4, 1, 6, 11};
		table[0b01100010] = new int[] {6, 11, 10, 6, 10, 3, 6, 3, 0, 6, 0, 8, 6, 8, 9};
		table[0b01100011] = new int[] {2, 8, 9, 3, 1, 6, 3, 6, 11, 3, 11, 10};
		table[0b01100100] = new int[] {4, 8, 9, 4, 9, 1, 4, 1, 5, 4, 5, 11, 4, 11, 10};
		table[0b01100101] = new int[] {2, 8, 9, 5, 11, 10, 5, 10, 4, 5, 4, 0};
		table[0b01100110] = new int[] {0, 8, 9, 0, 9, 1, 3, 5, 11, 3, 11, 10};
		table[0b01100111] = new int[] {2, 8, 9, 3, 5, 11, 3, 11, 10};
		table[0b01101000] = new int[] {7, 11, 10, 5, 3, 8, 5, 8, 9, 5, 9, 6, 3, 4, 8};
		table[0b01101001] = new int[] {0, 3, 4, 1, 6, 5, 2, 8, 9, 7, 11, 10};
		table[0b01101010] = new int[] {7, 11, 10, 0, 8, 9, 0, 9, 6, 0, 6, 5};
		table[0b01101011] = new int[] {1, 6, 5, 2, 8, 9, 7, 11, 10};
		table[0b01101100] = new int[] {7, 11, 10, 1, 3, 4, 1, 4, 8, 1, 8, 9};
		table[0b01101101] = new int[] {0, 3, 4, 2, 8, 9, 7, 11, 10};
		table[0b01101110] = new int[] {7, 11, 10, 0, 8, 9, 0, 9, 1};
		table[0b01101111] = new int[] {2, 8, 9, 7, 11, 10};

		table[0b01110000] = new int[] {2, 6, 11, 2, 11, 10, 2, 10, 4};
		table[0b01110001] = new int[] {0, 1, 11, 0, 11, 10, 0, 10, 4, 1, 6, 11};
		table[0b01110010] = new int[] {2, 6, 11, 2, 11, 10, 0, 2, 10, 0, 10, 3};
		table[0b01110011] = new int[] {3, 1, 6, 3, 6, 11, 3, 11, 10};
		table[0b01110100] = new int[] {10, 4, 2, 10, 2, 11, 11, 2, 1, 11, 1, 5};
		table[0b01110101] = new int[] {5, 11, 10, 5, 10, 4, 5, 4, 0};
		table[0b01110110] = new int[] {0, 2, 1, 3, 5, 11, 3, 11, 10};
		table[0b01110111] = new int[] {3, 5, 11, 3, 11, 10};
		table[0b01111000] = new int[] {7, 11, 10, 2, 6, 5, 2, 5, 3, 2, 3, 4};
		table[0b01111001] = new int[] {0, 3, 4, 1, 6, 5, 7, 11, 10};
		table[0b01111010] = new int[] {0, 2, 6, 0, 6, 5, 7, 11, 10};
		table[0b01111011] = new int[] {1, 6, 5, 7, 11, 10};
		table[0b01111100] = new int[] {1, 3, 4, 1, 4, 2, 7, 11, 10};
		table[0b01111101] = new int[] {0, 3, 4, 7, 11, 10};
		table[0b01111110] = new int[] {0, 2, 1, 7, 11, 10};
		table[0b01111111] = new int[] {7, 11, 10};

		table[0b10000000] = new int[] {7, 10, 11};
		table[0b10000001] = new int[] {0, 1, 2, 7, 10, 11};
		table[0b10000010] = new int[] {0, 4, 10, 0, 10, 11, 0, 11, 7, 0, 7, 3};
		table[0b10000011] = new int[] {11, 7, 3, 11, 3, 1, 11, 1, 2, 11, 2, 4, 11, 4, 10};
		table[0b10000100] = new int[] {1, 5, 7, 1, 7, 10, 1, 10, 11, 1, 11, 6};
		table[0b10000101] = new int[] {10, 11, 6, 10, 6, 2, 10, 2, 0, 10, 0, 5, 10, 5, 7};
		table[0b10000110] = new int[] {3, 5, 7, 1, 0, 10, 1, 10, 11, 1, 11, 6, 0, 4, 10};
		table[0b10000111] = new int[] {3, 5, 7, 2, 4, 10, 2, 10, 11, 2, 11, 6};
		table[0b10001000] = new int[] {3, 10, 11, 3, 11, 5};
		table[0b10001001] = new int[] {2, 0, 3, 2, 3, 10, 2, 10, 11, 2, 11, 5, 2, 5, 1};
		table[0b10001010] = new int[] {5, 0, 4, 5, 4, 10, 5, 10, 11};
		table[0b10001011] = new int[] {2, 4, 10, 2, 10, 11, 1, 2, 11, 1, 11, 5};
		table[0b10001100] = new int[] {3, 10, 11, 3, 11, 6, 3, 6, 1};
		table[0b10001101] = new int[] {10, 2, 0, 10, 0, 3, 2, 10, 11, 2, 11, 6};
		table[0b10001110] = new int[] {1, 0, 10, 1, 10, 11, 1, 11, 6, 0, 4, 10};
		table[0b10001111] = new int[] {2, 4, 10, 2, 10, 11, 2, 11, 6};

		table[0b10010000] = new int[] {2, 9, 11, 2, 11, 7, 2, 7, 10, 2, 10, 8};
		table[0b10010001] = new int[] {7, 10, 8, 7, 8, 0, 7, 0, 1, 7, 1, 9, 7, 9, 11};
		table[0b10010010] = new int[] {4, 10, 8, 3, 0, 9, 3, 9, 11, 3, 11, 7, 0, 2, 9};
		table[0b10010011] = new int[] {4, 10, 8, 1, 9, 11, 1, 11, 7, 1, 7, 3};
		table[0b10010100] = new int[] {6, 9, 11, 1, 5, 10, 1, 10, 8, 1, 8, 2, 5, 7, 10};
		table[0b10010101] = new int[] {6, 9, 11, 0, 5, 7, 0, 7, 10, 0, 10, 8};
		table[0b10010110] = new int[] {0, 2, 1, 3, 5, 7, 4, 10, 8, 6, 9, 11};
		table[0b10010111] = new int[] {3, 5, 7, 4, 10, 8, 6, 9, 11};
		table[0b10011000] = new int[] {2, 9, 11, 2, 11, 5, 2, 5, 3, 2, 3, 10, 2, 10, 8};
		table[0b10011001] = new int[] {0, 3, 10, 0, 10, 8, 1, 9, 11, 1, 11, 5};
		table[0b10011010] = new int[] {4, 10, 8, 5, 0, 2, 5, 2, 9, 5, 9, 11};
		table[0b10011011] = new int[] {4, 10, 8, 1, 9, 11, 1, 11, 5};
		table[0b10011100] = new int[] {6, 9, 11, 3, 10, 8, 3, 8, 2, 3, 2, 1};
		table[0b10011101] = new int[] {6, 9, 11, 0, 3, 10, 0, 10, 8};
		table[0b10011110] = new int[] {0, 2, 1, 4, 10, 8, 6, 9, 11};
		table[0b10011111] = new int[] {4, 10, 8, 6, 9, 11};

		table[0b10100000] = new int[] {4, 8, 11, 4, 11, 7};
		table[0b10100001] = new int[] {1, 2, 8, 1, 8, 11, 1, 11, 7, 1, 7, 4, 1, 4, 0};
		table[0b10100010] = new int[] {8, 11, 7, 8, 7, 3, 8, 3, 0};
		table[0b10100011] = new int[] {11, 1, 2, 11, 2, 8, 7, 3, 1, 7, 1, 11};
		table[0b10100100] = new int[] {1, 5, 7, 1, 7, 4, 1, 4, 8, 1, 8, 11, 1, 11, 6};
		table[0b10100101] = new int[] {0, 5, 7, 0, 7, 4, 2, 8, 11, 2, 11, 6};
		table[0b10100110] = new int[] {3, 5, 7, 8, 11, 6, 8, 6, 1, 8, 1, 0};
		table[0b10100111] = new int[] {3, 5, 7, 2, 8, 11, 2, 11, 6};
		table[0b10101000] = new int[] {11, 5, 3, 11, 3, 4, 11, 4, 8};
		table[0b10101001] = new int[] {0, 3, 4, 11, 5, 1, 11, 1, 2, 11, 2, 8};
		table[0b10101010] = new int[] {0, 8, 11, 0, 11, 5};
		table[0b10101011] = new int[] {11, 5, 1, 11, 1, 2, 11, 2, 8};
		table[0b10101100] = new int[] {1, 3, 4, 1, 4, 8, 8, 11, 6, 8, 6, 1};
		table[0b10101101] = new int[] {0, 3, 4, 2, 8, 11, 2, 11, 6};
		table[0b10101110] = new int[] {8, 11, 6, 8, 6, 1, 8, 1, 0};
		table[0b10101111] = new int[] {2, 8, 11, 2, 11, 6};

		table[0b10110000] = new int[] {4, 2, 9, 4, 9, 11, 4, 11, 7};
		table[0b10110001] = new int[] {7, 4, 0, 7, 0, 1, 1, 9, 11, 1, 11, 7};
		table[0b10110010] = new int[] {3, 0, 9, 3, 9, 11, 3, 11, 7, 0, 2, 9};
		table[0b10110011] = new int[] {1, 9, 11, 1, 11, 7, 1, 7, 3};
		table[0b10110100] = new int[] {6, 9, 11, 4, 2, 1, 4, 1, 5, 4, 5, 7};
		table[0b10110101] = new int[] {6, 9, 11, 0, 5, 7, 0, 7, 4};
		table[0b10110110] = new int[] {0, 2, 1, 3, 5, 7, 6, 9, 11};
		table[0b10110111] = new int[] {3, 5, 7, 6, 9, 11};
		table[0b10111000] = new int[] {3, 4, 2, 3, 2, 5, 9, 11, 5, 9, 5, 2};
		table[0b10111001] = new int[] {0, 3, 4, 1, 9, 11, 1, 11, 5};
		table[0b10111010] = new int[] {5, 0, 2, 5, 2, 9, 5, 9, 11};
		table[0b10111011] = new int[] {1, 9, 11, 1, 11, 5};
		table[0b10111100] = new int[] {6, 9, 11, 1, 3, 4, 1, 4, 2};
		table[0b10111101] = new int[] {0, 3, 4, 6, 9, 11};
		table[0b10111110] = new int[] {0, 2, 1, 6, 9, 11};
		table[0b10111111] = new int[] {6, 9, 11};

		table[0b11000000] = new int[] {6, 7, 10, 6, 10, 9};
		table[0b11000001] = new int[] {0, 1, 6, 0, 6, 7, 0, 7, 10, 0, 10, 9, 0, 9, 2};
		table[0b11000010] = new int[] {0, 4, 10, 0, 10, 9, 0, 9, 6, 0, 6, 7, 0, 7, 3};
		table[0b11000011] = new int[] {1, 6, 7, 1, 7, 3, 2, 4, 10, 2, 10, 9};
		table[0b11000100] = new int[] {9, 1, 5, 9, 5, 7, 9, 7, 10};
		table[0b11000101] = new int[] {10, 9, 2, 10, 2, 0, 0, 5, 7, 0, 7, 10};
		table[0b11000110] = new int[] {3, 5, 7, 9, 1, 0, 9, 0, 4, 9, 4, 10};
		table[0b11000111] = new int[] {3, 5, 7, 2, 4, 10, 2, 10, 9};
		table[0b11001000] = new int[] {10, 9, 6, 10, 6, 5, 10, 5, 3};
		table[0b11001001] = new int[] {1, 6, 5, 10, 9, 2, 10, 2, 0, 10, 0, 3};
		table[0b11001010] = new int[] {10, 9, 6, 10, 6, 5, 5, 0, 4, 5, 4, 10};
		table[0b11001011] = new int[] {1, 6, 5, 2, 4, 10, 2, 10, 9};
		table[0b11001100] = new int[] {1, 3, 10, 1, 10, 9};
		table[0b11001101] = new int[] {10, 9, 2, 10, 2, 0, 10, 0, 3};
		table[0b11001110] = new int[] {9, 1, 0, 9, 0, 4, 9, 4, 10};
		table[0b11001111] = new int[] {2, 4, 10, 2, 10, 9};

		table[0b11010000] = new int[] {6, 7, 10, 6, 10, 8, 6, 8, 2};
		table[0b11010001] = new int[] {8, 0, 1, 8, 1, 6, 10, 8, 6, 10, 6, 7};
		table[0b11010010] = new int[] {4, 10, 8, 6, 7, 3, 6, 3, 0, 6, 0, 2};
		table[0b11010011] = new int[] {4, 10, 8, 1, 6, 7, 1, 7, 3};
		table[0b11010100] = new int[] {1, 5, 10, 1, 10, 8, 1, 8, 2, 5, 7, 10};
		table[0b11010101] = new int[] {0, 5, 7, 0, 7, 10, 0, 10, 8};
		table[0b11010110] = new int[] {0, 2, 1, 3, 5, 7, 4, 10, 8};
		table[0b11010111] = new int[] {3, 5, 7, 4, 10, 8};
		table[0b11011000] = new int[] {8, 2, 6, 8, 6, 10, 10, 6, 5, 10, 5, 3};
		table[0b11011001] = new int[] {1, 6, 5, 0, 3, 10, 0, 10, 8};
		table[0b11011010] = new int[] {4, 10, 8, 0, 2, 6, 0, 6, 5};
		table[0b11011011] = new int[] {1, 6, 5, 4, 10, 8};
		table[0b11011100] = new int[] {3, 10, 8, 3, 8, 2, 3, 2, 1};
		table[0b11011101] = new int[] {0, 3, 10, 0, 10, 8};
		table[0b11011110] = new int[] {0, 2, 1, 4, 10, 8};
		table[0b11011111] = new int[] {4, 10, 8};

		table[0b11100000] = new int[] {7, 4, 8, 7, 8, 9, 7, 9, 6};
		table[0b11100001] = new int[] {2, 8, 9, 7, 4, 0, 7, 0, 1, 7, 1, 6};
		table[0b11100010] = new int[] {9, 6, 7, 9, 7, 8, 8, 7, 3, 8, 3, 0};
		table[0b11100011] = new int[] {2, 8, 9, 1, 6, 7, 1, 7, 3};
		table[0b11100100] = new int[] {8, 9, 1, 8, 1, 4, 4, 1, 5, 4, 5, 7};
		table[0b11100101] = new int[] {2, 8, 9, 0, 5, 7, 0, 7, 4};
		table[0b11100110] = new int[] {3, 5, 7, 0, 8, 9, 0, 9, 1};
		table[0b11100111] = new int[] {2, 8, 9, 3, 5, 7};
		table[0b11101000] = new int[] {5, 3, 8, 5, 8, 9, 5, 9, 6, 3, 4, 8};
		table[0b11101001] = new int[] {0, 3, 4, 1, 6, 5, 2, 8, 9};
		table[0b11101010] = new int[] {0, 8, 9, 0, 9, 6, 0, 6, 5};
		table[0b11101011] = new int[] {1, 6, 5, 2, 8, 9};
		table[0b11101100] = new int[] {1, 3, 4, 1, 4, 8, 1, 8, 9};
		table[0b11101101] = new int[] {0, 3, 4, 2, 8, 9};
		table[0b11101110] = new int[] {0, 8, 9, 0, 9, 1};
		table[0b11101111] = new int[] {2, 8, 9};

		table[0b11110000] = new int[] {2, 6, 7, 2, 7, 4};
		table[0b11110001] = new int[] {7, 4, 0, 7, 0, 1, 7, 1, 6};
		table[0b11110010] = new int[] {6, 7, 3, 6, 3, 0, 6, 0, 2};
		table[0b11110011] = new int[] {1, 6, 7, 1, 7, 3};
		table[0b11110100] = new int[] {4, 2, 1, 4, 1, 5, 4, 5, 7};
		table[0b11110101] = new int[] {0, 5, 7, 0, 7, 4};
		table[0b11110110] = new int[] {0, 2, 1, 3, 5, 7};
		table[0b11110111] = new int[] {3, 5, 7};
		table[0b11111000] = new int[] {2, 6, 5, 2, 5, 3, 2, 3, 4};
		table[0b11111001] = new int[] {0, 3, 4, 1, 6, 5};
		table[0b11111010] = new int[] {0, 2, 6, 0, 6, 5};
		table[0b11111011] = new int[] {1, 6, 5};
		table[0b11111100] = new int[] {1, 3, 4, 1, 4, 2};
		table[0b11111101] = new int[] {0, 3, 4};
		table[0b11111110] = new int[] {0, 2, 1};
		table[0b11111111] = new int[] {};

	}



}


void
MembraneGenerationSystem::shutdown() {
    System::shutdown();
}


void
MembraneGenerationSystem::update(
    int renderTime,
    int logicTime
) {
}
