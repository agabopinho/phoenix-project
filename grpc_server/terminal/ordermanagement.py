import logging

import google.protobuf.timestamp_pb2 as protoTimestamp
import google.protobuf.wrappers_pb2 as protoWrappers
import MetaTrader5 as mt5
import ordermanagement_pb2 as protos
import ordermanagement_pb2_grpc as services
import pytz

from terminal.helpers import TerminalHelper

logger = logging.getLogger("app")


class OrderManagement(services.OrderManagementServicer):
    def __parseOrders(self, result):
        orders = []
        for order in result:
            order = order._asdict()
            timeSetup = protoTimestamp.Timestamp()
            timeSetup.FromMilliseconds(int(order['time_setup_msc']))

            timeExpiration = protoTimestamp.Timestamp()
            timeExpiration.FromSeconds(int(order['time_expiration']))

            timeDone = protoTimestamp.Timestamp()
            timeDone.FromMilliseconds(int(order['time_done_msc']))

            orders.append(protos.Order(
                ticket=protoWrappers.Int64Value(value=order['ticket']),
                timeSetup=timeSetup,
                orderType=int(order['type']),
                orderState=int(order['state']),
                timeExpiration=timeExpiration,
                timeDone=timeDone,
                typeFilling=int(order['type_filling']),
                orderTime=int(order['type_time']),
                magic=protoWrappers.Int64Value(value=order['magic']),
                reason=int(order['reason']),
                positionId=protoWrappers.Int64Value(
                    value=order['position_id']),
                positionById=protoWrappers.Int64Value(
                    value=order['position_by_id']),
                volumeInitial=protoWrappers.DoubleValue(
                    value=order['volume_initial']),
                volumeCurrent=protoWrappers.DoubleValue(
                    value=order['volume_current']),
                priceOpen=protoWrappers.DoubleValue(value=order['price_open']),
                stopLoss=protoWrappers.DoubleValue(value=order['sl']),
                takeProfit=protoWrappers.DoubleValue(value=order['tp']),
                priceCurrent=protoWrappers.DoubleValue(
                    value=order['price_current']),
                priceStopLimit=protoWrappers.DoubleValue(
                    value=order['price_stoplimit']),
                symbol=order['symbol'],
                comment=order['comment'],
                externalId=order['external_id'],
            ))
        return orders

    def GetPositions(self, request, _):
        result = None

        if request.symbol:
            result = mt5.positions_get(symbol=request.symbol)
        elif request.group:
            result = mt5.positions_get(group=request.group)
        elif request.ticket:
            result = mt5.positions_get(ticket=request.ticket.value)

        positions = []
        for p in result:
            p = p._asdict()
            time = protoTimestamp.Timestamp()
            time.FromMilliseconds(int(p['time_msc']))

            timeUpdate = protoTimestamp.Timestamp()
            timeUpdate.FromMilliseconds(int(p['time_update_msc']))

            positions.append(protos.Position(
                ticket=protoWrappers.Int64Value(value=p['ticket']),
                time=time,
                type=int(p['type']),
                magic=protoWrappers.Int64Value(value=p['magic']),
                identifier=protoWrappers.Int64Value(value=p['identifier']),
                reason=int(p['reason']),
                volume=protoWrappers.DoubleValue(value=p['volume']),
                priceOpen=protoWrappers.DoubleValue(value=p['price_open']),
                stopLoss=protoWrappers.DoubleValue(value=p['sl']),
                takeProfit=protoWrappers.DoubleValue(value=p['tp']),
                priceCurrent=protoWrappers.DoubleValue(
                    value=p['price_current']),
                swap=protoWrappers.DoubleValue(value=p['swap']),
                profit=protoWrappers.DoubleValue(value=p['profit']),
                symbol=p['symbol'],
                comment=p['comment'],
                externalId=p['external_id'],
                timeUpdate=timeUpdate,
            ))
        return protos.GetPositionsReply(positions=positions)

    def GetOrders(self, request, _):
        result = None

        if request.symbol:
            result = mt5.orders_get(symbol=request.symbol)
        elif request.group:
            result = mt5.orders_get(group=request.group)
        elif request.ticket:
            result = mt5.orders_get(ticket=request.ticket.value)

        return protos.GetOrdersReply(orders=self.__parseOrders(result))

    def GetHistoryOrders(self, request, _):
        result = None

        if request.group:
            result = mt5.history_orders_get(
                request.group.fromDate.ToDatetime(tzinfo=pytz.utc),
                request.group.toDate.ToDatetime(tzinfo=pytz.utc),
                group=request.group.value)
        elif request.ticket:
            result = mt5.history_orders_get(ticket=request.ticket)
        elif request.position:
            result = mt5.history_orders_get(position=request.position)

        return protos.GetOrdersReply(orders=self.__parseOrders(result))
