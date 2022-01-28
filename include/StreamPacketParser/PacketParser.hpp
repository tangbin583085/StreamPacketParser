#pragma once
#include "PacketParserOptions.hpp"
#include "PacketParseResult.hpp"
#include <memory>

namespace spp {
// One instance per ordered stream; append/reset must not be concurrent or reentrant.
class PacketParser {
public:
    explicit PacketParser(PacketParserOptions options = {});
    ~PacketParser();
    PacketParser(const PacketParser&) = delete;
    PacketParser& operator=(const PacketParser&) = delete;

    PacketParseResult append(ByteView data);
    PacketParseResult append(const Bytes& data) { return append(ByteView(data)); }
    PacketParseResult append(const std::uint8_t* data, std::size_t size) {
        return append(ByteView(data, size));
    }
    std::size_t buffered_byte_count() const noexcept;
    void reset() noexcept;
private:
    struct Impl;
    std::unique_ptr<Impl> impl_;
};
}
