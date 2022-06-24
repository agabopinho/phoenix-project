"""The Python implementation of the GRPC helloworld.Greeter server."""

import json
import logging
import sys
from concurrent import futures
from datetime import datetime

import google.protobuf.timestamp_pb2 as protoTimestamp
import google.protobuf.wrappers_pb2 as protoWrappers
import grpc
import MetaTrader5 as mt5
import pandas as pd
import pytz
from matplotlib.pyplot import flag

import marketdata_pb2 as protos
import marketdata_pb2_grpc as services

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


class Terminal(services.MarketDataServicer):
    def CopyTicksRange(self, request: protos.CopyTicksRangeRequest, context):
        if not initplatform():
            return protos.CopyTicksRangeReply(success=False)

        symbol = request.symbol.upper()
        fromDate = datetime.fromtimestamp(
            request.fromDate.seconds, tz=pytz.utc)
        toDate = datetime.fromtimestamp(request.toDate.seconds, tz=pytz.utc)
        copyTicks = mt5.COPY_TICKS_ALL if request.type == 0 else request.type

        ticks = pd.DataFrame(mt5.copy_ticks_range(
            symbol, fromDate, toDate, copyTicks))

        trades = []
        for _, row in ticks.iterrows():
            time = protoTimestamp.Timestamp()
            time.FromMilliseconds(int(row['time_msc']))

            trades.append(protos.Trade(
                time=time,
                bid=protoWrappers.DoubleValue(value=row['bid']),
                ask=protoWrappers.DoubleValue(value=row['ask']),
                last=protoWrappers.DoubleValue(value=row['last']),
                volume=protoWrappers.DoubleValue(value=row['volume']),
                flags=int(row['flags']),
                realVolume=protoWrappers.DoubleValue(value=row['volume_real']),
            ))

        return protos.CopyTicksRangeReply(success=True, trades=trades)


def serve():
    server = grpc.server(futures.ThreadPoolExecutor(max_workers=10))
    services.add_MarketDataServicer_to_server(Terminal(), server)
    server.add_insecure_port("[::]:5051")
    server.start()
    server.wait_for_termination()


if __name__ == "__main__":
    logger.info("listening...")
    serve()
