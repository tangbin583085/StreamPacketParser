#pragma once
#include "../common/DemoProtocol.hpp"
#include <QByteArray>
#include <QDebug>
#include <QIODevice>

// Call on the device's thread. Keep one parser for each connection/session.
inline void receive_bytes(QIODevice& device, spp::PacketParser& parser) {
    while (device.bytesAvailable() > 0) {
        const QByteArray bytes = device.read(64 * 1024);
        if (bytes.isEmpty()) break;
        const auto result = parser.append(
            reinterpret_cast<const std::uint8_t*>(bytes.constData()),
            static_cast<std::size_t>(bytes.size()));
        for (const auto& diagnostic : result.diagnostics)
            qWarning() << "Parser:" << diagnostic.message.c_str()
                       << "discarded" << static_cast<qulonglong>(diagnostic.discarded_bytes);
        for (const auto& packet : result.packets) {
            // Demo frame size is bounded to 4096, so the Qt 5 int conversion is safe.
            const QByteArray payload(reinterpret_cast<const char*>(packet.payload.data()),
                                     static_cast<int>(packet.payload.size()));
            qInfo() << "Frame bytes:" << static_cast<qulonglong>(packet.frame_length())
                    << "payload:" << payload.toHex(' ');
            // Dispatch your command handler here. Copy data for queued cross-thread delivery.
        }
    }
}
