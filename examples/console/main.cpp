#include "../common/DemoProtocol.hpp"
#include <iomanip>
#include <iostream>

int main() {
    spp::PacketParser parser(demo::options());
    const auto bytes = demo::frame({0x11, 0x22, 0x33});
    const auto first = parser.append(bytes.data(), 5);
    std::cout << "First chunk: " << first.packets.size() << " packet(s)\n";
    const auto result = parser.append(bytes.data() + 5, bytes.size() - 5);
    for (const auto& packet : result.packets) {
        std::cout << "Frame " << packet.frame_length() << " bytes, payload:";
        for (auto byte : packet.payload)
            std::cout << ' ' << std::hex << std::setw(2) << std::setfill('0')
                      << static_cast<unsigned>(byte);
        std::cout << std::dec << '\n';
    }
    return result.packets.size() == 1 && result.diagnostics.empty() ? 0 : 1;
}
