"""The Python implementation of the GRPC helloworld.Greeter server."""

import asyncio
import logging
import sys
import grpc

import MarketData_pb2_grpc as services
import OrderManagementSystem_pb2_grpc as OrderManagementSystemService

from terminal.Extensions.MT5 import MT5
from terminal.MarketData import MarketData
from terminal.OrderManagementSystem import OrderManagementSystem


async def serve():
    logger = logging.getLogger("app")

    address = "[::]:5051"
    server = grpc.aio.server()

    services.add_MarketDataServicer_to_server(MarketData(), server)
    OrderManagementSystemService.add_OrderManagementSystemServicer_to_server(
        OrderManagementSystem(), server
    )

    server.add_insecure_port(address)

    await server.start()

    logger.info("listening on %s", address)

    await server.wait_for_termination()


if __name__ == "__main__":
    logging.basicConfig(
        format="%(asctime)s %(levelname)s:%(name)s: %(message)s",
        level=logging.INFO,
        datefmt="%H:%M:%S",
        stream=sys.stderr,
    )

    logging.getLogger("chardet.charsetprober").disabled = True

    MT5.initialize()

    logging.basicConfig(level=logging.INFO)
    asyncio.get_event_loop().run_until_complete(serve())
