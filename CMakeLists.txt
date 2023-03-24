cmake_minimum_required(VERSION 3.16)
project(StreamPacketParser VERSION 3.6.1 LANGUAGES CXX)

add_library(StreamPacketParser STATIC
    src/PacketParser.cpp
    src/PacketParserOptions.cpp
    src/Checksums.cpp
    src/Validators.cpp)
add_library(StreamPacketParser::StreamPacketParser ALIAS StreamPacketParser)
target_compile_features(StreamPacketParser PUBLIC cxx_std_17)
set_target_properties(StreamPacketParser PROPERTIES
    CXX_EXTENSIONS OFF POSITION_INDEPENDENT_CODE ON)
target_include_directories(StreamPacketParser PUBLIC
    "$<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>")

option(SPP_BUILD_EXAMPLES "Build the console example" OFF)
if(SPP_BUILD_EXAMPLES)
    add_subdirectory(examples/console)
endif()

option(SPP_BUILD_TESTS "Build parser tests" OFF)
if(SPP_BUILD_TESTS)
    enable_testing()
    add_subdirectory(tests)
endif()
