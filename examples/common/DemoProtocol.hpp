#pragma once
#include <StreamPacketParser/StreamPacketParser.hpp>
#include <stdexcept>

namespace demo {
inline spp::PacketParserOptions options() {
    spp::PacketParserOptions options;
    options.payload_offset = 6;
    options.validator = spp::crc16_modbus_validator(2);
    return options;
}
// AA 55 | version | command | payload length BE16 | payload | CRC LE16
inline spp::Bytes frame(const spp::Bytes& payload, std::uint8_t command = 0x10) {
    if (payload.size() > 4088) throw std::invalid_argument("demo payload too large");
    spp::Bytes bytes{0xAA, 0x55, 0x01, command,
                     static_cast<std::uint8_t>(payload.size() >> 8),
                     static_cast<std::uint8_t>(payload.size())};
    bytes.insert(bytes.end(), payload.begin(), payload.end());
    const auto crc = spp::crc16_modbus(spp::ByteView(bytes).subview(2, bytes.size() - 2));
    bytes.push_back(static_cast<std::uint8_t>(crc));
    bytes.push_back(static_cast<std::uint8_t>(crc >> 8));
    return bytes;
}
}
