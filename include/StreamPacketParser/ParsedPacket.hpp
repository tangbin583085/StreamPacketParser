#pragma once
#include "ByteView.hpp"

namespace spp {
// Both vectors own their bytes and remain valid after append(), reset(), or parser destruction.
struct ParsedPacket {
    Bytes raw_data;
    Bytes payload;
    std::size_t frame_length() const noexcept { return raw_data.size(); }
};
}
