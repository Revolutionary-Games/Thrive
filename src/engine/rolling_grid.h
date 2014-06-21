#pragma once

#include "engine/typedefs.h"
#include <memory>

// Defines a basic grid that fits the criteria set on issue 165

namespace luabind {
    class scope;
}

namespace thrive {

// TODO make generic

class RollingGrid {

public:
    
    /**
     * @brief constructor
     * 
     * @param width The width in grid cells
     * @param height The height in grid cells
     */
    RollingGrid(
           int width, int height
    );
    
    /**
     * @brief destructor
     */
    ~RollingGrid();

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - Yet to be decided
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    // TODO probably not the best move function
    /** 
     * Moves the grid a certain distance in world coordinates.
     * For performance, try batching grid moves when you can.
     * @param dx
     * @param dy
     */
    void
    move(int dx, int dy);

    /**
     * Read the value of a location in the grid. Locations outside 
     * grid return the default value.
     */
    int
    peek(long x , long y);

    // TODO decide what happens on out-of-bounds accesses
    /**
     * Get read-write access to a location in the grid.
     * Out of bounds accesses should be avoided.
     * @param x X coordinate to access, in world coordinates.
     * @param y Y coordinate to access, in world coordinates.
     */
    int&
    operator() (long x, long y); 

private:
    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
