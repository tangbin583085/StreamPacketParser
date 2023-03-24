#include "TestSupport.hpp"
#include <random>

int main() {
    return run_test([] {
        const auto first = demo::frame({0x11, 0x22, 0x33});
        const auto second = demo::frame({});
        const auto third = demo::frame({0xAA, 0x55, 0x01});
        const auto stream = joined(joined(first, second), third);
        // Every possible two-chunk split, including empty first/last input.
        for (std::size_t split = 0; split <= stream.size(); ++split) {
            spp::PacketParser parser(demo::options());
            auto a = parser.append(stream.data(), split);
            auto b = parser.append(stream.data() + split, stream.size() - split);
            CHECK(a.diagnostics.empty() && b.diagnostics.empty());
            a.packets.insert(a.packets.end(), b.packets.begin(), b.packets.end());
            CHECK(a.packets.size() == 3);
            CHECK(a.packets[0].raw_data == first);
            CHECK(a.packets[0].payload == spp::Bytes({0x11, 0x22, 0x33}));
            CHECK(a.packets[1].raw_data == second && a.packets[1].payload.empty());
            CHECK(a.packets[2].raw_data == third);
            CHECK(parser.buffered_byte_count() == 0);
        }
        for (unsigned seed = 0; seed < 50; ++seed) {
            spp::PacketParser parser(demo::options());
            std::mt19937 rng(seed);
            std::vector<spp::ParsedPacket> packets;
            for (std::size_t offset = 0; offset < stream.size();) {
                const auto count = std::min<std::size_t>(1 + rng() % 7, stream.size() - offset);
                auto result = parser.append(stream.data() + offset, count);
                CHECK(result.diagnostics.empty());
                packets.insert(packets.end(), result.packets.begin(), result.packets.end());
                offset += count;
            }
            CHECK(packets.size() == 3);
            CHECK(packets[0].raw_data == first && packets[1].raw_data == second &&
                  packets[2].raw_data == third);
        }
        spp::PacketParser parser(demo::options());
        std::size_t count = 0;
        for (auto byte : first) count += parser.append(&byte, 1).packets.size();
        CHECK(count == 1 && parser.buffered_byte_count() == 0);
    });
}
