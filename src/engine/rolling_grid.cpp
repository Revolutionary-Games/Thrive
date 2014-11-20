#include <vector>
#include <memory>
#include <cstring>
#include "engine/rolling_grid.h"
#include <boost/multi_array.hpp>
#include "scripting/luabind.h"

using namespace thrive;


// TODO: optimize, template, and add support for a lower-level leaf datastructure

struct RollingGrid::Implementation {
    /// Dimensions in grid coordinates. Must be a power of 2.
    int m_width, m_height;
    /// Width and height of each grid cell in world coordinates
    int m_resolution;
    /// Position of smallest corner of rolling grid in world coordinates.
    long m_x, m_y;

    // for convenience
    int m_rows, m_cols;

    static const int DEFAULT_VAL = 0;

    boost::multi_array<int, 2> m_data; // row-major (where rows are of constant x)
    typedef boost::multi_array<int, 2>::index Index;

    Implementation(int width, int height, int resolution)
        : m_width(width), m_height(height), m_resolution(resolution), 
        m_x(0), m_y(0),
        m_rows(height / resolution), m_cols(width / resolution) {
        
        m_data.resize(boost::extents[m_rows][m_cols]);
        // TODO clear all data
        for (int i = 0; i < m_rows; i++)
            for (int j = 0; j < m_cols; ++j)
                m_data[i][j] = DEFAULT_VAL;
    }

    int garbage = DEFAULT_VAL;
    
    void 
    clearRow(int row) {
        for (int i = 0; i < m_cols; i++) {
            m_data[row][i] = DEFAULT_VAL;
        }
    }

    void
    clearCol(int col) {
        for (int i = 0; i < m_rows; i++) {
            m_data[i][col] = DEFAULT_VAL;
        }
    }

    // Used to control wraparound
    // Note -- they denote the row above and column left of the wraparound on the /grid/
    int m_wrapc = 0, m_wrapr = 0;

    void
    move(int dx, int dy) {
        m_x += dx; m_y += dy;
        int wrapfc = (((m_x / m_resolution) % m_cols) + m_cols) % m_cols;
        int wrapfr = (((m_y / m_resolution) % m_rows) + m_rows) % m_rows;

#if 1 // clear wrapped rows and cols
        // rows <=> y
        if (dy < 0) {
            if (wrapfr > m_wrapr) {
                // clear edge areas
                for (int row = 0; row < m_wrapr; row++) {
                    clearRow(row);
                }
                for (int row = wrapfr; row < m_rows; row++) {
                    clearRow(row);
                }
            } else if (wrapfr < m_wrapr) {
                // clear middle area
                for (int row = wrapfr; row < m_wrapr; row++) {
                    clearRow(row);
                }
            } // else do nothing, no y movement
        } else if (dy > 0) {
            if (wrapfr < m_wrapr) {
                // clear edge areas
                for (int row = m_wrapr; row < m_rows; row++) {
                    clearRow(row);
                }
                for (int row = 0; row < wrapfr; row++) {
                    clearRow(row);
                }
            } else if (wrapfr > m_wrapr) {
                // clear middle area
                for (int row = m_wrapr; row < wrapfr; row++) {
                    clearRow(row);
                }
            } // else do nothing, no y movement
        } // else do nothing, no y movement

        // x is pretty much same
        // cols <=> x
        if (dx < 0) {
            if (wrapfc > m_wrapc) {
                // clear edge areas
                for (int col = 0; col < m_wrapc; col++) {
                    clearCol(col);
                }
                for (int col = wrapfc; col < m_cols; col++) {
                    clearCol(col);
                }
            } else if (wrapfc < m_wrapc) {
                // clear middle area
                for (int col = wrapfc; col < m_wrapc; col++) {
                    clearCol(col);
                }
            } // else do nothing, no y movement
        } else if (dx > 0) {
            if (wrapfc < m_wrapc) {
                // clear edge areas
                for (int col = m_wrapr; col < m_cols; col++) {
                    clearCol(col);
                }
                for (int col = 0; col < wrapfc; col++) {
                    clearCol(col);
                }
            } else if (wrapfc > m_wrapc) {
                // clear middle area
                for (int col = m_wrapc; col < wrapfc; col++) {
                    clearCol(col);
                }
            } // else do nothing, no x movement
        } // else do nothing, no x movement
#endif
        m_wrapc = wrapfc;
        m_wrapr = wrapfr;
    }

    /**
    * Beware, if out of range you'll be given a (usable) garbage address,
    * which is guaranteed to hold the default value on at least the first read.
    */
    int&
    operator()(long xin, long yin) {

        // essentially, we temp-move the grid to (xin, yin) and read at (m_wrapr, m_wrapy)

        int x = xin - m_x;
        int y = yin - m_y;
        // x & y now in relative world coordinates
        if (x < 0 || y < 0 || x >= m_width || y >= m_height) {
            return garbage = DEFAULT_VAL;
        }
        x = x / m_resolution % m_cols;
        y = y / m_resolution % m_rows;
        x = (x + m_wrapc) % m_cols;
        y = (y + m_wrapr) % m_rows;
        return m_data[y][x];
    }
};


luabind::scope
RollingGrid::luaBindings() {
    using namespace luabind;
    return class_<RollingGrid>("RollingGrid")
        .def(constructor<int, int, int>())
        .def("move", &RollingGrid::move)
        .def("get", &RollingGrid::get)
        .def("set", &RollingGrid::set)
    ;
}

RollingGrid::RollingGrid(int width, int height, int resolution) 
    : m_impl(new Implementation(width, height, resolution)) {

}

// TODO make stuff actually happen
RollingGrid::~RollingGrid(){}

void
RollingGrid::move(int dx, int dy) {
    m_impl->move(dx, dy);
}

int&
RollingGrid::operator()(long x, long y) {
    return m_impl->operator()(x, y);
}

int
RollingGrid::get(long x, long y) {
    return m_impl->operator()(x, y);
}

void
RollingGrid::set(long x, long y, int v) {
    m_impl->operator()(x, y) = v;
}
