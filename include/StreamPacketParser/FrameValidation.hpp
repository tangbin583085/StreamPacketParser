#pragma once
#include "ByteView.hpp"
#include <functional>
#include <string>

namespace spp {
enum class ValidationError { none, frame_too_short, checksum_mismatch };
struct ValidationResult {
    ValidationError error = ValidationError::none;
    std::string message;
    bool valid() const noexcept { return error == ValidationError::none; }
};
// Empty std::function means no validation. Exceptions become parser diagnostics.
using FrameValidator = std::function<ValidationResult(ByteView)>;
}
