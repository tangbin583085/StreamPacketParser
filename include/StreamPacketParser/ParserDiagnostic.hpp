#pragma once
#include <cstddef>
#include <exception>
#include <string>

namespace spp {
enum class DiagnosticCode {
    noise_discarded,
    invalid_frame_length,
    validation_failed,
    validator_exception,
    buffer_limit_exceeded
};
struct ParserDiagnostic {
    DiagnosticCode code;
    std::string message;
    std::size_t discarded_bytes = 0;
    std::exception_ptr exception;
};
}
