# colorama_demo.py
from colorama import init, Back
import json
import sys

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

    print(color + 'Side: {}, In: {}, Out: {}'.format('Buy' if previous['Signal'] in BUY else 'Sell', previous['Price'], current['Price']))
    print(color + 'Op. Profit {}'.format(profit))
    print()
    print('>>> Total Profit: {}{}'.format(Back.GREEN if gain + loss > 0 else Back.RED, gain + loss))
    print()

    previous = current
    
print(Back.YELLOW + 'Result >>>')
print('Loss: {}'.format(loss))
print('LossCount: {}'.format(lossCount))
print('Gain: {}'.format(gain))
print('GainCount: {}'.format(gainCount))
print('OpCount: {}'.format(opCount))
print('Total Profit: {}{}'.format(Back.GREEN if gain + loss > 0 else Back.RED, gain + loss))