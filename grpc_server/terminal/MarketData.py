import logging

import google.protobuf.timestamp_pb2 as timestampProtos
import google.protobuf.wrappers_pb2 as wrappersProtos
import MarketData_pb2 as protos
import Contracts_pb2 as contractsProtos
import MarketData_pb2_grpc as services
import MetaTrader5 as mt5
import pytz

from terminal.Extensions.ChunkHelper import ChunkHelper
from terminal.Extensions.Mt5Helper import Mt5Helper

logger = logging.getLogger("app")

_MILLIS_PER_SECOND = 1000


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
        result = mt5.symbol_info_tick(request.symbol)

        responseStatus = Mt5Helper.ErrorToResponseStatus()
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
        data = self.__copyTicksRange(request)

        responseStatus = Mt5Helper.ErrorToResponseStatus()
        if responseStatus.responseCode != contractsProtos.RES_S_OK:
            yield protos.StreamTicksRangeReply(responseStatus=responseStatus)

        for chunk in ChunkHelper.Chunks(data, request.chunckSize):
            chunkData = []
            for trade in chunk:
                time = timestampProtos.Timestamp()
                time.FromMilliseconds(int(trade["time_msc"]))
                chunkData.append(
                    protos.Trade(
                        time=time,
                        bid=wrappersProtos.DoubleValue(value=trade["bid"]),
                        ask=wrappersProtos.DoubleValue(value=trade["ask"]),
                        last=wrappersProtos.DoubleValue(value=trade["last"]),
                        volume=wrappersProtos.DoubleValue(value=trade["volume"]),
                        flags=int(trade["flags"]),
                        volumeReal=wrappersProtos.DoubleValue(
                            value=trade["volume_real"]
                        ),
                    )
                )
            yield protos.StreamTicksRangeReply(
                trades=chunkData, responseStatus=responseStatus
            )

    def StreamRatesRange(self, request, _):
        data = self.__copyRatesRange(request)

        responseStatus = Mt5Helper.ErrorToResponseStatus()
        if responseStatus.responseCode != contractsProtos.RES_S_OK:
            yield protos.StreamRatesRangeReply(responseStatus=responseStatus)

        for chunk in ChunkHelper.Chunks(data, request.chunckSize):
            chunkData = []
            for rate in chunk:
                time = timestampProtos.Timestamp()
                time.FromSeconds(int(rate["time"]))
                chunkData.append(
                    protos.Rate(
                        time=time,
                        open=wrappersProtos.DoubleValue(value=rate["open"]),
                        high=wrappersProtos.DoubleValue(value=rate["high"]),
                        low=wrappersProtos.DoubleValue(value=rate["low"]),
                        close=wrappersProtos.DoubleValue(value=rate["close"]),
                        tickVolume=wrappersProtos.DoubleValue(value=rate["tick_volume"]),
                        spread=wrappersProtos.DoubleValue(value=rate["spread"]),
                        volume=wrappersProtos.DoubleValue(value=rate["real_volume"]),
                    )
                )
            yield protos.StreamRatesRangeReply(
                rates=chunkData, responseStatus=responseStatus
            )

    def StreamRatesFromTicksRange(self, request, _):
        data = self.__copyTicksRange(
            protos.StreamTicksRangeRequest(
                symbol=request.symbol,
                fromDate=request.fromDate,
                toDate=request.toDate,
                type=int(mt5.COPY_TICKS_TRADE),
            )
        )

        responseStatus = Mt5Helper.ErrorToResponseStatus()
        if responseStatus.responseCode != contractsProtos.RES_S_OK:
            yield protos.StreamRatesRangeReply(responseStatus=responseStatus)

        resample = Mt5Helper.ResultToDataFrame(data).resample(
            rule=request.timeframe.ToTimedelta(), label="left"
        )

        rates = resample["last"].ohlc()
        rates["tick_volume"] = resample["last"].count()
        rates["real_volume"] = resample["volume"].sum()

        for chunk in ChunkHelper.Chunks(rates, request.chunckSize):
            chunkData = []
            for index, rate in chunk.iterrows():
                time = timestampProtos.Timestamp()
                time.FromMilliseconds(int(index.timestamp() * _MILLIS_PER_SECOND))
                chunkData.append(
                    protos.Rate(
                        time=time,
                        open=wrappersProtos.DoubleValue(value=rate["open"]),
                        high=wrappersProtos.DoubleValue(value=rate["high"]),
                        low=wrappersProtos.DoubleValue(value=rate["low"]),
                        close=wrappersProtos.DoubleValue(value=rate["close"]),
                        tickVolume=wrappersProtos.DoubleValue(value=rate["tick_volume"]),
                        spread=wrappersProtos.DoubleValue(value=0),
                        volume=wrappersProtos.DoubleValue(value=rate["real_volume"]),
                    )
                )
            yield protos.StreamRatesRangeReply(
                rates=chunkData, responseStatus=responseStatus
            )
