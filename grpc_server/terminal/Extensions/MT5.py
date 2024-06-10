import logging

import MetaTrader5 as mt5
import pandas as pd
import Contracts_pb2 as contractsProtos
import google.protobuf.wrappers_pb2 as wrappersProtos

logger = logging.getLogger("app")


class MT5:

    @staticmethod
    def initialize():
        if not mt5.initialize():
            error = mt5.last_error()
            logger.error("initialize failed, error code = %s", error)
            raise f"initialize failed, error code = {error}"

    @staticmethod
    def create_ticks_dataframe(ticks):
        trades = pd.DataFrame(ticks)
        trades.index = pd.to_datetime(trades["time_msc"], unit="ms")
        trades.drop(columns=["time"], inplace=True)
        trades.drop(columns=["time_msc"], inplace=True)
        trades.dropna(inplace=True)
        return trades

    @staticmethod
    def create_ohlc_from_ticks(ticks, rule):
        if isinstance(ticks, pd.DataFrame):
            trades = ticks
        else:
            trades = MT5.create_ticks_dataframe(ticks)
        resample = trades.resample(rule=rule, label="left")
        rates = resample["last"].ohlc()
        rates["tick_volume"] = resample["last"].count()
        rates["real_volume"] = resample["volume"].sum()
        rates.dropna(inplace=True)
        return rates

    @staticmethod
    def response_status():
        error = mt5.last_error()

        if error[0] == mt5.RES_S_OK:
            logger.debug("response status, error code = %s", error)
        else:
            logger.error("response status, error code = %s", error)

        return contractsProtos.ResponseStatus(
            responseCode=int(error[0]),
            responseMessage=wrappersProtos.StringValue(value=error[1]),
        )
