"""The Python implementation of the GRPC helloworld.Greeter server."""

import logging
import sys
from concurrent import futures

import grpc

import marketdata_pb2_grpc as marketDataService
import ordermanagement_pb2_grpc as orderManagementService
from terminal.marketdata import MarketData
from terminal.ordermanagement import OrderManagement
import MetaTrader5 as mt5

logging.basicConfig(
    format="%(asctime)s %(levelname)s:%(name)s: %(message)s",
    level=logging.DEBUG,
    datefmt="%H:%M:%S",
    stream=sys.stderr,
)
logging.getLogger("chardet.charsetprober").disabled = True
logger = logging.getLogger("app")


def serve():
    mt5.initialize()
    server = grpc.server(futures.ThreadPoolExecutor(max_workers=50))
    marketDataService.add_MarketDataServicer_to_server(MarketData(), server)
    orderManagementService.add_OrderManagementServicer_to_server(
        OrderManagement(), server)
    server.add_insecure_port("[::]:5051")
    server.start()
    server.wait_for_termination()


if __name__ == "__main__":
    logger.info("listening...")
    serve()
