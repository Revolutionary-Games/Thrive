#pragma once

#include "engine/typedefs.h"
#include <memory>

// Defines a basic grid that fits the criteria set on issue 165

namespace luabind {
    class scope;
}

namespace thrive {

// TODO make generic

// TODO should probably compute grid resolution in templates

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

    /**
     * Sets the resolution by specifying the 
     * size of a single grid cell in pixels.
     * @param dx
     * @param dy
     */
    void setResolution(int dx, int dy);

    // TODO probably not the best move function
    /** 
     * Moves the grid dc columns (left or right, undecided)
     * and dr rows (up or down, undecided), where dr and dc
     * are in terms of grid cells. For performance, try
     * batching grid moves when you can.
     * @param dc
     * @param dr
     */
    void move(int dc, int dr);


    // TODO decide whether stuff is accessed by position 
    int operator() (long x, long y); 

private:
    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
