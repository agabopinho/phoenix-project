import logging

import google.protobuf.timestamp_pb2 as timestampProtos
import google.protobuf.wrappers_pb2 as wrappersProtos
import MarketData_pb2 as protos
import Contracts_pb2 as contractsProtos
import MarketData_pb2_grpc as services
import MetaTrader5 as mt5
import pytz

from terminal.Extensions.MT5 import MT5

logger = logging.getLogger("app")

_MILLIS_PER_SECOND = 1000
_NANOS_PER_MILLIS = 1000000


class MarketData(services.MarketDataServicer):
    def __copyTicksRange(self, request):
        return mt5.copy_ticks_range(
            request.symbol.upper(),
            request.fromDate.ToDatetime(tzinfo=pytz.utc),
            request.toDate.ToDatetime(tzinfo=pytz.utc),
            mt5.COPY_TICKS_ALL if request.type == 0 else request.type,
        )

    def __copyRatesRange(self, request):
        return mt5.copy_rates_range(
            request.symbol.upper(),
            request.timeframe,
            request.fromDate.ToDatetime(tzinfo=pytz.utc),
            request.toDate.ToDatetime(tzinfo=pytz.utc),
        )

    def GetSymbolTick(self, request, _):
        MT5.initialize()

        result = mt5.symbol_info_tick(request.symbol)
        responseStatus = MT5.response_status()

        if responseStatus.responseCode != contractsProtos.RES_S_OK:
            return protos.GetSymbolTickReply(responseStatus=responseStatus)

        time = timestampProtos.Timestamp()
        time.FromMilliseconds(int(result.time_msc))

        return protos.GetSymbolTickReply(
            trade=protos.Trade(
                time=time,
                bid=wrappersProtos.DoubleValue(value=result.bid),
                ask=wrappersProtos.DoubleValue(value=result.ask),
                last=wrappersProtos.DoubleValue(value=result.last),
                volume=wrappersProtos.DoubleValue(value=result.volume),
                flags=int(result.flags),
                volumeReal=wrappersProtos.DoubleValue(value=result.volume_real),
            ),
            responseStatus=responseStatus,
        )

    def StreamTicksRange(self, request, _):
        MT5.initialize()

        data = self.__copyTicksRange(request)
        responseStatus = MT5.response_status()

        if responseStatus.responseCode != contractsProtos.RES_S_OK:
            yield protos.StreamTicksRangeReply(responseStatus=responseStatus)

        logger.debug("StreamTicksRange: %s", len(data))

        trades = [
            protos.Trade(
                time=timestampProtos.Timestamp(
                    seconds=int(trade["time_msc"] / _MILLIS_PER_SECOND),
                    nanos=int(
                        (trade["time_msc"] % _MILLIS_PER_SECOND) * _NANOS_PER_MILLIS
                    ),
                ),
                bid=wrappersProtos.DoubleValue(value=trade["bid"]),
                ask=wrappersProtos.DoubleValue(value=trade["ask"]),
                last=wrappersProtos.DoubleValue(value=trade["last"]),
                volume=wrappersProtos.DoubleValue(value=trade["volume"]),
                flags=int(trade["flags"]),
                volumeReal=wrappersProtos.DoubleValue(value=trade["volume_real"]),
            )
            for trade in data
        ]
        del data
        tradesCount = len(trades)

        for i in range(0, tradesCount, request.chunkSize):
            chunk = trades[i : i + request.chunkSize]
            logger.debug("reply %s trades", len(chunk))
            yield protos.StreamTicksRangeReply(
                trades=chunk, responseStatus=responseStatus
            )

    def StreamRatesRange(self, request, _):
        MT5.initialize()

        data = self.__copyRatesRange(request)
        responseStatus = MT5.response_status()

        if responseStatus.responseCode != contractsProtos.RES_S_OK:
            yield protos.StreamRatesRangeReply(responseStatus=responseStatus)

        for i in range(0, len(data), request.chunkSize):
            rates = []
            for rate in data[i : i + request.chunkSize]:
                time = timestampProtos.Timestamp()
                time.FromSeconds(int(rate["time"]))
                rates.append(
                    protos.Rate(
                        time=time,
                        open=wrappersProtos.DoubleValue(value=rate["open"]),
                        high=wrappersProtos.DoubleValue(value=rate["high"]),
                        low=wrappersProtos.DoubleValue(value=rate["low"]),
                        close=wrappersProtos.DoubleValue(value=rate["close"]),
                        tickVolume=wrappersProtos.DoubleValue(
                            value=rate["tick_volume"]
                        ),
                        spread=wrappersProtos.DoubleValue(value=rate["spread"]),
                        volume=wrappersProtos.DoubleValue(value=rate["real_volume"]),
                    )
                )
            yield protos.StreamRatesRangeReply(
                rates=rates, responseStatus=responseStatus
            )

    def StreamRatesFromTicksRange(self, request, _):
        MT5.initialize()

        data = self.__copyTicksRange(
            protos.StreamTicksRangeRequest(
                symbol=request.symbol,
                fromDate=request.fromDate,
                toDate=request.toDate,
                type=int(mt5.COPY_TICKS_TRADE),
            )
        )
        responseStatus = MT5.response_status()

        if responseStatus.responseCode != contractsProtos.RES_S_OK:
            yield protos.StreamRatesRangeReply(responseStatus=responseStatus)

        dataOHLC = MT5.create_ohlc_from_ticks(data, request.timeframe.ToTimedelta())

        for i in range(0, len(dataOHLC), request.chunkSize):
            rates = []
            for index, rate in dataOHLC[i : i + request.chunkSize].iterrows():
                time = timestampProtos.Timestamp()
                time.FromMilliseconds(int(index.timestamp() * _MILLIS_PER_SECOND))
                rates.append(
                    protos.Rate(
                        time=time,
                        open=wrappersProtos.DoubleValue(value=rate["open"]),
                        high=wrappersProtos.DoubleValue(value=rate["high"]),
                        low=wrappersProtos.DoubleValue(value=rate["low"]),
                        close=wrappersProtos.DoubleValue(value=rate["close"]),
                        tickVolume=wrappersProtos.DoubleValue(
                            value=rate["tick_volume"]
                        ),
                        spread=wrappersProtos.DoubleValue(value=0),
                        volume=wrappersProtos.DoubleValue(value=rate["real_volume"]),
                    )
                )
            yield protos.StreamRatesRangeReply(
                rates=rates, responseStatus=responseStatus
            )
