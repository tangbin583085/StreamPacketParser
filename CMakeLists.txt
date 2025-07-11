cmake_minimum_required(VERSION 3.16)
project(StreamPacketParser VERSION 3.6.1 LANGUAGES CXX)

option(SPP_BUILD_TESTS "Build parser tests" OFF)
option(SPP_BUILD_EXAMPLES "Build the console example" OFF)
option(SPP_BUILD_QT_TCP_EXAMPLE "Build the Qt TCP example" OFF)
option(SPP_BUILD_QT_SERIAL_EXAMPLE "Build the Qt serial example" OFF)

add_library(StreamPacketParser STATIC
    src/PacketParser.cpp
    src/PacketParserOptions.cpp
    src/Checksums.cpp
    src/Validators.cpp)
add_library(StreamPacketParser::StreamPacketParser ALIAS StreamPacketParser)
target_compile_features(StreamPacketParser PUBLIC cxx_std_17)
set_target_properties(StreamPacketParser PROPERTIES
    CXX_EXTENSIONS OFF POSITION_INDEPENDENT_CODE ON)
include(GNUInstallDirs)
target_include_directories(StreamPacketParser PUBLIC
    "$<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>"
    "$<INSTALL_INTERFACE:${CMAKE_INSTALL_INCLUDEDIR}>")

include(CMakePackageConfigHelpers)
install(TARGETS StreamPacketParser EXPORT StreamPacketParserTargets
    ARCHIVE DESTINATION ${CMAKE_INSTALL_LIBDIR})
install(DIRECTORY include/ DESTINATION ${CMAKE_INSTALL_INCLUDEDIR})
set(SPP_CONFIG_DIR "${CMAKE_INSTALL_LIBDIR}/cmake/StreamPacketParser")
configure_package_config_file(cmake/StreamPacketParserConfig.cmake.in
    "${CMAKE_CURRENT_BINARY_DIR}/StreamPacketParserConfig.cmake"
    INSTALL_DESTINATION "${SPP_CONFIG_DIR}")
write_basic_package_version_file(
    "${CMAKE_CURRENT_BINARY_DIR}/StreamPacketParserConfigVersion.cmake"
    VERSION ${PROJECT_VERSION} COMPATIBILITY SameMajorVersion)
install(EXPORT StreamPacketParserTargets NAMESPACE StreamPacketParser::
    DESTINATION "${SPP_CONFIG_DIR}")
install(FILES
    "${CMAKE_CURRENT_BINARY_DIR}/StreamPacketParserConfig.cmake"
    "${CMAKE_CURRENT_BINARY_DIR}/StreamPacketParserConfigVersion.cmake"
    DESTINATION "${SPP_CONFIG_DIR}")
install(FILES LICENSE DESTINATION "${CMAKE_INSTALL_DATADIR}/StreamPacketParser")

if(SPP_BUILD_TESTS)
    enable_testing()
    add_subdirectory(tests)
endif()
if(SPP_BUILD_EXAMPLES)
    add_subdirectory(examples/console)
endif()
if(SPP_BUILD_QT_TCP_EXAMPLE OR SPP_BUILD_QT_SERIAL_EXAMPLE)
    add_subdirectory(examples/qt)
endif()
