python -m grpc_tools.protoc -Iprotos --python_out=. --grpc_python_out=. .\protos\contract.proto
python -m grpc_tools.protoc -Iprotos --python_out=. --grpc_python_out=. .\protos\marketdata.proto
python -m grpc_tools.protoc -Iprotos --python_out=. --grpc_python_out=. .\protos\ordermanagement.proto