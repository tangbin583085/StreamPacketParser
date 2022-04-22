#include <StreamPacketParser/Checksums.hpp>
namespace spp {
std::uint8_t xor_checksum(ByteView data) noexcept {
    std::uint8_t value = 0;
    for (auto byte : data) value ^= byte;
    return value;
}
std::uint16_t crc16_modbus(ByteView data) noexcept {
    std::uint16_t crc = 0xFFFF;
    for (auto byte : data) {
        crc ^= byte;
        for (int bit = 0; bit < 8; ++bit)
            crc = static_cast<std::uint16_t>((crc >> 1) ^ ((crc & 1) ? 0xA001 : 0));
    }
    return crc;
}
}
