import logging

import google.protobuf.timestamp_pb2 as protoTimestamp
import google.protobuf.wrappers_pb2 as protoWrappers
import MetaTrader5 as mt5
import pandas as pd
import pytz

import marketdata_pb2 as protos
import marketdata_pb2_grpc as services

logger = logging.getLogger("app")

_MILLIS_PER_SECOND = 1000


class TerminalHelper:
    @staticmethod
    def init():
        if not mt5.initialize():
            logger.error("initialize() failed, error code = %s",
                         mt5.last_error())
            return False

        return True


class MarketData(services.MarketDataServicer):
    def __internalCopyTicksRange(self, request):
        if not TerminalHelper.init():
            return None

        symbol = request.symbol.upper()
        fromDate = request.fromDate.ToDatetime(tzinfo=pytz.utc)
        toDate = request.toDate.ToDatetime(tzinfo=pytz.utc)
        copyTicks = mt5.COPY_TICKS_ALL if request.type == 0 else request.type

        return mt5.copy_ticks_range(symbol, fromDate, toDate, copyTicks)

    def __internalCopyRatesRange(self, request):
        if not TerminalHelper.init():
            return None

        symbol = request.symbol.upper()
        fromDate = request.fromDate.ToDatetime(tzinfo=pytz.utc)
        toDate = request.toDate.ToDatetime(tzinfo=pytz.utc)

        return mt5.copy_rates_range(symbol, request.timeframe, fromDate, toDate)

    def __chunks(self, l, n):
        for i in range(0, len(l), n):
            yield l[i:i+n]

    def __ticksToDateFrame(self, ticks):
        dataframe = pd.DataFrame(ticks)
        dataframe.index = pd.to_datetime(
            dataframe['time_msc'], unit='ms', utc=True)
        dataframe.drop(columns=['time'], inplace=True)
        return dataframe

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

    def CopyRatesRangeStream(self, request, _):
        data = self.__internalCopyRatesRange(request)

        if data is None:
            return

        for chunk in self.__chunks(data, request.chunckSize):
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
            yield protos.CopyRatesRangeReply(rates=chunkData)

    def CopyRatesFromTicksRangeStream(self, request, _):
        data = self.__internalCopyTicksRange(protos.CopyTicksRangeRequest(
            symbol=request.symbol,
            fromDate=request.fromDate,
            toDate=request.toDate,
            type=mt5.COPY_TICKS_TRADE))

        if data is None:
            return

        rule = request.timeframe.ToTimedelta()
        resample = self.__ticksToDateFrame(data).resample(rule, label='left')

        rates = resample['last'].ohlc()
        rates['tick_volume'] = resample['last'].count()
        rates['real_volume'] = resample['volume'].sum()

        for chunk in self.__chunks(rates, request.chunckSize):
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
            yield protos.CopyRatesRangeReply(rates=chunkData)
