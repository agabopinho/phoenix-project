sum_buy = 0
sum_buy_qty = 0
buy_avg = 0
sum_sell = 0
sum_sell_qty = 0
sell_avg = 0
net_avg = 0
net_qty = 0
profit = 0
used_slippage = 0
slippage = 5


def update(i):
    rates.loc[i, "sum_buy"] = sum_buy
    rates.loc[i, "sum_buy_qty"] = sum_buy_qty
    rates.loc[i, "buy_avg"] = buy_avg
    rates.loc[i, "sum_sell"] = sum_sell
    rates.loc[i, "sum_sell_qty"] = sum_sell_qty
    rates.loc[i, "sell_avg"] = sell_avg
    rates.loc[i, "net_avg"] = net_avg
    rates.loc[i, "net_qty"] = net_qty
    rates.loc[i, "profit"] = profit
    rates.loc[i, "slippage"] = used_slippage


for i, item in rates.iterrows():
    if np.isnan(item.open) or np.isnan(item.signal):
        update(i)
        continue

    used_slippage = slippage

    # sinal de compra
    if item.open == item.sma2:
        sum_buy += item.open + used_slippage
        sum_buy_qty += 1

    # sinal de venda
    if item.open == item.sma1:
        sum_sell += item.open - used_slippage
        sum_sell_qty -= 1

    buy_avg = 0 if sum_buy == 0 else sum_buy / sum_buy_qty
    sell_avg = 0 if sum_sell == 0 else sum_sell / sum_sell_qty

    buy = item.open + used_slippage if buy_avg == 0 else buy_avg
    sell = -item.open - used_slippage if sell_avg == 0 else sell_avg

    net_avg = sell + buy
    net_qty = sum_buy_qty + sum_sell_qty
    profit = net_avg * net_qty

    update(i)

rates[["profit"]].plot(figsize=(30, 10))
