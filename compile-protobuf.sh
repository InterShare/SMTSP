#!/bin/bash

protoc --proto_path=Protobuf/ \
    --csharp_out=Sources/SMTSP/Discovery \
    Protobuf/discovery.proto

protoc --proto_path=Protobuf/ \
    --csharp_out=Sources/SMTSP/Communication \
    Protobuf/communication.proto

# protoc --proto_path=Protobuf/ \
#     --csharp_out=Sources/SMTSP.Generated \
#     Protobuf/*.proto
