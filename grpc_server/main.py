"""The Python implementation of the GRPC helloworld.Greeter server."""

import logging
import sys
from concurrent import futures

import grpc

import marketdata_pb2_grpc as services
from marketdata import MarketData

logging.basicConfig(
    format="%(asctime)s %(levelname)s:%(name)s: %(message)s",
    level=logging.DEBUG,
    datefmt="%H:%M:%S",
    stream=sys.stderr,
)
logging.getLogger("chardet.charsetprober").disabled = True
logger = logging.getLogger("app")


def serve():
    server = grpc.server(futures.ThreadPoolExecutor(max_workers=50))
    services.add_MarketDataServicer_to_server(MarketData(), server)
    server.add_insecure_port("[::]:5051")
    server.start()
    server.wait_for_termination()


if __name__ == "__main__":
    logger.info("listening...")
    serve()
