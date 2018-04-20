#include "engine/engine.h"

#include "engine/entity_manager.h"
#include "engine/tests/test_component.h"
#include "util/make_unique.h"

#include <gtest/gtest.h>
#include <iostream>

using namespace thrive;

namespace {

class TestEngine : public Engine {
public:
    TestEngine(EntityManager& entityManager) : Engine(entityManager) {}
};

} // namespace
