#pragma once
#include "ByteView.hpp"
namespace spp {
std::uint8_t xor_checksum(ByteView data) noexcept;
std::uint16_t crc16_modbus(ByteView data) noexcept;
}
