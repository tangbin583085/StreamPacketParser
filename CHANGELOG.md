# Changes

## 3.6.1

- C++17 parser with CMake source integration and installed-package support.
- Payload and total-frame length modes, with 1/2/4-byte fields in both byte orders.
- XOR and CRC16-Modbus validation, including configurable checksum placement.
- Partial-header retention, malformed-frame recovery, and owned packet results.
- Console, Qt TCP, and Qt serial receive examples.
- Tests for chunking, lengths, validation, buffer limits, and parser lifetime.

See README for usage and docs/release-checklist.md for validation steps.
