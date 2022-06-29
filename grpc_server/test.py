
from datetime import datetime
import MetaTrader5 as mt5


mt5.initialize()
#print(mt5.terminal_info()._asdict())

deals = mt5.history_deals_get(datetime(2021, 1, 1), datetime(2023, 1, 1), group='*')

for d in deals: 
    print ('\nDeal: ', d)


mt5.RES_E_AUTH_FAILED