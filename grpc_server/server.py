"""The Python implementation of the GRPC helloworld.Greeter server."""

import logging
import sys
from concurrent import futures

import grpc
import MetaTrader5 as mt5

import codegen.terminal_pb2 as protos
import codegen.terminal_pb2_grpc as services

logging.basicConfig(
    format="%(asctime)s %(levelname)s:%(name)s: %(message)s",
    level=logging.DEBUG,
    datefmt="%H:%M:%S",
    stream=sys.stderr,
)
logging.getLogger("chardet.charsetprober").disabled = True
logger = logging.getLogger("app")


def initplatform():
    if not mt5.initialize():
        logger.error("initialize() failed, error code = %s", mt5.last_error())
        return False

    return True


class Terminal(services.TerminalServicer):
    def CopyTicksRange(self, request, context):

        if not initplatform():
            return protos.CopyTicksRangeReply()

        symbol = request.symbol.upper()

        ticks = mt5.copy_ticks_range(
            symbol, request.fromdate, request.todate, request.type)

        print(ticks)

        return protos.CopyTicksRangeReply()


def serve():
    server = grpc.server(futures.ThreadPoolExecutor(max_workers=10))
    services.add_TerminalServicer_to_server(Terminal(), server)
    server.add_insecure_port("[::]:5051")
    server.start()
    server.wait_for_termination()


if __name__ == "__main__":
    logger.info("listening...")
    serve()
