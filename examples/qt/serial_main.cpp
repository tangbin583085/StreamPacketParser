#include "ReceiveBytes.hpp"
#include <QCoreApplication>
#include <QSerialPort>
#include <QStringList>
#include <exception>

int main(int argc, char** argv) {
    QCoreApplication app(argc, argv);
    const auto args = app.arguments();
    if (args.size() < 2 || args.size() > 3) {
        qCritical() << "Usage: spp_qt_serial <port-name> [baud-rate]";
        return 2;
    }
    bool valid_baud = true;
    const auto baud = args.size() == 3 ? args[2].toInt(&valid_baud) : 115200;
    if (!valid_baud || baud <= 0) {
        qCritical() << "Baud rate must be a positive integer";
        return 2;
    }
    spp::PacketParser parser(demo::options());
    QSerialPort serial;
    serial.setPortName(args[1]);
    serial.setReadBufferSize(64 * 1024);
    if (!serial.setBaudRate(baud) || !serial.setDataBits(QSerialPort::Data8) ||
        !serial.setParity(QSerialPort::NoParity) || !serial.setStopBits(QSerialPort::OneStop) ||
        !serial.setFlowControl(QSerialPort::NoFlowControl)) {
        qCritical() << serial.errorString();
        return 1;
    }
    if (!serial.open(QIODevice::ReadOnly)) {
        qCritical() << serial.errorString();
        return 1;
    }
    QObject::connect(&serial, &QSerialPort::readyRead, &serial, [&] {
        try { receive_bytes(serial, parser); }
        catch (const std::exception& error) {
            qCritical() << "Receive failed:" << error.what();
            parser.reset();
            serial.close();
            app.exit(1);
        }
    });
    QObject::connect(&serial, &QSerialPort::errorOccurred, &serial,
                     [&](QSerialPort::SerialPortError error) {
        if (error == QSerialPort::NoError) return;
        qCritical() << serial.errorString();
        parser.reset();
        serial.close();
        app.exit(1);
    });
    const auto code = app.exec();
    parser.reset();
    serial.close();
    return code;
}
