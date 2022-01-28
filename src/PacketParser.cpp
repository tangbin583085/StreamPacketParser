#include <StreamPacketParser/PacketParser.hpp>
#include "ByteBuffer.hpp"
#include <algorithm>
#include <cstdint>
#include <utility>

namespace spp {
struct PacketParser::Impl {
    explicit Impl(PacketParserOptions config) : options(std::move(config)) {
        options.validate();
    }
    PacketParserOptions options;
    detail::ByteBuffer buffer;

    static void report(PacketParseResult& result, DiagnosticCode code,
                       const char* message, std::size_t discarded) {
        result.diagnostics.push_back({code, message, discarded, {}});
    }

    std::size_t partial_header(ByteView data) const {
        auto count = std::min(data.size(), options.header.size() - 1);
        for (; count > 0; --count) {
            if (std::equal(data.end() - count, data.end(), options.header.begin()))
                return count;
        }
        return 0;
    }

    std::uint32_t read_length(ByteView data) const noexcept {
        std::uint32_t value = 0;
        for (std::size_t i = 0; i < options.length_field_size; ++i) {
            const auto byte = static_cast<std::uint32_t>(data[options.length_field_offset + i]);
            if (options.byte_order == ByteOrder::little_endian) value |= byte << (8 * i);
            else value = (value << 8) | byte;
        }
        return value;
    }

    void parse(PacketParseResult& result) {
        while (buffer.size() != 0) {
            const auto data = buffer.view();
            const auto found = std::search(data.begin(), data.end(),
                                           options.header.begin(), options.header.end());
            if (found == data.end()) {
                const auto discarded = data.size() - partial_header(data);
                if (discarded) {
                    report(result, DiagnosticCode::noise_discarded, "noise before header", discarded);
                    buffer.discard(discarded);
                }
                return;
            }
            const auto noise = static_cast<std::size_t>(found - data.begin());
            if (noise != 0) {
                report(result, DiagnosticCode::noise_discarded, "noise before header", noise);
                buffer.discard(noise);
                continue;
            }
            if (data.size() < options.length_field_offset + options.length_field_size) return;
            const auto encoded = read_length(data);
            // Subtraction avoids overflow even on 32-bit targets.
            const auto overhead = options.length_mode == FrameLengthMode::payload_length
                                    ? options.fixed_frame_overhead : 0;
            if (encoded > options.max_frame_length - overhead ||
                static_cast<std::size_t>(encoded) + overhead < options.min_frame_length) {
                report(result, DiagnosticCode::invalid_frame_length, "length outside configured bounds", 1);
                buffer.discard(1);
                continue;
            }
            const auto length = static_cast<std::size_t>(encoded) + overhead;
            if (data.size() < length) return;
            const auto frame = data.subview(0, length);
            if (options.validator) {
                ValidationResult validation;
                try {
                    validation = options.validator(frame);
                } catch (...) {
                    result.diagnostics.push_back({DiagnosticCode::validator_exception,
                                                  "frame validator threw", 1, std::current_exception()});
                    buffer.discard(1);
                    continue;
                }
                if (!validation.valid()) {
                    result.diagnostics.push_back({DiagnosticCode::validation_failed,
                                                  validation.message, 1, {}});
                    buffer.discard(1);
                    continue;
                }
            }
            std::size_t offset = 0;
            std::size_t payload_size = 0;
            if (options.length_mode == FrameLengthMode::payload_length) {
                offset = options.payload_offset.value_or(options.length_field_offset + options.length_field_size);
                payload_size = encoded;
            } else if (options.payload_offset) {
                offset = *options.payload_offset;
                payload_size = length - offset;
            }
            ParsedPacket packet;
            packet.raw_data.assign(frame.begin(), frame.end());
            if (payload_size) {
                const auto payload = frame.subview(offset, payload_size);
                packet.payload.assign(payload.begin(), payload.end());
            }
            result.packets.push_back(std::move(packet));
            buffer.discard(length);
        }
    }
};

PacketParser::PacketParser(PacketParserOptions options)
    : impl_(std::make_unique<Impl>(std::move(options))) {}
PacketParser::~PacketParser() = default;

PacketParseResult PacketParser::append(ByteView data) {
    PacketParseResult result;
    while (!data.empty()) {
        impl_->parse(result);
        const auto limit = impl_->options.max_buffered_bytes;
        if (impl_->buffer.size() >= limit) {
            Impl::report(result, DiagnosticCode::buffer_limit_exceeded, "buffer full; discarded one byte", 1);
            impl_->buffer.discard(1);
            continue;
        }
        const auto count = std::min(data.size(), limit - impl_->buffer.size());
        impl_->buffer.append(data.subview(0, count), limit);
        data = data.subview(count, data.size() - count);
    }
    impl_->parse(result);
    return result;
}
std::size_t PacketParser::buffered_byte_count() const noexcept { return impl_->buffer.size(); }
void PacketParser::reset() noexcept { impl_->buffer.clear(); }
}
