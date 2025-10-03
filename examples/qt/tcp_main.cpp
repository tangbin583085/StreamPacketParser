#include "ReceiveBytes.hpp"
#include <QCoreApplication>
#include <QTcpSocket>
#include <QStringList>
#include <QTimer>
#include <exception>

int main(int argc, char** argv) {
    QCoreApplication app(argc, argv);
    const auto args = app.arguments();
    if (args.size() != 3) {
        qCritical() << "Usage: spp_qt_tcp <host> <port>";
        return 2;
    }
    bool valid_port = false;
    const auto port = args[2].toUShort(&valid_port);
    if (!valid_port || port == 0) {
        qCritical() << "Port must be between 1 and 65535";
        return 2;
    }
    spp::PacketParser parser(demo::options());
    QTcpSocket socket;
    int exit_code = 0;
    socket.setReadBufferSize(64 * 1024);
    QObject::connect(&socket, &QTcpSocket::connected, &socket, [&] {
        parser.reset();
        qInfo() << "Connected; waiting for demo protocol frames";
    });
    QObject::connect(&socket, &QTcpSocket::readyRead, &socket, [&] {
        try { receive_bytes(socket, parser); }
        catch (const std::exception& error) {
            qCritical() << "Receive failed:" << error.what();
            exit_code = 1;
            parser.reset();
            socket.abort();
            app.exit(exit_code);
        }
    });
    QObject::connect(&socket, &QTcpSocket::disconnected, &socket, [&] {
        parser.reset();
        app.exit(exit_code);
    });
    QObject::connect(&socket, &QTcpSocket::errorOccurred, &socket,
                     [&](QAbstractSocket::SocketError) {
        qCritical() << socket.errorString();
        exit_code = 1;
        parser.reset();
        app.exit(exit_code);
    });
    // Start after the event loop so an immediate connection error can exit it.
    QTimer::singleShot(0, &socket, [&] { socket.connectToHost(args[1], port); });
    return app.exec();
}
