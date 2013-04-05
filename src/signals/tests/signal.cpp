#include "signals/signal.h"

#include "util/make_unique.h"

#include <gtest/gtest.h>

using namespace thrive::signals;


TEST (SignalDeathTest, SelfDeletion) {
    auto signalPtr = std::make_shared<Signal<>>();
    int count = 0;
    auto slot = [&count, &signalPtr] () {
        count++;
        signalPtr.reset();
    };
    signalPtr->connect(slot);
    EXPECT_DEATH(
        {(*signalPtr)();},
        "Cannot destroy a signal from inside a slot"
    );
}


TEST (SignalDeathTest, RecursiveEmission) {
    Signal<> signal;
    auto slot = [&signal] () {
        signal();
    };
    signal.connect(slot);
    EXPECT_DEATH(
        signal(),
        "Cannot recursively emit signal"
    );
}


TEST (Signal, Emit) {
    Signal<> signal;
    bool emitted = false;
    auto slot = [&emitted] () {
        emitted = true;
    };
    signal.connect(slot);
    signal();
    EXPECT_TRUE(emitted);
}


TEST (Signal, Argument) {
    Signal<int> signal;
    int value = 0;
    auto slot = [&value] (int v) {
        value = v;
    };
    signal.connect(slot);
    for (int i : {5, 10, -20, 3}) {
        signal(i);
        EXPECT_EQ(i, value);
    }
}


TEST (Signal, ManualDisconnect) {
    Signal<> signal;
    int count = 0;
    auto slot = [&count] () {
        count++;
    };
    auto connection = signal.connect(slot);
    signal();
    EXPECT_EQ(1, count);
    connection->disconnect();
    signal();
    EXPECT_EQ(1, count);
}


TEST (Signal, SelfDisconnect) {
    Signal<> signal;
    int count = 0;
    std::shared_ptr<Connection> connection;
    auto slot = [&count, &connection] () {
        count++;
        connection->disconnect();
    };
    connection = signal.connect(slot);
    signal();
    EXPECT_EQ(1, count);
    signal();
    EXPECT_EQ(1, count);
}


TEST (Signal, SharedPtrGuardian) {
    struct Dummy {};
    auto watched = std::make_shared<Dummy>();
    Signal<> signal;
    int count = 0;
    auto slot = [&count] () {
        count++;
    };
    signal.connect(slot, createSharedPtrGuardian(watched));
    signal();
    EXPECT_EQ(1, count);
    watched.reset();
    signal();
    EXPECT_EQ(1, count);
}

