
#include "CppUTest/CommandLineTestRunner.h"
#include "LightSm.mocks.test.h"
#include "LightSm.h"

int main(int ac, char** av) {
    return CommandLineTestRunner::RunAllTests(ac, av);
}

TEST_GROUP(LightSmTest) {
};

TEST(LightSmTest, StartsInOFFState) {
    LightSm sm;
    LightSm_ctor(&sm);
    LightSm_start(&sm);
    CHECK_EQUAL(LightSm_StateId_OFF, sm.state_id);
}

