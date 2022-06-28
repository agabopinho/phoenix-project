import logging

import MetaTrader5 as mt5
import pandas as pd

logger = logging.getLogger("app")


class TerminalHelper:

    @staticmethod
    def init():
        if not mt5.initialize():
            logger.error("initialize() failed, error code = %s",
                         mt5.last_error())
            return False

        return True

    @staticmethod
    def resultToDateFrame(result, others: list[str] = None):
        dataframe = pd.DataFrame(result)
        dataframe.index = pd.to_datetime(
            dataframe['time_msc'], unit='ms', utc=True)
        if others is not None:
            for c in others:
                dataframe[c] = pd.to_datetime(
                    dataframe[c], unit='ms', utc=True)
        dataframe.drop(columns=['time'], inplace=True)
        return dataframe


class ChunkHelper:
    @staticmethod
    def chunks(l, n):
        for i in range(0, len(l), n):
            yield l[i:i+n]
