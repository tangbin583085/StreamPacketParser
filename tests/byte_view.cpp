#include "TestSupport.hpp"

int main() {
    return run_test([] {
        const spp::ByteView empty;
        CHECK(empty.empty() && empty.begin() == empty.end());
        CHECK(empty.subview(0, 0).empty());
        expect_throw<std::invalid_argument>([] { spp::ByteView view(nullptr, 1); });
        const spp::Bytes bytes{1, 2, 3};
        const spp::ByteView view(bytes);
        CHECK(view.size() == 3 && view[1] == 2);
        CHECK(view.subview(1, 2)[1] == 3);
        CHECK(view.subview(3, 0).empty());
        expect_throw<std::out_of_range>([&] { view.subview(4, 0); });
        expect_throw<std::out_of_range>([&] { view.subview(1, 3); });
    });
}
