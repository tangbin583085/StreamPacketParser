#pragma once
#include <StreamPacketParser/ByteView.hpp>
#include <algorithm>
#include <stdexcept>

namespace spp::detail {
// Sliding storage: discarding a bad byte does not move the rest of the frame.
class ByteBuffer {
public:
    std::size_t size() const noexcept { return storage_.size() - start_; }
    ByteView view() const {
        return ByteView(storage_).subview(start_, size());
    }
    void append(ByteView input, std::size_t limit) {
        if (size() > limit || input.size() > limit - size())
            throw std::length_error("parser buffer limit exceeded");
        if (input.empty()) return;
        if (start_ && input.size() > limit - storage_.size()) {
            std::move(storage_.begin() + static_cast<std::ptrdiff_t>(start_),
                      storage_.end(), storage_.begin());
            storage_.resize(size());
            start_ = 0;
        }
        storage_.insert(storage_.end(), input.begin(), input.end());
    }
    void discard(std::size_t count) {
        if (count > size()) throw std::out_of_range("buffer discard");
        start_ += count;
        if (start_ == storage_.size()) clear();
    }
    void clear() noexcept { storage_.clear(); start_ = 0; }
private:
    Bytes storage_;
    std::size_t start_ = 0;
};
}
