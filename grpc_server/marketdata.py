
import logging
from datetime import datetime

import google.protobuf.timestamp_pb2 as protoTimestamp
import google.protobuf.wrappers_pb2 as protoWrappers
import MetaTrader5 as mt5
import pytz

import marketdata_pb2 as protos
import marketdata_pb2_grpc as services

logger = logging.getLogger("app")


class TerminalHelper:
    @staticmethod
    def init():
        if not mt5.initialize():
            logger.error("initialize() failed, error code = %s",
                         mt5.last_error())
            return False

        return True


class DateHelper:
    @staticmethod
    def fromtimestamp(date):
        return datetime.fromtimestamp(date.seconds, tz=pytz.utc)


class Terminal(services.MarketDataServicer):
    def __internalCopyTicksRange(self, request):
        if not TerminalHelper.init():
            return None

        symbol = request.symbol.upper()

        fromDate = DateHelper.fromtimestamp(request.fromDate)
        toDate = DateHelper.fromtimestamp(request.toDate)

        copyTicks = mt5.COPY_TICKS_ALL if request.type == 0 else request.type

        return mt5.copy_ticks_range(symbol, fromDate, toDate, copyTicks)

    def __chunks(self, l, n):
        for i in range(0, len(l), n):
            yield l[i:i+n]

    def CopyTicksRangeStream(self, request, _):
        data = self.__internalCopyTicksRange(request)

        if data is None:
            return

        for chunk in self.__chunks(data, request.chunckSize):
            chunkData = []
            for trade in chunk:
                time = protoTimestamp.Timestamp()
                time.FromMilliseconds(int(trade['time_msc']))
                chunkData.append(protos.Trade(
                    time=time,
                    bid=protoWrappers.DoubleValue(value=trade['bid']),
                    ask=protoWrappers.DoubleValue(value=trade['ask']),
                    last=protoWrappers.DoubleValue(value=trade['last']),
                    volume=protoWrappers.DoubleValue(value=trade['volume']),
                    flags=int(trade['flags']),
                    realVolume=protoWrappers.DoubleValue(
                        value=trade['volume_real']),
                ))
            yield protos.CopyTicksRangeReply(trades=chunkData)
