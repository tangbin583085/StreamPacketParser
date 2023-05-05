#include "TestSupport.hpp"

int main() {
    return run_test([] {
        const auto good = demo::frame({0x77});
        spp::PacketParser parser(demo::options());
        auto noise = parser.append(spp::Bytes{0xFF, 0x00, 0xAA});
        CHECK(noise.packets.empty() && parser.buffered_byte_count() == 1);
        CHECK(noise.diagnostics.size() == 1 && noise.diagnostics[0].discarded_bytes == 2);
        auto recovered = parser.append(good.data() + 1, good.size() - 1);
        CHECK(recovered.packets.size() == 1 && recovered.packets[0].raw_data == good);

        auto bad = demo::frame({0x11, 0x22});
        bad.back() ^= 0xFF;
        auto result = parser.append(joined(bad, good));
        CHECK(result.packets.size() == 1 && result.packets[0].raw_data == good);
        CHECK(has_diagnostic(result, spp::DiagnosticCode::validation_failed));

        result = parser.append(joined({0xAA, 0x55, 1, 0x10, 0xFF, 0xFF}, good));
        CHECK(result.packets.size() == 1);
        CHECK(has_diagnostic(result, spp::DiagnosticCode::invalid_frame_length));

        auto options = demo::options();
        options.validator = [calls = 0](spp::ByteView) mutable -> spp::ValidationResult {
            if (calls++ == 0) throw std::runtime_error("validator error");
            return {};
        };
        spp::PacketParser throwing(options);
        result = throwing.append(joined(good, good));
        CHECK(result.packets.size() == 1 && result.packets[0].raw_data == good);
        CHECK(has_diagnostic(result, spp::DiagnosticCode::validator_exception));
        bool found_exception = false;
        for (const auto& diagnostic : result.diagnostics) {
            if (diagnostic.code == spp::DiagnosticCode::validator_exception) {
                CHECK(diagnostic.exception != nullptr);
                expect_throw<std::runtime_error>([&] { std::rethrow_exception(diagnostic.exception); });
                found_exception = true;
            }
        }
        CHECK(found_exception);

        // A header with a repeated prefix must retain the longest matching suffix.
        options = {};
        options.header = {0xAA, 0xAA, 0x55};
        options.length_field_offset = 3;
        options.length_field_size = 1;
        options.fixed_frame_overhead = 4;
        options.min_frame_length = 4;
        options.payload_offset = 4;
        spp::PacketParser overlap(options);
        CHECK(overlap.append(spp::Bytes{0x00, 0xAA, 0xAA}).packets.empty());
        CHECK(overlap.buffered_byte_count() == 2);
        result = overlap.append(spp::Bytes{0x55, 1, 0x42});
        CHECK(result.packets.size() == 1 && result.packets[0].payload == spp::Bytes{0x42});
    });
}
