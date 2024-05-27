set codegen=.\
mkdir %codegen%
python -m grpc_tools.protoc -Iprotos --python_out=%codegen% --grpc_python_out=%codegen% .\protos\Contracts.proto
python -m grpc_tools.protoc -Iprotos --python_out=%codegen% --grpc_python_out=%codegen%  .\protos\MarketData.proto
python -m grpc_tools.protoc -Iprotos --python_out=%codegen% --grpc_python_out=%codegen%  .\protos\OrderManagementSystem.proto