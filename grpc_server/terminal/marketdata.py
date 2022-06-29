import logging

import google.protobuf.timestamp_pb2 as protoTimestamp
import google.protobuf.wrappers_pb2 as protoWrappers
import marketdata_pb2 as protos
import marketdata_pb2_grpc as marketDataService
import MetaTrader5 as mt5
import pytz

from terminal.helpers import ChunkHelper, TerminalHelper

logger = logging.getLogger("app")

_MILLIS_PER_SECOND = 1000


class MarketData(marketDataService.MarketDataServicer):
    def __copyTicksRange(self, request):
        return mt5.copy_ticks_range(
            request.symbol.upper(),
            request.fromDate.ToDatetime(tzinfo=pytz.utc),
            request.toDate.ToDatetime(tzinfo=pytz.utc),
            mt5.COPY_TICKS_ALL if request.type == 0 else request.type)

    def __copyRatesRange(self, request):
        symbol = request.symbol.upper()
        fromDate = request.fromDate.ToDatetime(tzinfo=pytz.utc)
        toDate = request.toDate.ToDatetime(tzinfo=pytz.utc)

        return mt5.copy_rates_range(symbol, request.timeframe, fromDate, toDate)

    def GetSymbolTick(self, request, _):
        result = mt5.symbol_info_tick(request.symbol)
        error = mt5.last_error()

        if result is None:
            return protos.GetSymbolTickReply(
                responseCode=int(error[0]),
                responseMessage=protoWrappers.StringValue(value=error[1]),
            )

        time = protoTimestamp.Timestamp()
        time.FromMilliseconds(int(result.time_msc))
        return protos.GetSymbolTickReply(
            trade=protos.Trade(
                time=time,
                bid=protoWrappers.DoubleValue(value=result.bid),
                ask=protoWrappers.DoubleValue(value=result.ask),
                last=protoWrappers.DoubleValue(value=result.last),
                volume=protoWrappers.DoubleValue(value=result.volume),
                flags=int(result.flags),
                volumeReal=protoWrappers.DoubleValue(value=result.volume_real),
            ),
            responseCode=int(error[0]),
            responseMessage=protoWrappers.StringValue(value=error[1]),
        )

    def StreamTicksRange(self, request, _):
        data = self.__copyTicksRange(request)
        error = mt5.last_error()

        yield protos.StreamTicksRangeReply(
            responseCode=int(error[0]),
            responseMessage=protoWrappers.StringValue(value=error[1]),
        )

        for chunk in ChunkHelper.chunks(data, request.chunckSize):
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
                    volumeReal=protoWrappers.DoubleValue(
                        value=trade['volume_real']),
                ))
            yield protos.StreamTicksRangeReply(trades=chunkData)

    def StreamRatesRange(self, request, _):
        data = self.__copyRatesRange(request)
        error = mt5.last_error()

        yield protos.StreamRatesRangeReply(
            responseCode=int(error[0]),
            responseMessage=protoWrappers.StringValue(value=error[1]),
        )

        for chunk in ChunkHelper.chunks(data, request.chunckSize):
            chunkData = []
            for rate in chunk:
                time = protoTimestamp.Timestamp()
                time.FromSeconds(int(rate['time']))
                chunkData.append(protos.Rate(
                    time=time,
                    open=protoWrappers.DoubleValue(value=rate['open']),
                    high=protoWrappers.DoubleValue(value=rate['high']),
                    low=protoWrappers.DoubleValue(value=rate['low']),
                    close=protoWrappers.DoubleValue(value=rate['close']),
                    tickVolume=protoWrappers.DoubleValue(
                        value=rate['tick_volume']),
                    spread=protoWrappers.DoubleValue(value=rate['spread']),
                    volume=protoWrappers.DoubleValue(
                        value=rate['real_volume']),
                ))
            yield protos.StreamRatesRangeReply(rates=chunkData)

    def StreamRatesFromTicksRange(self, request, _):
        data = self.__copyTicksRange(protos.StreamTicksRangeRequest(
            symbol=request.symbol,
            fromDate=request.fromDate,
            toDate=request.toDate,
            type=int(mt5.COPY_TICKS_TRADE)))
        error = mt5.last_error()

        yield protos.StreamRatesRangeReply(
            responseCode=int(error[0]),
            responseMessage=protoWrappers.StringValue(value=error[1]),
        )

        resample = TerminalHelper.resultToDateFrame(data).resample(
            rule=request.timeframe.ToTimedelta(), label='left')

        rates = resample['last'].ohlc()
        rates['tick_volume'] = resample['last'].count()
        rates['real_volume'] = resample['volume'].sum()

        for chunk in ChunkHelper.chunks(rates, request.chunckSize):
            chunkData = []
            for index, rate in chunk.iterrows():
                time = protoTimestamp.Timestamp()
                time.FromMilliseconds(
                    int(index.timestamp() * _MILLIS_PER_SECOND))
                chunkData.append(protos.Rate(
                    time=time,
                    open=protoWrappers.DoubleValue(value=rate['open']),
                    high=protoWrappers.DoubleValue(value=rate['high']),
                    low=protoWrappers.DoubleValue(value=rate['low']),
                    close=protoWrappers.DoubleValue(value=rate['close']),
                    tickVolume=protoWrappers.DoubleValue(
                        value=rate['tick_volume']),
                    spread=protoWrappers.DoubleValue(value=0),
                    volume=protoWrappers.DoubleValue(
                        value=rate['real_volume']),
                ))
            yield protos.StreamRatesRangeReply(rates=chunkData)
