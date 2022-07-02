from datetime import datetime, tzinfo
import MetaTrader5 as mt5
import pandas as pd
import pytz

mt5.initialize()

utc_from = datetime(2022, 6, 30, 14, 30, 1, 500, tzinfo=pytz.utc)
utc_to = datetime(2022, 6, 30, 14, 30, 1, 500, tzinfo=pytz.utc)

ticks = mt5.copy_ticks_range("WINQ22", utc_from, utc_to, mt5.COPY_TICKS_TRADE)
dates = []
for t in ticks:
    dates.append(datetime.fromtimestamp(t['time_msc'] / 1000, tz=pytz.utc))

df = pd.DataFrame(dates)

print(df.min())
print(df.max())