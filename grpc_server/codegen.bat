python -m grpc_tools.protoc -Iprotos --python_out=. --grpc_python_out=. .\protos\enums.proto
python -m grpc_tools.protoc -Iprotos --python_out=. --grpc_python_out=. .\protos\marketdata.proto