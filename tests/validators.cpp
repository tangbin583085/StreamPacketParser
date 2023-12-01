#include "TestSupport.hpp"

int main() {
    return run_test([] {
        const spp::Bytes ascii{'1','2','3','4','5','6','7','8','9'};
        CHECK(spp::crc16_modbus(spp::ByteView(ascii)) == 0x4B37);
        CHECK(spp::crc16_modbus({}) == 0xFFFF);
        CHECK(spp::xor_checksum({}) == 0);
        const spp::Bytes little{1, 3, 0, 0, 0, 10, 0xC5, 0xCD};
        const spp::Bytes big{1, 3, 0, 0, 0, 10, 0xCD, 0xC5};
        auto crc = spp::crc16_modbus_validator();
        CHECK(crc(spp::ByteView(little)).valid());
        CHECK(spp::crc16_modbus_validator(0, 2, spp::ByteOrder::big_endian)(spp::ByteView(big)).valid());
        CHECK(!crc(spp::ByteView(big)).valid());
        CHECK(crc({}).error == spp::ValidationError::frame_too_short);
        auto with_suffix = little;
        with_suffix.push_back(0xEE);
        CHECK(spp::crc16_modbus_validator(0, 3)(spp::ByteView(with_suffix)).valid());
        CHECK(spp::crc16_modbus_validator(99)(spp::ByteView(little)).error ==
              spp::ValidationError::frame_too_short);
        expect_throw<std::invalid_argument>([] { spp::crc16_modbus_validator(0, 1); });
        expect_throw<std::invalid_argument>([] {
            spp::crc16_modbus_validator(0, 2, static_cast<spp::ByteOrder>(42));
        });

        auto xor_check = spp::xor_validator(1);
        const spp::Bytes good{0xAA, 0x10, 0x20, 0x30, 0};
        const spp::Bytes bad{0xAA, 0x10, 0x20, 0x30, 0xFF};
        CHECK(xor_check(spp::ByteView(good)).valid());
        CHECK(xor_check(spp::ByteView(bad)).error == spp::ValidationError::checksum_mismatch);
        CHECK(xor_check({}).error == spp::ValidationError::frame_too_short);
        expect_throw<std::invalid_argument>([] { spp::xor_validator(0, 0); });
        auto options = demo::options();
        options.fixed_frame_overhead = 7;
        options.min_frame_length = 7;
        options.validator = spp::xor_validator(2);
        spp::PacketParser parser(options);
        spp::Bytes frame{0xAA, 0x55, 1, 0x10, 0, 1, 0x42};
        frame.push_back(spp::xor_checksum(spp::ByteView(frame).subview(2, frame.size() - 2)));
        CHECK(parser.append(frame).packets.size() == 1);
    });
}
