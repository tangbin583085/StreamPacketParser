#include "TestSupport.hpp"

int main() {
    return run_test([] {
        for (auto width : {1u, 2u, 4u}) {
            for (auto order : {spp::ByteOrder::little_endian, spp::ByteOrder::big_endian}) {
                for (auto mode : {spp::FrameLengthMode::payload_length,
                                  spp::FrameLengthMode::total_frame_length}) {
                    spp::PacketParserOptions options;
                    options.length_field_offset = 2;
                    options.length_field_size = width;
                    options.byte_order = order;
                    options.length_mode = mode;
                    options.min_frame_length = 2 + width;
                    options.fixed_frame_overhead =
                        mode == spp::FrameLengthMode::payload_length ? 2 + width : 0;
                    options.payload_offset = 2 + width;
                    spp::Bytes frame{0xAA, 0x55};
                    const auto encoded = mode == spp::FrameLengthMode::payload_length ? 2u : 4u + width;
                    for (unsigned i = 0; i < width; ++i) {
                        const auto shift = 8 * (order == spp::ByteOrder::little_endian ? i : width - 1 - i);
                        frame.push_back(static_cast<std::uint8_t>(encoded >> shift));
                    }
                    frame.insert(frame.end(), {0x11, 0x22});
                    spp::PacketParser parser(options);
                    auto result = parser.append(frame);
                    CHECK(result.packets.size() == 1 && result.packets[0].raw_data == frame);
                    CHECK(result.packets[0].payload == spp::Bytes({0x11, 0x22}));
                    if (mode == spp::FrameLengthMode::total_frame_length) {
                        options.payload_offset.reset();
                        spp::PacketParser no_payload(options);
                        CHECK(no_payload.append(frame).packets[0].payload.empty());
                    }
                }
            }
        }
        auto options = demo::options();
        options.length_field_size = 4;
        options.length_field_offset = 2;
        options.max_frame_length = 64;
        options.max_buffered_bytes = 64;
        spp::PacketParser overflow(options);
        auto result = overflow.append(spp::Bytes{0xAA, 0x55, 0xFF, 0xFF, 0xFF, 0xFF});
        CHECK(result.packets.empty());
        CHECK(has_diagnostic(result, spp::DiagnosticCode::invalid_frame_length));

        options = demo::options();
        options.min_frame_length = 10;
        spp::PacketParser minimum(options);
        result = minimum.append(joined(demo::frame({}), demo::frame({1, 2})));
        CHECK(result.packets.size() == 1);
        CHECK(has_diagnostic(result, spp::DiagnosticCode::invalid_frame_length));
    });
}
