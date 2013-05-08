#include "engine/component_factory.h"

#include "engine/tests/test_component.h"

#include <gtest/gtest.h>

using namespace thrive;

TEST(ComponentFactory, NameNotFound) {
    EXPECT_THROW(
        ComponentFactory::instance().create("I hopefully don't exist"),
        std::invalid_argument
    );
}

using TestRegisteredComponent = TestComponent<0>;

REGISTER_COMPONENT(TestRegisteredComponent)

TEST(ComponentFactory, Create) {
    std::shared_ptr<Component> component = ComponentFactory::instance().create(
        TestComponent<0>::TYPE_NAME()
    );
    auto testComponent = std::dynamic_pointer_cast<TestComponent<0>>(component);
    EXPECT_TRUE(testComponent != nullptr);
}
