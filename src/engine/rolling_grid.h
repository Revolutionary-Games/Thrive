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
     * @param width The width of the grid in number of grid cells
     * @param height The height of the grid in number of grid cells
     * @param resolution The width and height of each grid cell
     */
    RollingGrid(
           int width, int height, int resolution
    );
    
    /**
     * @brief destructor
     */
    ~RollingGrid();

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - constructor(int, int, int)
    * - RollingGrid::move(int, int)
    * - RollingGrid::get(long, long)
    * - RollingGrid::set(long, long, int)
    * @return
    */
    static luabind::scope
    luaBindings();

    // TODO probably not the best move function
    /** 
     * Moves the grid a certain distance in world coordinates.
     * @param dx
     * @param dy
     */
    void
    move(int dx, int dy);

    // TODO decide what happens on out-of-bounds accesses
    /**
     * Get read-write access to a location in the grid.
     * Out of bounds accesses are useless but harmless.
     * @param x X coordinate to access, in world coordinates.
     * @param y Y coordinate to access, in world coordinates.
     */
    int&
    operator() (long x, long y); 

    /*
     * Lua-accessible position indexing
     */
    int
    get(long x, long y);
    void
    set(long x, long y, int v);

private:
    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
