#include <vector>
#include <memory>
#include <cstring>
#include "engine/rolling_grid.h"
#include "scripting/luabind.h"

using namespace thrive;

struct Node {
    static const unsigned char SIZE = 64;
    /// Absolute x and y (world coords) of smallest corner of Node
    long x=0, y=0;
    int data[SIZE*SIZE] = {0}; // row-major

    Node() {
        memset(data, 0, Node.SIZE*Node.SIZE*sizeof(int));
    }
};

// NOTE: If we decide to always have every node exist, we can
//          do away with all the housecleaning optimizations,
//          and we can also ensure that any one location in space 
//          maps to a constant memory location (per instance, obv),
//          which might be worth this ugly comment formatting.

// TODO: lotsa stuff

struct RollingGrid::Implementation {
    /// BUFFER_THICKNESS = num_nodes_in_row - m_width/Node.SIZE
    ///  = num_nodes_in_column - m_height/Node.SIZE
    static const unsigned char BUFFER_THICKNESS = 1;
    /// Dimensions in world coordinates
    int m_width, m_height;
    /// Width and height of each grid cell in world coordinates
    int m_resolution;
    /// Position of smallest corner of rolling grid in world coordinates
    long m_x, m_y; 

    int m_rows, m_cols;

    static const int default_val = 0;

    std::vector<std::unique_ptr<Node>> m_data; // row-major

    Implementation(int width, int height, int resolution)
        : m_width(width), m_height(height), m_resolution(resolution), 
        m_rows(BUFFER_THICKNESS + height / (resolution * Node.SIZE), 
        m_cols(BUFFER_THICKNESS + width / (resolution * Node.SIZE)) {
        // clear all data
        m_data.resize(m_rows * m_cols);
        for (int i = 0; i < m_rows * m_cols; i++) {
            m_data[i]();
        }

    }

    ~Implementation() {

    }

    // Used to control wraparound
    int m_wrapx = 0, m_wrapy = 0;

    void
    move(int dx, int dy) {
        m_x += dx;
        m_y += dy;

        // Calculate the intersection (in m_data nodes) 
        // between previous position and current

        // delete all the nodes outside the intersection

        // update wraparound

    }

    // NOTE: much of this work will be done bitwise for speed.
    int
    peek(long x, long y) {
        if (x < m_x || y < m_y || x > m_x + m_width || y > m_y + m_height)
            return default_val;
        // in grid coordinates
        int row = (y - m_y) / (m_resolution) 
            + (m_height / m_resolution) // should be sped up ...later
            % (m_height / m_resolution);
        int col = (x - m_x) / (m_resolution)
            + (m_width / m_resolution)
            % (m_width / m_resolution);
        // location in m_data
        int data_row = (row / Node.SIZE + m_wrapy) % m_rows;
        int data_col = (col / Node.SIZE + m_wrapx) % m_cols;
        if (nullptr == m_data[data_row * m_cols + data_col])
            return default_val;
        // location in the Node
        int node_row = row % Node.SIZE;
        int node_col = col % Node.SIZE;
        return m_data[data_row * m_cols + data_col]->data[
                        node_row * Node.SIZE + node_col];
    }

    /// number of edits per rewipe
    static const int WIPE_RATE = 20;

    /**
     * Provides a value avaiable for editing. Since it would
     * be hard to ensure only actual changes spawn new data blocks,
     * this spawns a data block if there isn't one already.
     * 
     * Use only if you're sure you'll be changing the value.
     */
    int&
    operator()(long x, long y) {
        int row = (y - m_y) / (m_resolution) // almost same as peek
            + (m_height / m_resolution) // should be sped up ...later
            % (m_height / m_resolution);
        int col = (x - m_x) / (m_resolution)
            + (m_width / m_resolution)
            % (m_width / m_resolution);
        // location in m_data
        int data_row = (row / Node.SIZE + m_wrapy) % m_rows;
        int data_col = (col / Node.SIZE + m_wrapx) % m_cols;
        auto data_ptr = m_data[data_row * m_cols + data_col]
        if (nullptr == data_ptr) {
            data_ptr.reset(new Node);
        }
        // location in the Node
        int node_row = row % Node.SIZE;
        int node_col = col % Node.SIZE;
        return data_ptr->data[node_row * Node.SIZE + node_col];
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

RollingGrid::RollingGrid(int width, int height, int resolution) 
    : m_impl(new Implementation(width, height, resolution) {

}


// TODO make stuff actually happen
RollingGrid::~RollingGrid(){}


void
RollingGrid::move(int dx, int dy) {
    m_impl->move(dx, dy);
}

int
peek(long x, long y) {
    return m_impl->peek(x, y);
}

int&
RollingGrid::operator()(long x, long y) {
    return m_impl->operator()(x, y);
}
