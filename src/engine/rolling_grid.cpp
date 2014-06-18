#include "engine/rolling_grid.h"
#include "scripting/luabind.h"

using namespace thrive;

struct RollingGrid::Implementation {

    Implementation(int width, int height)
    {
    }

};

luabind::scope
RollingGrid::luaBindings() {
    using namespace luabind;
    return class_<RollingGrid>("RollingGrid")
        .def(constructor<int, int>())
        .def("move", &RollingGrid::move)
        .def("operator()", &RollingGrid::operator()) // can this be exported by luabind?
    ;
}

RollingGrid::RollingGrid(int width, int height) 
    : m_impl(new Implementation(width, height) {

}

// TODO make stuff actually happen
RollingGrid::~RollingGrid(){}

void
RollingGrid::setResolution(int dx, int dy) {}

void
RollingGrid::move(int dc, int dr) {}

int
RollingGrid::operator()(long x, long y) {}
