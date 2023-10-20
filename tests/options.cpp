#include "TestSupport.hpp"
#include <limits>

int main() {
    return run_test([] {
        spp::PacketParserOptions{}.validate();
        const auto invalid = [](auto change) {
            spp::PacketParserOptions options;
            change(options);
            expect_throw<std::invalid_argument>([&] { spp::PacketParser parser(options); });
        };
        invalid([](auto& o) { o.header.clear(); });
        invalid([](auto& o) { o.length_field_offset = 1; });
        for (auto width : {0u, 3u, 8u})
            invalid([width](auto& o) { o.length_field_size = width; });
        invalid([](auto& o) { o.length_field_offset = std::numeric_limits<std::size_t>::max(); });
        invalid([](auto& o) { o.min_frame_length = 5; });
        invalid([](auto& o) { o.max_frame_length = 7; });
        invalid([](auto& o) { o.max_buffered_bytes = 100; });
        invalid([](auto& o) { o.byte_order = static_cast<spp::ByteOrder>(42); });
        invalid([](auto& o) { o.length_mode = static_cast<spp::FrameLengthMode>(42); });
        invalid([](auto& o) { o.fixed_frame_overhead = 5; });
        invalid([](auto& o) { o.fixed_frame_overhead = 4097; });
        invalid([](auto& o) { o.length_mode = spp::FrameLengthMode::total_frame_length; });
        invalid([](auto& o) { o.payload_offset = 9; });
        invalid([](auto& o) { o.min_frame_length = 12; o.payload_offset = 10; });
        invalid([](auto& o) { o.max_buffered_bytes = std::numeric_limits<std::size_t>::max(); });
        // Default no-validation mode accepts a frame without checksum validation.
        spp::PacketParser parser;
        const auto result = parser.append(spp::Bytes{0xAA, 0x55, 1, 0x10, 0, 0, 0, 0});
        CHECK(result.packets.size() == 1);
    });
}
