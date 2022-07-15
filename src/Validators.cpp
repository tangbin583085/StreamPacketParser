#include <StreamPacketParser/Validators.hpp>
#include <StreamPacketParser/Checksums.hpp>
#include <stdexcept>

namespace spp {
FrameValidator xor_validator(std::size_t start, std::size_t from_end) {
    if (from_end < 1) throw std::invalid_argument("XOR offset from end must be at least 1");
    return [start, from_end](ByteView frame) -> ValidationResult {
        if (from_end > frame.size() || start > frame.size() - from_end)
            return {ValidationError::frame_too_short, "frame too short for XOR range"};
        const auto index = frame.size() - from_end;
        if (xor_checksum(frame.subview(start, index - start)) != frame[index])
            return {ValidationError::checksum_mismatch, "XOR checksum mismatch"};
        return {};
    };
}
FrameValidator crc16_modbus_validator(std::size_t start, std::size_t from_end, ByteOrder order) {
    if (from_end < 2) throw std::invalid_argument("CRC offset from end must be at least 2");
    if (order != ByteOrder::little_endian && order != ByteOrder::big_endian)
        throw std::invalid_argument("unsupported checksum byte order");
    return [start, from_end, order](ByteView frame) -> ValidationResult {
        if (from_end > frame.size() || start > frame.size() - from_end)
            return {ValidationError::frame_too_short, "frame too short for CRC range"};
        const auto index = frame.size() - from_end;
        const auto expected = order == ByteOrder::little_endian
            ? static_cast<std::uint16_t>(frame[index] | (frame[index + 1] << 8))
            : static_cast<std::uint16_t>((frame[index] << 8) | frame[index + 1]);
        if (crc16_modbus(frame.subview(start, index - start)) != expected)
            return {ValidationError::checksum_mismatch, "CRC16-Modbus checksum mismatch"};
        return {};
    };
}
}
