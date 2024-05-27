import logging

import MetaTrader5 as mt5
import pandas as pd
import Contracts_pb2 as contractsProtos
import google.protobuf.wrappers_pb2 as wrappersProtos

logger = logging.getLogger("app")


class Mt5Helper:

    @staticmethod
    def Init():
        if not mt5.initialize():
            logger.error("initialize() failed, error code = %s", mt5.last_error())
            return False

        return True

    @staticmethod
    def ResultToDataFrame(result):
        dataframe = pd.DataFrame(result)
        dataframe.index = pd.to_datetime(dataframe["time_msc"], unit="ms", utc=True)
        dataframe.drop(columns=["time"], inplace=True)
        return dataframe

    @staticmethod
    def ErrorToResponseStatus():
        error = mt5.last_error()

        logger.info("Error: %s", error)

        return contractsProtos.ResponseStatus(
            responseCode=int(error[0]),
            responseMessage=wrappersProtos.StringValue(value=error[1]),
        )
