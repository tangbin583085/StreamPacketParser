#pragma once

#include <cstddef>
#include <cstdint>
#include <limits>
#include <stdexcept>
#include <vector>

namespace spp {
using Bytes = std::vector<std::uint8_t>;

// Non-owning input view. The caller keeps the underlying bytes alive.
class ByteView {
public:
    ByteView() noexcept = default;
    ByteView(const std::uint8_t* data, std::size_t size) : data_(data), size_(size) {
        if (!data && size != 0) throw std::invalid_argument("null data with nonzero size");
        if (size > static_cast<std::size_t>(std::numeric_limits<std::ptrdiff_t>::max()))
            throw std::length_error("byte view exceeds supported iterator range");
    }
    explicit ByteView(const Bytes& bytes) : ByteView(bytes.data(), bytes.size()) {}
    const std::uint8_t* data() const noexcept { return data_; }
    std::size_t size() const noexcept { return size_; }
    bool empty() const noexcept { return size_ == 0; }
    const std::uint8_t* begin() const noexcept { return data_; }
    const std::uint8_t* end() const noexcept { return size_ ? data_ + size_ : data_; }
    std::uint8_t operator[](std::size_t index) const noexcept { return data_[index]; }
    ByteView subview(std::size_t offset, std::size_t count) const {
        if (offset > size_ || count > size_ - offset)
            throw std::out_of_range("byte view range");
        return ByteView(offset ? data_ + offset : data_, count);
    }
private:
    const std::uint8_t* data_ = nullptr;
    std::size_t size_ = 0;
};
} // namespace spp
