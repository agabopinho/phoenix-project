import logging
import sys
from datetime import date, datetime, timedelta

import MetaTrader5 as mt5
import pyodbc
import pytz

from settings import *

logging.basicConfig(
    format="%(asctime)s %(levelname)s:%(name)s: %(message)s",
    level=logging.DEBUG,
    datefmt="%H:%M:%S",
    stream=sys.stderr,
)
logging.getLogger("chardet.charsetprober").disabled = True
logger = logging.getLogger("app")


CONN_STR = 'DRIVER={ODBC Driver 17 for SQL Server};Server=localhost;Database=backtest_data;Trusted_Connection=Yes;'


def chunks(l, n):
    if l is None:
        return
    for i in range(0, len(l), n):
        yield l[i:i+n]


def ticks(date: date):
    fromDate = datetime.combine(date, datetime.min.time())
    return mt5.copy_ticks_range(
        SYMBOL.upper(),
        datetime.combine(date, datetime.min.time()),
        fromDate + timedelta(days=1),
        mt5.COPY_TICKS_ALL)


def delete(date: date):
    conn = pyodbc.connect(CONN_STR)
    cursor = conn.cursor()
    cursor.execute(
        'delete from trade where symbol=? and convert(date, [time]) = ?', SYMBOL, date)
    conn.commit()
    conn.close()


def buildInsert(params):
    conn = pyodbc.connect(CONN_STR)
    cursor = conn.cursor()
    cursor.fast_executemany = True
    cursor.executemany("""
            insert into trade (symbol, [time], bid, ask, [last], volume, flags, volumeReal) 
            values (?, ?, ?, ?, ?, ?, ?, ?)""", params)
    conn.commit()
    conn.close()


def main():
    mt5.initialize()

    date = FROM_DATE

    while date <= TO_DATE:
        logger.info(f'Loading {date}')
        data = ticks(date)
        delete(date)

        for c in chunks(data, 50000):
            params = []

            for trade in c:
                item = []
                item.append(SYMBOL)
                item.append(datetime.fromtimestamp(
                    trade['time_msc'] / 1000, pytz.utc))
                item.append(float(trade['bid']))
                item.append(float(trade['ask']))
                item.append(float(trade['last']))
                item.append(float(trade['volume']))
                item.append(float(trade['flags']))
                item.append(float(trade['volume_real']))
                params.append(item)

            buildInsert(params)

        date = date + timedelta(days=1)


if __name__ == "__main__":
    main()
