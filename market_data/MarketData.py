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

DEFAULT_OFFSET_DAYS = 30
DEFAULT_OFFSET_MINUTES = 30
DEFAULT_TIMEFRAME_MT5 = mt5.TIMEFRAME_M10
DEFAULT_TIMEFRAME_KEY_PART = 'M10'


def getheaddatakey() -> str:
    return 'mkt:_head:data'


def getsymbolrateskey(symbol: str, timeframe: str) -> str:
    return f'mkt:{symbol.lower()}:rates:{timeframe.lower()}'


def getsymbolmetakey(symbol: str) -> str:
    return f'mkt:{symbol.lower()}:meta'


def getsymbols():
    for symbol in open('symbols.txt', mode='r').read().split('\n'):
        if not symbol.startswith('#'):
            yield symbol


def init_platform():
    if not mt5.initialize():
        logging.error("initialize() failed, error code = %s", mt5.last_error())
        return False

    return True


def get_redis() -> aioredis.Redis:
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


async def initdata(symbol: list[str], redis: aioredis.Redis):
    logger.info('starting symbol %s', symbol)

    offset = timedelta(days=DEFAULT_OFFSET_DAYS)

    fromdate = (datetime.now() - offset).replace(tzinfo=pytz.utc)
    todate = (datetime.now() + timedelta(days=1)).replace(tzinfo=pytz.utc)

    rates = mt5.copy_rates_range(
        symbol.upper(), DEFAULT_TIMEFRAME_MT5, fromdate, todate)

    metadata = {
        'init_at': datetime.now().timestamp(),
        'init_count': len(rates) if rates is not None else 0
    }

    await puttocache(symbol, DEFAULT_TIMEFRAME_KEY_PART, rates, metadata, redis)


async def pollingsymbols(symbols: list[str], redis: aioredis.Redis):
    offset = timedelta(minutes=DEFAULT_OFFSET_MINUTES)

    fromdate = (datetime.now() - offset).replace(tzinfo=pytz.utc)
    todate = (datetime.now() + timedelta(days=1)).replace(tzinfo=pytz.utc)

    await asyncio.gather(*(pollingsymbol(symbol, fromdate, todate, redis) for symbol in symbols))


async def pollingsymbol(symbol: str, fromdate: datetime, todate: datetime, redis: aioredis.Redis):
    if await redis.exists(getsymbolmetakey(symbol)) == 0:
        await initdata(symbol, redis)
        return

    rates = mt5.copy_rates_range(
        symbol.upper(), DEFAULT_TIMEFRAME_MT5, fromdate, todate)

    await puttocache(symbol, DEFAULT_TIMEFRAME_KEY_PART, rates, None, redis)


async def puttocache(symbol: list[str], timeframe: str, rates: any, metainfo: dict[any, any], redis: aioredis.Redis):
    rates_frame = pd.DataFrame(rates)
    exp = timedelta(minutes=5)

    headkey = getheaddatakey()
    symbolkey = getsymbolrateskey(symbol, timeframe)
    symbolmetakey = getsymbolmetakey(symbol)

    if rates_frame.empty:
        logging.warning('No data for symbol: %s', symbol)

        await redis.expire(headkey, exp)
        await redis.expire(symbolkey, exp)
        await redis.expire(symbolmetakey, exp)

        return

    mapping = {}
    for _, row in rates_frame.iterrows():
        score = float(row['time'])
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

    await redis.hmset(symbolkey, mapping)
    await redis.expire(symbolkey, exp)

    metadata.update({
        'current_count': await redis.hlen(symbolkey)
    })

    await redis.set(symbolmetakey, json.dumps(metadata), ex=exp)


async def main():
    redis = get_redis()

    await cleandata(getsymbols(), redis)

    while True:
        if not init_platform():
            continue

        logging.info('polling...')

        try:
            await pollingsymbols(getsymbols(), redis)
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
