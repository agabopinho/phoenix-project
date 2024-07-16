from dataclasses import dataclass, field
from datetime import datetime
from enum import Enum
from typing import List, Optional


class SideType(Enum):
    BUY = 1
    SELL = 2


@dataclass
class Position:
    date: datetime
    type: SideType
    open_price: float
    close_price: Optional[float] = None
    current_price: float = 0.0
    profit: float = 0.0

    @property
    def is_open(self) -> bool:
        return self.close_price is None

    def update(self, price: float, close_position: bool):
        self.current_price = price

        if close_position:
            self.close_price = price

        if self.type == SideType.BUY:
            self.profit = price - self.open_price

        if self.type == SideType.SELL:
            self.profit = self.open_price - price


@dataclass
class Brick:
    date: datetime
    close: float


@dataclass
class Backtest:
    slippage: float
    bricks: List[Brick] = field(default_factory=list)
    positions: List[Position] = field(default_factory=list)

    @property
    def current_position(self) -> Optional[Position]:
        if not self.positions:
            return None
        last = self.positions[-1]
        if not last.is_open:
            return None
        return last

    def add_brick(self, brick: Brick):
        self.bricks.append(brick)
        self.statistics()

    def add_position(self, type: SideType):
        if self.current_position:
            raise ValueError("Position open.")

        brick = self.bricks[-1]
        open_price = (
            brick.close + self.slippage
            if type == SideType.BUY
            else brick.close - self.slippage
        )

        self.positions.append(
            Position(date=brick.date, type=type, open_price=open_price)
        )
        self.statistics()

    def close_position(self):
        if not self.current_position:
            raise ValueError("No open position to close.")

        self.statistics(close_position=True)

    def statistics(self, close_position: bool = False):
        last_position = self.current_position

        if not last_position:
            return

        brick = self.bricks[-1]
        close_type = (
            SideType.SELL if last_position.type == SideType.BUY else SideType.BUY
        )
        close_price = (
            brick.close + self.slippage
            if close_type == SideType.BUY
            else brick.close - self.slippage
        )

        last_position.update(close_price, close_position)
