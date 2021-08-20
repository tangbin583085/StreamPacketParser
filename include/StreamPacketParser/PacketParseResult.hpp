#pragma once
#include "ParsedPacket.hpp"
#include "ParserDiagnostic.hpp"
#include <vector>

namespace spp {
struct PacketParseResult {
    std::vector<ParsedPacket> packets;
    std::vector<ParserDiagnostic> diagnostics;
};
}
