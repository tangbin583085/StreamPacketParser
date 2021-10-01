#pragma once
#include "ByteOrder.hpp"
#include "FrameLengthMode.hpp"
#include "FrameValidation.hpp"
#include <optional>

namespace spp {
struct PacketParserOptions {
    Bytes header{0xAA, 0x55};
    std::size_t length_field_offset = 4;
    std::size_t length_field_size = 2;
    ByteOrder byte_order = ByteOrder::big_endian;
    FrameLengthMode length_mode = FrameLengthMode::payload_length;
    std::size_t fixed_frame_overhead = 8;
    std::size_t min_frame_length = 8;
    std::size_t max_frame_length = 4096;
    std::size_t max_buffered_bytes = 8192;
    std::optional<std::size_t> payload_offset;
    FrameValidator validator;

    // Throws std::invalid_argument for inconsistent layouts.
    void validate() const;
};
}
