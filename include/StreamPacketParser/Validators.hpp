#pragma once
#include "ByteOrder.hpp"
#include "FrameValidation.hpp"

namespace spp {
// Both validators cover [data_start_offset, frame.size() - checksum_offset_from_end).
FrameValidator xor_validator(std::size_t data_start_offset = 0,
                             std::size_t checksum_offset_from_end = 1);
FrameValidator crc16_modbus_validator(std::size_t data_start_offset = 0,
                                     std::size_t checksum_offset_from_end = 2,
                                     ByteOrder checksum_byte_order = ByteOrder::little_endian);
}
