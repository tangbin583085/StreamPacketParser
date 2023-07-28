#include "TestSupport.hpp"

int main() {
    return run_test([] {
        auto options = demo::options();
        spp::PacketParser parser(options);
        options.header[0] = 0;
        options.payload_offset = 0; // Parser owns a snapshot of the configuration.
        auto bytes = demo::frame({0x11, 0x12});
        auto result = parser.append(bytes);
        CHECK(result.packets.size() == 1);
        const auto expected = bytes;
        bytes.assign(bytes.size(), 0);
        parser.append(demo::frame({0x22}));
        parser.reset();
        CHECK(result.packets[0].raw_data == expected);
        CHECK(result.packets[0].payload == spp::Bytes({0x11, 0x12}));

        parser.append(expected.data(), 4);
        CHECK(parser.buffered_byte_count() == 4);
        CHECK(parser.append(nullptr, 0).packets.empty());
        CHECK(parser.buffered_byte_count() == 4);
        parser.reset();
        parser.reset();
        CHECK(parser.buffered_byte_count() == 0);
        CHECK(parser.append(expected).packets.size() == 1);
        expect_throw<std::invalid_argument>([&] { parser.append(nullptr, 1); });

        spp::PacketParseResult owned;
        {
            spp::PacketParser temporary(demo::options());
            owned = temporary.append(expected);
        }
        CHECK(owned.packets[0].raw_data == expected);
    });
}
