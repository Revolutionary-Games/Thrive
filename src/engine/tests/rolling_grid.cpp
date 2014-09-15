#include "engine/rolling_grid.h"
#include "gtest/gtest.h"

using namespace thrive;

TEST(RollingGrid, Initialization) {
    RollingGrid grid(1920, 1080, 1);
}

TEST(RollingGrid, Read) {
    RollingGrid grid(1920, 1080, 1);
    EXPECT_EQ(0, grid(0, 0)); // somewhere in-range
    EXPECT_EQ(0, grid(15649, 986984)); // somewhere out-of-range
}

TEST(RollingGrid, Edit) {
    RollingGrid grid(1920, 1080, 1);
    EXPECT_EQ(0, grid(20, 20));
    grid(20, 20) = 5;
    // std::cout << "set (20,20) to 5" << std::endl;
    EXPECT_EQ(5, grid(20, 20));
    // std::cout << "checked (20,20) == 5" << std::endl;
    // make sure neighbors haven't been screwed with
    for (int i = 18; i < 23; i++) {
        for (int j = 18; j < 23; j++) {
            if (i != 20 || j != 20) {
                EXPECT_EQ(0, grid(i, j));
            }
        }
    }
}

TEST(RollingGrid, SmallMove) {
    RollingGrid grid(1920, 1080, 1);
    grid(100, 100) = 1;
    // std::cout << "set (100,100) to 1" << std::endl;
    grid.move(1, 0);
    // std::cout << "moved by (15, -12)" << std::endl;
    EXPECT_EQ(1, grid(100, 100));
    // std::cout << "checked (100,100)" << std::endl;
    EXPECT_EQ(0, grid(115, 88));
    EXPECT_EQ(0, grid(85, 112));
    for (int i = 0; i < 1920; i++) {
        for (int j = 0; j < 1080; j++) {
            int k = grid(i,j);
            if (k) std::cout << "(" << i << "," << j << ") = " << k << std::endl;
        }
    }
}

TEST(RollingGrid, BigMove) {
    RollingGrid grid(1920, 1080, 1);
    grid(0,0) = 1;
    grid.move(71280, 90506);
    EXPECT_EQ(0, grid(0,0));
    grid(71281, 90507) = 1;
    EXPECT_EQ(1, grid(71281, 90507));
}

TEST(RollingGrid, TinyGrid) {
    RollingGrid grid(1, 2, 1);
    grid(0,0) = 1;
    grid.move(0, -1);
    EXPECT_EQ(1, grid(0,0));
}

TEST(RollingGrid, NullMoves) {
    RollingGrid grid(100, 200, 1);
    grid(0,0) = 1;
    grid.move(0, 0);
    EXPECT_EQ(1, grid(0,0));

    grid.move(0, -1);
    grid.move(0, 1);
    EXPECT_EQ(1, grid(0,0));
}
