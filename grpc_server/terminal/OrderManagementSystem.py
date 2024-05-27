import logging

import google.protobuf.timestamp_pb2 as timestampProtos
import google.protobuf.wrappers_pb2 as wrappersProtos
import MetaTrader5 as mt5
import OrderManagementSystem_pb2 as protos
import Contracts_pb2 as contractsProtos
import OrderManagementSystem_pb2_grpc as services
import pytz

from terminal.Extensions.Mt5Helper import Mt5Helper

logger = logging.getLogger("app")


class OrderManagementSystem(services.OrderManagementSystemServicer):
    def __parseOrders(self, result):
        orders = []

        for order in result or []:
            order = order._asdict()

            timeSetup = timestampProtos.Timestamp()
            timeSetup.FromMilliseconds(int(order["time_setup_msc"]))

            timeExpiration = timestampProtos.Timestamp()
            timeExpiration.FromSeconds(int(order["time_expiration"]))

            timeDone = timestampProtos.Timestamp()
            timeDone.FromMilliseconds(int(order["time_done_msc"]))

            orders.append(
                protos.Order(
                    ticket=wrappersProtos.Int64Value(value=order["ticket"]),
                    timeSetup=timeSetup,
                    type=int(order["type"]),
                    state=int(order["state"]),
                    timeExpiration=timeExpiration,
                    timeDone=timeDone,
                    typeFilling=int(order["type_filling"]),
                    typeTime=int(order["type_time"]),
                    magic=wrappersProtos.Int64Value(value=order["magic"]),
                    reason=int(order["reason"]),
                    positionId=wrappersProtos.Int64Value(value=order["position_id"]),
                    positionById=wrappersProtos.Int64Value(
                        value=order["position_by_id"]
                    ),
                    volumeInitial=wrappersProtos.DoubleValue(
                        value=order["volume_initial"]
                    ),
                    volumeCurrent=wrappersProtos.DoubleValue(
                        value=order["volume_current"]
                    ),
                    priceOpen=wrappersProtos.DoubleValue(value=order["price_open"]),
                    stopLoss=wrappersProtos.DoubleValue(value=order["sl"]),
                    takeProfit=wrappersProtos.DoubleValue(value=order["tp"]),
                    priceCurrent=wrappersProtos.DoubleValue(
                        value=order["price_current"]
                    ),
                    priceStopLimit=wrappersProtos.DoubleValue(
                        value=order["price_stoplimit"]
                    ),
                    symbol=wrappersProtos.StringValue(value=order["symbol"]),
                    comment=wrappersProtos.StringValue(value=order["comment"]),
                    externalId=wrappersProtos.StringValue(value=order["external_id"]),
                )
            )

        return orders

    def __orderRequest(self, request):
        orderRequest = {"action": int(request.action)}

        if request.HasField("magic"):
            orderRequest["magic"] = request.magic.value

        if request.HasField("order"):
            orderRequest["order"] = int(request.order)

        if request.HasField("symbol"):
            orderRequest["symbol"] = request.symbol.value

        if request.HasField("volume"):
            orderRequest["volume"] = request.volume.value

        if request.HasField("price"):
            orderRequest["price"] = request.price.value

        if request.HasField("stopLimit"):
            orderRequest["stoplimit"] = request.stopLimit.value

        if request.HasField("stopLoss"):
            orderRequest["sl"] = request.stopLoss.value

        if request.HasField("takeProfit"):
            orderRequest["tp"] = request.takeProfit.value

        if request.HasField("deviation"):
            orderRequest["deviation"] = request.deviation.value

        if request.HasField("type"):
            orderRequest["type"] = int(request.type)

        if request.HasField("typeFilling"):
            orderRequest["type_filling"] = int(request.typeFilling)

        if request.HasField("typeTime"):
            orderRequest["type_time"] = int(request.typeTime)

        if request.HasField("expiration"):
            orderRequest["expiration"] = request.expiration.ToDatetime(tzinfo=pytz.utc)

        if request.HasField("comment"):
            orderRequest["comment"] = request.comment.value

        if request.HasField("position"):
            orderRequest["position"] = request.position.value

        if request.HasField("positionBy"):
            orderRequest["position_by"] = request.positionBy.value

        return orderRequest

    def GetPositions(self, request, _):
        result = []

        if request.HasField("symbol"):
            result = mt5.positions_get(symbol=request.symbol.value)
        elif request.HasField("group"):
            result = mt5.positions_get(group=request.group.value)
        elif request.HasField("ticket"):
            result = mt5.positions_get(ticket=request.ticket.value)
        else:
            result = mt5.positions_get()

        responseStatus = Mt5Helper.ErrorToResponseStatus()
        if responseStatus.responseCode != contractsProtos.RES_S_OK:
            return protos.GetPositionsReply(responseStatus=responseStatus)

        positions = []

        for position in result or []:
            position = position._asdict()
            time = timestampProtos.Timestamp()
            time.FromMilliseconds(int(position["time_msc"]))

            timeUpdate = timestampProtos.Timestamp()
            timeUpdate.FromMilliseconds(int(position["time_update_msc"]))

            positions.append(
                protos.Position(
                    ticket=wrappersProtos.Int64Value(value=position["ticket"]),
                    time=time,
                    timeUpdate=timeUpdate,
                    type=int(position["type"]),
                    magic=wrappersProtos.Int64Value(value=position["magic"]),
                    identifier=wrappersProtos.Int64Value(value=position["identifier"]),
                    reason=int(position["reason"]),
                    volume=wrappersProtos.DoubleValue(value=position["volume"]),
                    priceOpen=wrappersProtos.DoubleValue(value=position["price_open"]),
                    stopLoss=wrappersProtos.DoubleValue(value=position["sl"]),
                    takeProfit=wrappersProtos.DoubleValue(value=position["tp"]),
                    priceCurrent=wrappersProtos.DoubleValue(
                        value=position["price_current"]
                    ),
                    swap=wrappersProtos.DoubleValue(value=position["swap"]),
                    profit=wrappersProtos.DoubleValue(value=position["profit"]),
                    symbol=wrappersProtos.StringValue(value=position["symbol"]),
                    comment=wrappersProtos.StringValue(value=position["comment"]),
                    externalId=wrappersProtos.StringValue(
                        value=position["external_id"]
                    ),
                )
            )

        return protos.GetPositionsReply(
            positions=positions, responseStatus=responseStatus
        )

    def GetOrders(self, request, _):
        result = []

        if request.HasField("symbol"):
            result = mt5.orders_get(symbol=request.symbol.value)
        elif request.HasField("group"):
            result = mt5.orders_get(group=request.group.value)
        elif request.HasField("ticket"):
            result = mt5.orders_get(ticket=request.ticket.value)
        else:
            result = mt5.orders_get()

        responseStatus = Mt5Helper.ErrorToResponseStatus()
        if responseStatus.responseCode != contractsProtos.RES_S_OK:
            return protos.GetOrdersReply(responseStatus=responseStatus)

        return protos.GetOrdersReply(
            orders=self.__parseOrders(result), responseStatus=responseStatus
        )

    def GetHistoryOrders(self, request, _):
        result = []

        if request.HasField("group"):
            result = mt5.history_orders_get(
                request.group.fromDate.ToDatetime(tzinfo=pytz.utc),
                request.group.toDate.ToDatetime(tzinfo=pytz.utc),
                group=request.group.groupValue,
            )
        elif request.HasField("ticket"):
            result = mt5.history_orders_get(ticket=request.ticket.value)
        elif request.HasField("position"):
            result = mt5.history_orders_get(position=request.position.value)

        responseStatus = Mt5Helper.ErrorToResponseStatus()
        if responseStatus.responseCode != contractsProtos.RES_S_OK:
            return protos.GetHistoryOrdersReply(responseStatus=responseStatus)

        return protos.GetHistoryOrdersReply(
            orders=self.__parseOrders(result), responseStatus=responseStatus
        )

    def GetHistoryDeals(self, request, _):
        result = []

        if request.HasField("group"):
            result = mt5.history_deals_get(
                request.group.fromDate.ToDatetime(tzinfo=pytz.utc),
                request.group.toDate.ToDatetime(tzinfo=pytz.utc),
                group=request.group.groupValue,
            )
        elif request.HasField("ticket"):
            result = mt5.history_deals_get(ticket=request.ticket.value)
        elif request.HasField("position"):
            result = mt5.history_deals_get(position=request.position.value)

        responseStatus = Mt5Helper.ErrorToResponseStatus()
        if responseStatus.responseCode != contractsProtos.RES_S_OK:
            return protos.GetHistoryDealsReply(responseStatus=responseStatus)

        deals = []

        for deal in result or []:
            deal = deal._asdict()

            time = timestampProtos.Timestamp()
            time.FromMilliseconds(int(deal["time_msc"]))

            deals.append(
                protos.Deal(
                    ticket=wrappersProtos.Int64Value(value=deal["ticket"]),
                    order=wrappersProtos.Int64Value(value=deal["order"]),
                    time=time,
                    type=int(deal["type"]),
                    entry=int(deal["entry"]),
                    magic=wrappersProtos.Int64Value(value=deal["magic"]),
                    reason=int(deal["reason"]),
                    positionId=wrappersProtos.Int64Value(value=deal["position_id"]),
                    volume=wrappersProtos.DoubleValue(value=deal["volume"]),
                    price=wrappersProtos.DoubleValue(value=deal["price"]),
                    commission=wrappersProtos.DoubleValue(value=deal["commission"]),
                    swap=wrappersProtos.DoubleValue(value=deal["swap"]),
                    profit=wrappersProtos.DoubleValue(value=deal["profit"]),
                    fee=wrappersProtos.DoubleValue(value=deal["fee"]),
                    symbol=wrappersProtos.StringValue(value=deal["symbol"]),
                    comment=wrappersProtos.StringValue(value=deal["comment"]),
                    externalId=wrappersProtos.StringValue(value=deal["external_id"]),
                )
            )

        return protos.GetHistoryDealsReply(deals=deals, responseStatus=responseStatus)

    def CheckOrder(self, request, _):
        orderRequest = self.__orderRequest(request)

        result = mt5.order_check(orderRequest)

        responseStatus = Mt5Helper.ErrorToResponseStatus()
        if responseStatus.responseCode != contractsProtos.RES_S_OK:
            return protos.CheckOrderReply(responseStatus=responseStatus)

        return protos.CheckOrderReply(
            retcode=int(result.retcode),
            balance=wrappersProtos.DoubleValue(value=result.balance),
            equity=wrappersProtos.DoubleValue(value=result.equity),
            profit=wrappersProtos.DoubleValue(value=result.profit),
            margin=wrappersProtos.DoubleValue(value=result.margin),
            marginFree=wrappersProtos.DoubleValue(value=result.margin_free),
            marginLevel=wrappersProtos.DoubleValue(value=result.margin_level),
            comment=wrappersProtos.StringValue(value=result.comment),
            responseStatus=responseStatus,
        )

    def SendOrder(self, request, _):
        orderRequest = self.__orderRequest(request)

        logger.info("SendOrder Request: %s", orderRequest)

        result = mt5.order_send(orderRequest)

        logger.info("SendOrder Result: %s", result)

        responseStatus = Mt5Helper.ErrorToResponseStatus()
        if responseStatus.responseCode != contractsProtos.RES_S_OK:
            return protos.SendOrderReply(responseStatus=responseStatus)

        return protos.SendOrderReply(
            retcode=int(result.retcode),
            deal=wrappersProtos.Int64Value(value=result.deal),
            order=wrappersProtos.Int64Value(value=result.order),
            volume=wrappersProtos.DoubleValue(value=result.volume),
            price=wrappersProtos.DoubleValue(value=result.price),
            bid=wrappersProtos.DoubleValue(value=result.bid),
            ask=wrappersProtos.DoubleValue(value=result.ask),
            comment=wrappersProtos.StringValue(value=result.comment),
            requestId=wrappersProtos.Int64Value(value=result.request_id),
            retcodeExternal=wrappersProtos.Int64Value(value=result.retcode_external),
            responseStatus=responseStatus,
        )
