#include <StreamPacketParser/PacketParserOptions.hpp>
#include <limits>
#include <stdexcept>

namespace spp {
void PacketParserOptions::validate() const {
    const auto require = [](bool condition, const char* message) {
        if (!condition) throw std::invalid_argument(message);
    };
    require(!header.empty(), "header must not be empty");
    require(length_field_offset >= header.size(), "length field overlaps header");
    require(length_field_size == 1 || length_field_size == 2 || length_field_size == 4,
            "length field size must be 1, 2, or 4");
    require(byte_order == ByteOrder::little_endian || byte_order == ByteOrder::big_endian,
            "unsupported byte order");
    require(length_mode == FrameLengthMode::payload_length ||
            length_mode == FrameLengthMode::total_frame_length, "unsupported length mode");
    require(length_field_offset <= std::numeric_limits<std::size_t>::max() - length_field_size,
            "length field offset overflows");
    const auto field_end = length_field_offset + length_field_size;
    require(min_frame_length >= field_end, "minimum frame must cover length field");
    require(max_frame_length >= min_frame_length, "maximum frame is smaller than minimum");
    require(max_buffered_bytes >= max_frame_length, "buffer must hold maximum frame");
    require(max_buffered_bytes <= static_cast<std::size_t>(std::numeric_limits<std::ptrdiff_t>::max()),
            "buffer exceeds supported iterator range");
    if (length_mode == FrameLengthMode::payload_length) {
        require(fixed_frame_overhead >= field_end && fixed_frame_overhead <= max_frame_length,
                "fixed overhead must cover length field and fit maximum frame");
        const auto offset = payload_offset.value_or(field_end);
        require(offset <= fixed_frame_overhead, "payload offset exceeds fixed overhead");
    } else {
        require(fixed_frame_overhead == 0, "total frame mode requires zero fixed overhead");
    }
    require(!payload_offset || *payload_offset <= min_frame_length,
            "payload offset exceeds minimum frame");
}
}
