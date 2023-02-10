#pragma once
#include <StreamPacketParser/StreamPacketParser.hpp>
#include "../examples/common/DemoProtocol.hpp"
#include <algorithm>
#include <iostream>
#include <stdexcept>
#include <string>

// Deliberately independent of assert/NDEBUG so checks also run in Release builds.
#define CHECK(...) do { if (!(__VA_ARGS__)) throw std::runtime_error( \
    std::string(__FILE__) + ":" + std::to_string(__LINE__) + ": " + #__VA_ARGS__); } while (false)

template <typename Exception, typename Function>
void expect_throw(Function&& function) {
    bool thrown = false;
    try { function(); } catch (const Exception&) { thrown = true; }
    CHECK(thrown);
}
inline spp::Bytes joined(spp::Bytes first, const spp::Bytes& second) {
    first.insert(first.end(), second.begin(), second.end());
    return first;
}
inline bool has_diagnostic(const spp::PacketParseResult& result, spp::DiagnosticCode code) {
    return std::any_of(result.diagnostics.begin(), result.diagnostics.end(),
        [code](const spp::ParserDiagnostic& diagnostic) { return diagnostic.code == code; });
}
template <typename Function>
int run_test(Function&& function) {
    try { function(); return 0; }
    catch (const std::exception& error) { std::cerr << error.what() << '\n'; return 1; }
    catch (...) { std::cerr << "unexpected non-standard exception\n"; return 1; }
}
