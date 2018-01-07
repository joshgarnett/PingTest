#!/bin/bash

# build with protoc 3.4.1

mkdir -p Protocol
protoc --proto_path=./ --csharp_out=Protocol ping.proto
