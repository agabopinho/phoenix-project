import io
import logging

import google.protobuf.timestamp_pb2 as timestampProtos
import google.protobuf.wrappers_pb2 as wrappersProtos
import numpy as np
import MarketData_pb2 as protos
import Contracts_pb2 as contractsProtos
import MarketData_pb2_grpc as services
import MetaTrader5 as mt5
import pytz
import joblib
import pandas as pd

from terminal.Extensions.MT5Ext import MT5Ext

logger = logging.getLogger("app")

_MILLIS_PER_SECOND = 1000
_NANOS_PER_MILLIS = 1000000


class MarketData(services.MarketDataServicer):
    def __init__(self):
        self.models = {
            "bought": joblib.load("./models/bought.joblib"),
            "sold": joblib.load("./models/sold.joblib"),
        }

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

    def __ratesRangeFromTicks(self, request): 
        data = self.__copyTicksRange(
            protos.StreamTicksRangeRequest(
                symbol=request.symbol,
                fromDate=request.fromDate,
                toDate=request.toDate,
                type=int(mt5.COPY_TICKS_TRADE),
            )
        )
        responseStatus = MT5Ext.check_conn()

        if responseStatus.responseCode != contractsProtos.RES_S_OK:
            return (responseStatus, None)

        ohlc = MT5Ext.create_ohlc_from_ticks(data, request.timeframe.ToTimedelta())

        del data

        rates = [
            protos.Rate(
                time=timestampProtos.Timestamp(seconds=int(index.timestamp())),
                open=wrappersProtos.DoubleValue(value=rate["open"]),
                high=wrappersProtos.DoubleValue(value=rate["high"]),
                low=wrappersProtos.DoubleValue(value=rate["low"]),
                close=wrappersProtos.DoubleValue(value=rate["close"]),
                tickVolume=wrappersProtos.DoubleValue(value=rate["tick_volume"]),
                spread=wrappersProtos.DoubleValue(value=0),
                volume=wrappersProtos.DoubleValue(value=rate["real_volume"]),
            )
            for index, rate in ohlc.iterrows()
        ]

        del ohlc
        
        return (responseStatus, rates)
    
    def GetSymbolTick(self, request, _):
        tick = mt5.symbol_info_tick(request.symbol)
        responseStatus = MT5Ext.check_conn()

        if responseStatus.responseCode != contractsProtos.RES_S_OK:
            return protos.GetSymbolTickReply(responseStatus=responseStatus)

        return protos.GetSymbolTickReply(
            tick=protos.Tick(
                time=timestampProtos.Timestamp(
                    seconds=int(tick.time_msc / _MILLIS_PER_SECOND),
                    nanos=int((tick.time_msc % _MILLIS_PER_SECOND) * _NANOS_PER_MILLIS),
                ),
                bid=wrappersProtos.DoubleValue(value=tick.bid),
                ask=wrappersProtos.DoubleValue(value=tick.ask),
                last=wrappersProtos.DoubleValue(value=tick.last),
                volume=wrappersProtos.DoubleValue(value=tick.volume),
                flags=int(tick.flags),
                volumeReal=wrappersProtos.DoubleValue(value=tick.volume_real),
            ),
            responseStatus=responseStatus,
        )

    def StreamTicksRange(self, request, _):
        data = self.__copyTicksRange(request)
        responseStatus = MT5Ext.check_conn()

        if responseStatus.responseCode != contractsProtos.RES_S_OK:
            return protos.TicksRangeReply(responseStatus=responseStatus)

        logger.debug("StreamTicksRange: %s", len(data))

        ticks = [
            protos.Tick(
                time=timestampProtos.Timestamp(
                    seconds=int(tick["time_msc"] / _MILLIS_PER_SECOND),
                    nanos=int(
                        (tick["time_msc"] % _MILLIS_PER_SECOND) * _NANOS_PER_MILLIS
                    ),
                ),
                bid=wrappersProtos.DoubleValue(value=tick["bid"]),
                ask=wrappersProtos.DoubleValue(value=tick["ask"]),
                last=wrappersProtos.DoubleValue(value=tick["last"]),
                volume=wrappersProtos.DoubleValue(value=tick["volume"]),
                flags=int(tick["flags"]),
                volumeReal=wrappersProtos.DoubleValue(value=tick["volume_real"]),
            )
            for tick in data
        ]

        del data

        for i in range(0, len(ticks), request.chunkSize):
            chunk = ticks[i : i + request.chunkSize]
            logger.debug("reply %s trades", len(chunk))
            yield protos.TicksRangeReply(
                ticks=chunk,
                responseStatus=responseStatus,
            )

    def StreamTicksRangeBytes(self, request, _):
        data = self.__copyTicksRange(request)
        responseStatus = MT5Ext.check_conn()

        if responseStatus.responseCode != contractsProtos.RES_S_OK:
            return protos.TicksRangeBytesReply(responseStatus=responseStatus)

        logger.debug("StreamTicksRangeBytes: %s", len(data))

        with io.BytesIO() as bytesIO:
            np.savez_compressed(
                bytesIO,
                time_msc=data["time_msc"] if "time_msc" in request.returnFields else [],
                bid=data["bid"] if "bid" in request.returnFields else [],
                ask=data["ask"] if "ask" in request.returnFields else [],
                last=data["last"] if "last" in request.returnFields else [],
                volume=data["volume"] if "volume" in request.returnFields else [],
                volume_real=(
                    data["volume_real"] if "volume_real" in request.returnFields else []
                ),
                flags=data["flags"] if "flags" in request.returnFields else [],
            )
            bytesIO.flush()
            bytesIO.seek(0)
            chunk = bytesIO.read(request.chunkSize)
            while len(chunk) > 0:
                logger.debug("reply %s bytes", len(chunk))
                yield protos.TicksRangeBytesReply(
                    bytes=chunk,
                    responseStatus=responseStatus,
                )
                chunk = bytesIO.read(request.chunkSize)

    def GetTicksRangeBytes(self, request, _):
        data = self.__copyTicksRange(request)
        responseStatus = MT5Ext.check_conn()

        if responseStatus.responseCode != contractsProtos.RES_S_OK:
            return protos.TicksRangeBytesReply(responseStatus=responseStatus)

        logger.debug("GetTicksRangeBytes: %s", len(data))

        with io.BytesIO() as bytesIO:
            np.savez_compressed(
                bytesIO,
                time_msc=data["time_msc"] if "time_msc" in request.returnFields else [],
                bid=data["bid"] if "bid" in request.returnFields else [],
                ask=data["ask"] if "ask" in request.returnFields else [],
                last=data["last"] if "last" in request.returnFields else [],
                volume=data["volume"] if "volume" in request.returnFields else [],
                volume_real=(
                    data["volume_real"] if "volume_real" in request.returnFields else []
                ),
                flags=data["flags"] if "flags" in request.returnFields else [],
            )
            nbytes = bytesIO.tell()
            bytesIO.flush()
            bytesIO.seek(0)
            logger.debug("reply %s bytes", nbytes)
            return protos.TicksRangeBytesReply(
                bytes=bytesIO.read(),
                responseStatus=responseStatus,
            )

    def StreamRatesRange(self, request, _):
        data = self.__copyRatesRange(request)
        responseStatus = MT5Ext.check_conn()

        if responseStatus.responseCode != contractsProtos.RES_S_OK:
            return protos.RatesRangeReply(responseStatus=responseStatus)

        rates = [
            protos.Rate(
                time=timestampProtos.Timestamp(seconds=int(rate["time"])),
                open=wrappersProtos.DoubleValue(value=rate["open"]),
                high=wrappersProtos.DoubleValue(value=rate["high"]),
                low=wrappersProtos.DoubleValue(value=rate["low"]),
                close=wrappersProtos.DoubleValue(value=rate["close"]),
                tickVolume=wrappersProtos.DoubleValue(value=rate["tick_volume"]),
                spread=wrappersProtos.DoubleValue(value=rate["spread"]),
                volume=wrappersProtos.DoubleValue(value=rate["real_volume"]),
            )
            for rate in data
        ]

        del data

        for i in range(0, len(data), request.chunkSize):
            yield protos.RatesRangeReply(
                rates=rates[i : i + request.chunkSize],
                responseStatus=responseStatus,
            )

    def StreamRatesRangeFromTicks(self, request, _):
        responseStatus, rates = self.__ratesRangeFromTicks(request)

        if responseStatus.responseCode != contractsProtos.RES_S_OK:
            return protos.RatesRangeReply(responseStatus, None)
        
        for i in range(0, len(rates), request.chunkSize):
            yield protos.RatesRangeReply(
                rates=rates[i : i + request.chunkSize],
                responseStatus=responseStatus,
            )
            
    def GetRatesRangeFromTicks(self, request, _):
        responseStatus, rates = self.__ratesRangeFromTicks(request)
        
        return protos.RatesRangeReply(
            rates=rates,
            responseStatus=responseStatus,
        )

    def Predict(self, request, _):
        model = self.models[request.modelName]
        yPred = model.predict(pd.DataFrame([{"CURRENT_CHANGE": request.currentChange.value}]))
        return protos.PredictReply(
            prediction=wrappersProtos.DoubleValue(value=yPred[0])
        )
