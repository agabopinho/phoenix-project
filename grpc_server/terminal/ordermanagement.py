import logging

import google.protobuf.timestamp_pb2 as protoTimestamp
import google.protobuf.wrappers_pb2 as protoWrappers
import MetaTrader5 as mt5
import ordermanagement_pb2 as protos
import ordermanagement_pb2_grpc as services
import pytz

logger = logging.getLogger("app")


class OrderManagement(services.OrderManagementServicer):
    def __parseOrders(self, result):
        orders = []

        for order in result or []:
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
                type=int(order['type']),
                state=int(order['state']),
                timeExpiration=timeExpiration,
                timeDone=timeDone,
                typeFilling=int(order['type_filling']),
                typeTime=int(order['type_time']),
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

    def __orderRequest(self, request):
        orderRequest = {}

        if request.HasField('action'):
            orderRequest['action'] = int(request.action)

        if request.HasField('magic'):
            orderRequest['magic'] = int(request.magic)

        if request.HasField('order'):
            orderRequest['order'] = int(request.order)

        if request.HasField('symbol'):
            orderRequest['symbol'] = request.symbol

        if request.HasField('volume'):
            orderRequest['volume'] = float(request.volume.value)

        if request.HasField('price'):
            orderRequest['price'] = float(request.price.value)

        if request.HasField('stopLimit'):
            orderRequest['stoplimit'] = request.stopLimit

        if request.HasField('stopLoss'):
            orderRequest['sl'] = request.stopLoss

        if request.HasField('takeProfit'):
            orderRequest['tp'] = request.takeProfit

        if request.HasField('deviation'):
            orderRequest['deviation'] = request.deviation

        if request.HasField('type'):
            orderRequest['type'] = request.type

        if request.HasField('typeFilling'):
            orderRequest['type_filling'] = request.typeFilling

        if request.HasField('typeTime'):
            orderRequest['type_time'] = request.typeTime

        if request.HasField('expiration'):
            orderRequest['expiration'] = request.expiration

        if request.HasField('comment'):
            orderRequest['comment'] = request.comment

        if request.HasField('position'):
            orderRequest['position'] = request.position

        if request.HasField('positionBy'):
            orderRequest['position_by'] = request.positionBy

        return orderRequest

    def GetPositions(self, request, _):
        result = []

        if request.HasField('symbol'):
            result = mt5.positions_get(symbol=request.symbol)
        elif request.HasField('group'):
            result = mt5.positions_get(group=request.group)
        elif request.HasField('ticket'):
            result = mt5.positions_get(ticket=request.ticket.value)
        else:
            result = mt5.positions_get()

        positions = []
        for position in result or []:
            position = position._asdict()
            time = protoTimestamp.Timestamp()
            time.FromMilliseconds(int(position['time_msc']))

            timeUpdate = protoTimestamp.Timestamp()
            timeUpdate.FromMilliseconds(int(position['time_update_msc']))

            positions.append(protos.Position(
                ticket=protoWrappers.Int64Value(value=position['ticket']),
                time=time,
                timeUpdate=timeUpdate,
                type=int(position['type']),
                magic=protoWrappers.Int64Value(value=position['magic']),
                identifier=protoWrappers.Int64Value(
                    value=position['identifier']),
                reason=int(position['reason']),
                volume=protoWrappers.DoubleValue(value=position['volume']),
                priceOpen=protoWrappers.DoubleValue(
                    value=position['price_open']),
                stopLoss=protoWrappers.DoubleValue(value=position['sl']),
                takeProfit=protoWrappers.DoubleValue(value=position['tp']),
                priceCurrent=protoWrappers.DoubleValue(
                    value=position['price_current']),
                swap=protoWrappers.DoubleValue(value=position['swap']),
                profit=protoWrappers.DoubleValue(value=position['profit']),
                symbol=position['symbol'],
                comment=position['comment'],
                externalId=position['external_id'],
            ))

        return protos.GetPositionsReply(positions=positions)

    def GetOrders(self, request, _):
        result = []

        if request.HasField('symbol'):
            result = mt5.orders_get(symbol=request.symbol)
        elif request.HasField('group'):
            result = mt5.orders_get(group=request.group)
        elif request.HasField('ticket'):
            result = mt5.orders_get(ticket=request.ticket.value)
        else:
            result = mt5.orders_get()

        return protos.GetOrdersReply(orders=self.__parseOrders(result))

    def GetHistoryOrders(self, request, _):
        result = []

        if request.HasField('group'):
            result = mt5.history_orders_get(
                request.group.fromDate.ToDatetime(tzinfo=pytz.utc),
                request.group.toDate.ToDatetime(tzinfo=pytz.utc),
                group=request.group.groupValue)
        elif request.HasField('ticket'):
            result = mt5.history_orders_get(ticket=request.ticket.value)
        elif request.HasField('position'):
            result = mt5.history_orders_get(position=request.position.value)

        return protos.GetHistoryOrdersReply(orders=self.__parseOrders(result))

    def GetHistoryDeals(self, request, _):
        result = []

        if request.HasField('group'):
            result = mt5.history_deals_get(
                request.group.fromDate.ToDatetime(tzinfo=pytz.utc),
                request.group.toDate.ToDatetime(tzinfo=pytz.utc),
                group=request.group.groupValue)
        elif request.HasField('ticket'):
            result = mt5.history_deals_get(ticket=request.ticket.value)
        elif request.HasField('position'):
            result = mt5.history_deals_get(position=request.position.value)

        deals = []
        for deal in result or []:
            deal = deal._asdict()

            time = protoTimestamp.Timestamp()
            time.FromMilliseconds(int(deal['time_msc']))

            deals.append(protos.Deal(
                ticket=protoWrappers.Int64Value(value=deal['ticket']),
                order=protoWrappers.Int64Value(value=deal['order']),
                time=time,
                type=int(deal['type']),
                entry=int(deal['entry']),
                magic=protoWrappers.Int64Value(value=deal['magic']),
                reason=int(deal['reason']),
                positionId=protoWrappers.Int64Value(value=deal['position_id']),
                volume=protoWrappers.DoubleValue(value=deal['volume']),
                price=protoWrappers.DoubleValue(value=deal['price']),
                commission=protoWrappers.DoubleValue(value=deal['commission']),
                swap=protoWrappers.DoubleValue(value=deal['swap']),
                profit=protoWrappers.DoubleValue(value=deal['profit']),
                fee=protoWrappers.DoubleValue(value=deal['fee']),
                symbol=deal['symbol'],
                comment=deal['comment'],
                externalId=deal['external_id'],
            ))
        return protos.GetHistoryDealsReply(deals=deals)

    def CheckOrder(self, request, _):
        orderRequest = self.__orderRequest(request)

        result = mt5.order_send(orderRequest)

        return protos.CheckOrderReply(
            retcode=int(result.retcode),
            balance=protoWrappers.DoubleValue(value=result.balance),
            equity=protoWrappers.DoubleValue(value=result.equity),
            profit=protoWrappers.DoubleValue(value=result.profit),
            margin=protoWrappers.DoubleValue(value=result.margin),
            marginFree=protoWrappers.DoubleValue(value=result.margin_free),
            marginLevel=protoWrappers.DoubleValue(value=result.margin_level),
            comment=result.comment
        )

    def SendOrder(self, request, _):
        orderRequest = self.__orderRequest(request)

        result = mt5.order_send(orderRequest)

        return protos.GetOrdersReply(
            retcode=int(result.retcode),
            deal=protoWrappers.Int64Value(value=result.deal),
            order=protoWrappers.Int64Value(value=result.order),
            volume=protoWrappers.DoubleValue(value=result.volume),
            price=protoWrappers.DoubleValue(value=result.price),
            bid=protoWrappers.DoubleValue(value=result.bid),
            ask=protoWrappers.DoubleValue(value=result.ask),
            comment=result.comment,
            requestId=protoWrappers.Int64Value(value=result.request_id),
            retcodeExternal=protoWrappers.Int64Value(
                value=result.retcode_external),
        )
