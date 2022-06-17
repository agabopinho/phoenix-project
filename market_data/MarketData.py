import asyncio
import json
import logging
import platform
import sys
from datetime import datetime, timedelta

import aioredis
import MetaTrader5 as mt5
import pandas as pd
import pytz

logging.basicConfig(
    format="%(asctime)s %(levelname)s:%(name)s: %(message)s",
    level=logging.DEBUG,
    datefmt="%H:%M:%S",
    stream=sys.stderr,
)
logging.getLogger("chardet.charsetprober").disabled = True
logger = logging.getLogger("areq")


LOCALTZ = pytz.timezone('America/Sao_Paulo')

DEFAULT_OFFSET_INIT: timedelta = timedelta(days=5)
DEFAULT_OFFSET_POLLING: timedelta = timedelta(minutes=5)
DEFAULT_TIMEFRAME_MT5: int = mt5.TIMEFRAME_M1
DEFAULT_TIMEFRAME_KEY_PART: str = 'M1'

lastsymbols: list[str] = []
lastsymbolsreadaat: datetime = datetime.now()


def getheaddatakey() -> str:
    return 'mkt:_head:data'


def getsymbolrateskey(symbol: str, timeframe: str) -> str:
    return f'mkt:{symbol.lower()}:rates:{timeframe.lower()}'


def getsymbolmetakey(symbol: str) -> str:
    return f'mkt:{symbol.lower()}:meta'


def getsymbols() -> list[str]:
    global lastsymbols
    global lastsymbolsreadaat

    if lastsymbols and lastsymbolsreadaat and datetime.now() - lastsymbolsreadaat < timedelta(seconds=2):
        return lastsymbols

    lastsymbols = symbolsfromfile()
    lastsymbolsreadaat = datetime.now()

    return lastsymbols


def symbolsfromfile() -> list[str]:
    s: list[str] = []
    for symbol in open('symbols.txt', mode='r').read().split('\n'):
        if not symbol.startswith('#'):
            s.append(symbol)
    return s


def initplatform():
    if not mt5.initialize():
        logger.error("initialize() failed, error code = %s", mt5.last_error())
        return False

    return True


def getredis() -> aioredis.Redis:
    pool = aioredis.ConnectionPool(
        host='127.0.0.1', port=6379, db=0)
    return aioredis.Redis(connection_pool=pool)


async def cleandata(symbols: list[str], redis: aioredis.Redis):
    headkey = getheaddatakey()

    await redis.delete(headkey)

    for symbol in symbols:
        symbolkey = getsymbolrateskey(symbol, DEFAULT_TIMEFRAME_KEY_PART)
        symbolmetakey = getsymbolmetakey(symbol)

        await redis.delete(symbolkey)
        await redis.delete(symbolmetakey)


async def polling(symbols: list[str], redis: aioredis.Redis):
    await asyncio.gather(*(updatesymbol(symbol, redis) for symbol in symbols))


async def updatesymbol(symbol: str, redis: aioredis.Redis):
    datenow = datetime.now(LOCALTZ)

    if await redis.exists(getsymbolmetakey(symbol)) == 0:
        fromdate = (datenow - DEFAULT_OFFSET_INIT).replace(tzinfo=pytz.utc)
        todate = (datenow + timedelta(days=1)).replace(tzinfo=pytz.utc)

        await initdata(symbol, fromdate, todate, redis)

        return

    fromdate = (datenow - DEFAULT_OFFSET_POLLING).replace(tzinfo=pytz.utc)
    todate = (datenow + timedelta(days=1)).replace(tzinfo=pytz.utc)

    rates = mt5.copy_rates_range(
        symbol.upper(), DEFAULT_TIMEFRAME_MT5, fromdate, todate)

    await updatecache(symbol, DEFAULT_TIMEFRAME_KEY_PART, rates, None, redis)


async def initdata(symbol: list[str], fromdate: datetime, todate: datetime, redis: aioredis.Redis):
    logger.info('starting symbol %s', symbol)

    rates = mt5.copy_rates_range(
        symbol.upper(), DEFAULT_TIMEFRAME_MT5, fromdate, todate)

    metadata = {
        'init_at': datetime.now().timestamp(),
        'init_count': len(rates) if rates is not None else 0
    }

    await updatecache(symbol, DEFAULT_TIMEFRAME_KEY_PART, rates, metadata, redis)


async def updatecache(symbol: list[str], timeframe: str, rates: any, metainfo: dict[any, any], redis: aioredis.Redis):
    rates_frame = pd.DataFrame(rates)
    exp = timedelta(minutes=5)

    headkey = getheaddatakey()
    symbolkey = getsymbolrateskey(symbol, timeframe)
    symbolmetakey = getsymbolmetakey(symbol)

    if rates_frame.empty:
        logger.warning('No data for symbol: %s', symbol)

        await redis.expire(headkey, exp)
        await redis.expire(symbolkey, exp)
        await redis.expire(symbolmetakey, exp)

        return

    mapping = {}
    for _, row in rates_frame.iterrows():
        time = datetime.fromtimestamp(row['time'], pytz.utc)
        row['time'] = LOCALTZ.localize(time.replace(tzinfo=None)).timestamp()

        score = row['time']
        data = row.to_json(orient='values')

        mapping[score] = data

    metajson = await redis.get(symbolmetakey)
    metadata = json.loads(metajson) if metajson else {}

    metadata.update({
        'updated_at': datetime.now().timestamp(),
        'updated_count': len(mapping),
        'available_rates_timeframes': [timeframe]
    })

    if metainfo:
        metainfo.update(metadata)
        metadata = metainfo

    await redis.set(headkey, rates_frame.columns.to_series().to_json(orient='values'), ex=exp)

    await redis.hset(symbolkey, mapping=mapping)
    await redis.expire(symbolkey, exp)

    metadata.update({
        'current_count': await redis.hlen(symbolkey)
    })

    await redis.set(symbolmetakey, json.dumps(metadata), ex=exp)


async def main():
    redis = getredis()

    await cleandata(getsymbols(), redis)

    lastsecond = 0

    while True:
        if not initplatform():
            continue

        if lastsecond != datetime.now().second:
            logger.info('polling...')
            lastsecond = datetime.now().second

        try:
            await polling(getsymbols(), redis)
        except asyncio.CancelledError:
            logger.exception('CancelledError', exc_info=True, stack_info=True)
            quit()
        except KeyboardInterrupt:
            logger.exception('KeyboardInterrupt',
                             exc_info=True, stack_info=True)
            quit()
        except Exception:
            logger.exception('Exception', exc_info=True, stack_info=True)

if __name__ == "__main__":
    if platform.system() == 'Windows':
        asyncio.set_event_loop_policy(asyncio.WindowsSelectorEventLoopPolicy())

    asyncio.run(main())
