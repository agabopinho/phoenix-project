# colorama_demo.py
import os
from colorama import init, Back
import json
import sys

import pandas as pd

init(autoreset=True)

BUY = ['StrongBuy', 'Buy']
SELL = ['StrongSell', 'Sell']

filename = sys.argv[1]
result = []

if filename.endswith('.json'):
    result = json.load(open(filename, 'r'))
if filename.endswith('.log'):
    content = open(filename, 'r').readlines()
    for line in content:
        result.append(json.loads(line[line.index('{'):]))

if len(result) == 0:
    print('No data')
    quit()

previous = None
loss = 0
lossCount = 0
gain = 0
gainCount = 0
opCount = 0
df = pd.DataFrame(columns=['timeIn', 'timeOut', 'side',
                           'priceIn', 'priceOut', 'opProfit', 'sumProfit'])

for current in result:
    if previous is None:
        print(current)
        previous = current
        continue

    if 'Signal' not in current:
        continue

    if current['Signal'] not in SELL and current['Signal'] not in BUY:
        continue

    if previous['Signal'] in SELL and current['Signal'] in SELL or previous['Signal'] in BUY and current['Signal'] in BUY:
        continue

    opCount += 1

    if previous['Signal'] in SELL:
        profit = previous['Price'] - current['Price']
    elif previous['Signal'] in BUY:
        profit = current['Price'] - previous['Price']

    if profit > 0:
        gain += profit
        gainCount += 1
    else:
        loss += profit
        lossCount += 1

    color = Back.GREEN if profit > 0 else Back.RED
    side = 'Buy' if previous['Signal'] in BUY else 'Sell'

    df = pd.concat([df, pd.DataFrame({
        'timeIn': previous['Date'],
        'timeOut': current['Date'],
        'side': side,
        'priceIn': previous['Price'],
        'priceOut': current['Price'],
        'opProfit': profit,
        'sumProfit': gain + loss
    }, index=[1])], ignore_index=True)

    print('Time In: {}, Time Out: {}'.format(
        previous['Date'], current['Date']))
    print('Side: {}, In: {}, Out: {}'.format(
        side, previous['Price'], current['Price']))
    print('Op. Profit {}{}'.format(color, profit))
    print()
    print('{}>>>{} Total Profit: {}{}'.format(Back.YELLOW, Back.BLACK,
          Back.GREEN if gain + loss > 0 else Back.RED, gain + loss))
    print()

    previous = current

print(Back.YELLOW + 'Result >>>')
print()
print('Loss: {}'.format(loss))
print('LossCount: {}'.format(lossCount))
print('Gain: {}'.format(gain))
print('GainCount: {}'.format(gainCount))
print('OpCount: {}'.format(opCount))
print('Total Profit: {}{}'.format(
    Back.GREEN if gain + loss > 0 else Back.RED, gain + loss))

df.to_excel('result.xlsx', 'Result')

os.system('start EXCEL.EXE result.xlsx')