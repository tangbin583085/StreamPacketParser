#include "TestSupport.hpp"

int main() {
    return run_test([] {
        auto options = demo::options();
        options.max_frame_length = 64;
        options.max_buffered_bytes = 64;
        spp::PacketParser parser(options);
        auto result = parser.append(spp::Bytes(10000, 0xFF));
        CHECK(result.packets.empty());
        CHECK(parser.buffered_byte_count() == 0);
        std::size_t discarded = 0;
        for (const auto& diagnostic : result.diagnostics) discarded += diagnostic.discarded_bytes;
        CHECK(discarded == 10000);

        const auto largest = demo::frame(spp::Bytes(56, 0x11));
        result = parser.append(largest);
        CHECK(result.packets.size() == 1 && result.packets[0].frame_length() == 64);
        CHECK(parser.buffered_byte_count() == 0);
        spp::Bytes stream;
        for (int i = 0; i < 200; ++i) stream.insert(stream.end(), largest.begin(), largest.end());
        result = parser.append(stream);
        CHECK(result.packets.size() == 200 && result.diagnostics.empty());
        CHECK(parser.buffered_byte_count() == 0);

        const auto small = demo::frame({0x21});
        // Repeated chunks cross the sliding buffer's compaction boundary.
        stream.clear();
        for (int i = 0; i < 150; ++i) stream.insert(stream.end(), small.begin(), small.end());
        std::size_t packets = 0;
        for (std::size_t offset = 0; offset < stream.size();) {
            const auto count = std::min<std::size_t>(31, stream.size() - offset);
            result = parser.append(stream.data() + offset, count);
            CHECK(result.diagnostics.empty());
            for (const auto& packet : result.packets) CHECK(packet.raw_data == small);
            packets += result.packets.size();
            CHECK(parser.buffered_byte_count() <= options.max_buffered_bytes);
            offset += count;
        }
        CHECK(packets == 150 && parser.buffered_byte_count() == 0);
    });
}
