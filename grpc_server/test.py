from datetime import datetime
import MetaTrader5 as mt5

from terminal.helpers import ChunkHelper


for i in ChunkHelper.chunks(None, 100):
    print(i)

mt5.initialize()

info = mt5.symbol_info_tick('EURUSD')

print(info)