import math


class Range:

    def __init__(self, brick_size, times, prices, volume):
        self.brick_size = brick_size
        self.bricks = []
        [
            self.check_new_price(times[i], float(d), volume[i])
            for i, d in enumerate(prices)
        ]

    def check_new_price(self, time, price, volume, brick_size=None):
        if brick_size is not None:
            self.brick_size = brick_size

        if len(self.bricks) == 0:
            item = {
                "time": time,
                "type": "last",
                "open": price,
                "high": price,
                "low": price,
                "close": price,
                "ticks_count": 1,
                "volume": volume,
            }
            self.bricks.append(item)
            return

        last = self.bricks[-1]

        delta = abs(price - last["open"])
        bricks_count = math.floor(delta / self.brick_size)

        if bricks_count == 0:
            last["close"] = price
            last["high"] = max(last["open"], last["high"], last["low"], last["close"])
            last["low"] = min(last["open"], last["high"], last["low"], last["close"])
            last["ticks_count"] += 1
            last["volume"] += volume

        if last["type"] in ["up", "last"]:
            if price > last["open"]:
                delta = price - last["open"]
                bricks_count = math.floor(delta / self.brick_size)
                if bricks_count > 0:
                    self.add_bricks("up", time, price, volume, bricks_count)
            elif price <= last["open"]:
                delta = last["open"] - price
                bricks_count = math.floor(delta / self.brick_size)
                if bricks_count > 0:
                    self.add_bricks("down", time, price, volume, bricks_count)
            return

        if last["type"] == "down":
            if price < last["open"]:
                delta = last["open"] - price
                bricks_count = math.floor(delta / self.brick_size)
                if bricks_count > 0:
                    self.add_bricks("down", time, price, volume, bricks_count)
            elif price >= last["open"]:
                delta = price - last["open"]
                bricks_count = math.floor(delta / self.brick_size)
                if bricks_count > 0:
                    self.add_bricks("up", time, price, volume, bricks_count)
            return

    def add_bricks(self, type, time, price, volume, count):
        last = self.bricks[-1]

        if last["type"] == "last":
            if type == "up":
                last["type"] = type
                last["close"] = last["open"] + self.brick_size
                last["high"] = last["open"] + self.brick_size
            elif type == "down":
                last["type"] = type
                last["close"] = last["open"] - self.brick_size
                last["low"] = last["open"] - self.brick_size

        for _ in range(1, count):
            last = self.bricks[-1]

            if type == "up":
                item = {
                    "time": time,
                    "type": type,
                    "open": last["close"],
                    "high": last["close"] + self.brick_size,
                    "low": last["close"],
                    "close": last["close"] + self.brick_size,
                    "ticks_count": 0,
                    "volume": 0,
                }
                self.bricks.append(item)
            elif type == "down":
                item = {
                    "time": time,
                    "type": type,
                    "open": last["close"],
                    "high": last["close"],
                    "low": last["close"] - self.brick_size,
                    "close": last["close"] - self.brick_size,
                    "ticks_count": 0,
                    "volume": 0,
                }
                self.bricks.append(item)

        last = self.bricks[-1]

        item = {
            "time": time,
            "type": "last",
            "open": last["close"],
            "high": max(price, last["close"]),
            "low": min(price, last["close"]),
            "close": price,
            "ticks_count": 1,
            "volume": volume,
        }

        self.bricks.append(item)
