namespace Akuna
{
    using System;
    using System.Collections.Generic;

    public static class SortedListExtensions
    {
        private static int BinarySearch<TKey>(this IList<TKey> source, TKey target, Func<int, bool> zeroCmp, Func<int, int> loOp)
        {
            var comparer = Comparer<TKey>.Default;
            var l = 0;
            var h = source.Count - 1;

            while (l < h)
            {
                var mid = l + ((h - l) / 2);

                if (comparer.Compare(source[mid], target) < 0)
                {
                    l = mid + 1;
                }
                else
                {
                    h = mid - 1;
                }
            }

            if (zeroCmp(comparer.Compare(source[l], target)))
            {
                l = loOp(l);
            }

            return l;
        }

        public static int GreaterThanOrEqual<TKey, TValue>(this SortedList<TKey, TValue> list, TKey key) where TKey : notnull
        {
            return BinarySearch(list.Keys, key, a => a < 0, a => a + 1);
        }

        public static int LessThanOrEqual<TKey, TValue>(this SortedList<TKey, TValue> list, TKey key) where TKey : notnull
        {
            return BinarySearch(list.Keys, key, a => a > 0, a => a - 1);
        }
    }

    public enum BuySellType
    {
        Buy,
        Sell
    }

    public enum OpType
    {
        Buy,
        Sell,
        Cancel,
        Modify,
        Print,
    }

    public enum OrderType
    {
        Gfd,
        Ioc
    }

    public class OrderOperation
    {
        public static OrderOperation NullOperation { get; } = new OrderOperation { IsValid = false };

        public OpType OpType { get; set; }
        public uint Price { get; set; }
        public uint NewPrice { get; set; }
        public OrderType OrderType { get; set; }
        public uint Qty { get; set; }
        public uint NewQty { get; set; }
        public string OrderId { get; set; } = String.Empty;
        public BuySellType BuySell { get; set; }
        public bool IsValid { get; set; } = true;

        public Order ToOrder()
        {
            return new Order
            {
                BuySell = OpType == OpType.Buy ? BuySellType.Buy : BuySellType.Sell,
                Price = Price,
                Qty = Qty,
                OrderId = OrderId,
                IsIoc = OrderType == OrderType.Ioc,
            };
        }

        public override string ToString()
        {
            var result = string.Empty;

            switch (OpType)
            {
                case OpType.Buy:
                case OpType.Sell:
                    result = $"OpType:{OpType}, IsValid:{IsValid}, Price:{Price}, OrderType:{OrderType}, Qty:{Qty}, OrderId:{OrderId}";
                    break;
                case OpType.Cancel:
                    result = $"OpType:{OpType}, IsValid:{IsValid}, OrderId:{OrderId}";
                    break;
                case OpType.Modify:
                    result = $"OpType:{OpType}, IsValid:{IsValid}, BuySell:{BuySell}, NewPrice:{NewPrice}, NewQty:{NewQty}, OrderId:{OrderId}";
                    break;
                case OpType.Print:
                    result = $"OpType:{OpType}, IsValid:{IsValid}";
                    break;

            }

            return result;
        }
    }

    public class Order
    {
        public ulong SequenceId { get; set; }
        public BuySellType BuySell { get; set; }
        public uint Price { get; set; }
        public uint Qty { get; set; }
        public string OrderId { get; set; } = String.Empty;
        public bool IsIoc { get; set; }
        public bool IsValid { get; set; } = true;

        public override string ToString()
        {
            return $"Buy/Sell{BuySell}, Price:{Price}, Qty:{Qty}, OrderId:{OrderId}";
        }
    }

    public class OrderParser
    {
        private OrderOperation CreateBuySell(string[] parts)
        {
            var isValidOrder = true;

            var price = uint.Parse(parts[2]);
            if (price <= 0)
            {
                isValidOrder = false;
            }

            var qty = uint.Parse(parts[3]);
            if (qty <= 0)
            {
                isValidOrder = false;
            }

            return new OrderOperation
            {
                OpType = parts[0] == "BUY" ? OpType.Buy : OpType.Sell,
                OrderType = parts[1] == "IOC" ? OrderType.Ioc : OrderType.Gfd,
                Price = price,
                Qty = qty,
                OrderId = parts[4],
                IsValid = isValidOrder,
            };
        }

        private OrderOperation CreateCancel(string[] parts)
        {
            return new OrderOperation
            {
                OpType = OpType.Cancel,
                OrderId = parts[1],
                IsValid = true,
            };
        }

        private OrderOperation CreateModify(string[] parts)
        {
            var isValidOrder = true;

            var newPrice = uint.Parse(parts[3]);
            if (newPrice <= 0)
            {
                isValidOrder = false;
            }

            var newQty = uint.Parse(parts[4]);
            if (newQty <= 0)
            {
                isValidOrder = false;
            }
            return new OrderOperation
            {
                OpType = OpType.Modify,
                OrderId = parts[1],
                BuySell = parts[2] == "BUY" ? BuySellType.Buy : BuySellType.Sell,
                NewPrice = newPrice,
                NewQty = newQty,
                IsValid = isValidOrder
            };
        }

        private OrderOperation CreatePrint(string[] parts)
        {
            return new OrderOperation { OpType = OpType.Print, IsValid = true };
        }

        public OrderOperation Parse(string order)
        {
            var parts = order.Split(" ");

            OrderOperation output = OrderOperation.NullOperation;

            switch (parts[0])
            {
                case "BUY":
                case "SELL":
                    output = CreateBuySell(parts);
                    break;
                case "CANCEL":
                    output = CreateCancel(parts);
                    break;
                case "MODIFY":
                    output = CreateModify(parts);
                    break;
                case "PRINT":
                    output = CreatePrint(parts);
                    break;
            }

            return output;
        }
    }

    public class OutputWriter
    {
        public void Write(string value)
        {
            Console.WriteLine(value);
        }
    }

    public class OrderReader
    {
        private readonly OrderParser orderParser;

        public OrderReader(OrderParser orderParser)
        {
            this.orderParser = orderParser;
        }

        // This version used for the actual test (not for debugging)
        //public static IEnumerable<OrderOperation> NextOrderOperation()
        //{
        //    while (true)
        //    {
        //        var orderRow = Console.ReadLine();

        //        if (orderRow == null)
        //        {
        //            break;
        //        }

        //        var order = OrderParser.Parse(orderRow);

        //        yield return order;
        //    }

        //    yield break;
        //}

        private static IEnumerable<string> GetRows()
        {
            static IEnumerable<string> Test20()
            {
                return new[] {
                        "BUY GFD 7300 3 order0",
                        "PRINT",
                        "MODIFY order0 BUY 1 6",
                        "BUY IOC 4600 49 order1",
                        "BUY GFD 5100 48 order2",
                        "MODIFY order0 BUY 1 49",
                        "CANCEL order0",
                        "CANCEL order2",
                        "CANCEL order2",
                        "SELL IOC 3800 39 order3",
                        "SELL IOC 4000 93 order4",
                        "PRINT",
                        "MODIFY order3 BUY 1 11",
                        "CANCEL order1",
                        "SELL GFD 9300 90 order5",
                        "SELL GFD 400 7 order6",
                        "BUY GFD 1900 12 order7",
                        "BUY IOC 1300 59 order8",
                        "CANCEL order2",
                        "SELL GFD 8200 62 order9",
                        "BUY GFD 7000 87 order10",
                        "PRINT",
                        "CANCEL order3",
                        "MODIFY order1 BUY 1 55",
                        "BUY IOC 2100 61 order11",
                        "BUY GFD 4500 63 order12",
                        "PRINT",
                        "BUY GFD 9300 30 order13",
                        "SELL GFD 8600 93 order14",
                        "CANCEL order14",
                        "CANCEL order12",
                        "SELL IOC 4100 14 order15",
                        "SELL IOC 4500 49 order16",
                        "PRINT",
                        "BUY GFD 9500 33 order17",
                        "BUY IOC 6100 12 order18",
                        "CANCEL order17",
                        "BUY IOC 4100 72 order19",
                        "MODIFY order19 BUY 1 34",
                        "MODIFY order16 SELL 1 93",
                        "SELL IOC 9700 17 order20",
                        "MODIFY order1 BUY 1 98",
                        "CANCEL order16",
                        "SELL GFD 3000 27 order21",
                        "BUY GFD 9100 92 order22",
                        "PRINT",
                        "MODIFY order1 BUY 1 55",
                        "BUY GFD 6000 66 order23",
                        "BUY GFD 3500 53 order24",
                        "SELL IOC 1700 53 order25",
                        "MODIFY order21 BUY 1 15",
                        };
            }

            foreach (var i in Test20())
            {
                yield return i;
            }
        }

        public IEnumerable<OrderOperation> NextOrderOperation()
        {
            foreach (var orderRow in GetRows())
            {
                if (orderRow == null)
                {
                    yield break;
                }

                var order = orderParser.Parse(orderRow);

                yield return order;
            }
        }
    }

    public class OrderExecutor
    {
        private readonly OutputWriter outputWriter;

        public OrderExecutor(OutputWriter outputWriter)
        {
            this.outputWriter = outputWriter;
        }

        private class Disposable : IDisposable
        {
            private readonly Action dispose;

            public Disposable(Action dispose)
            {
                this.dispose = dispose;
            }

            public void Dispose()
            {
                dispose();
            }
        }

        public IDisposable Listen(OrderBook orderBook)
        {
            orderBook.OrderBookChanged += OnOrderBookChanged;
            return new Disposable(() => orderBook.OrderBookChanged -= OnOrderBookChanged);
        }

        private static (Order primary, Order secondary) GetPrimarySecondary(Order a, Order b)
        {
            var first = a.SequenceId < b.SequenceId ? a : b;
            var secondary = a.SequenceId > b.SequenceId ? a : b;

            return (first, secondary);
        }

        /// <summary>
        /// Will emit best execution trades orders from the best start price <paramref name="priceLevelIndexStart"/>
        /// </summary>
        private void MatchTradesAtPrice(Order order,
                                        OrderBook orderBook,
                                        SortedList<uint, SortedList<ulong, Order>> sourceBook,
                                        SortedList<uint, SortedList<ulong, Order>> toBook,
                                        int priceLevelIndexStart,
                                        Func<int, bool> priceLevelIndexPredicate,
                                        Func<int, int> priceLevelIndexOp)
        {
            var priceLevelDeleteSet = new List<uint>();

            for (var priceLevelIndex = priceLevelIndexStart; priceLevelIndexPredicate(priceLevelIndex); priceLevelIndex = priceLevelIndexOp(priceLevelIndex))
            {
                var price = toBook.Keys[priceLevelIndex];
                var orderList = toBook.Values[priceLevelIndex];

                var deleteOrderSequenceIdSet = new List<ulong>();

                foreach (var ord in orderList.Values)
                {
                    var (primary, secondary) = GetPrimarySecondary(order, ord);

                    if (order.Qty >= ord.Qty)
                    {
                        order.Qty -= ord.Qty;
                        orderBook.DeleteOrder(ord.OrderId);
                        deleteOrderSequenceIdSet.Add(ord.SequenceId);

                        outputWriter.Write($"TRADE {primary.OrderId} {primary.Price} {ord.Qty} {secondary.OrderId} {secondary.Price} {ord.Qty}");

                        if (order.Qty == 0)
                        {
                            break;
                        }
                    }
                    else
                    {
                        ord.Qty -= order.Qty;

                        outputWriter.Write($"TRADE {primary.OrderId} {primary.Price} {order.Qty} {secondary.OrderId} {secondary.Price} {order.Qty}");
                        order.Qty = 0;
                        break;
                    }
                }

                foreach (var deletedOrder in deleteOrderSequenceIdSet)
                {
                    orderList.Remove(deletedOrder);
                }

                if (orderList.Count == 0)
                {
                    priceLevelDeleteSet.Add(price);
                }

                if (order.Qty == 0)
                {
                    if (!order.IsIoc)
                    {
                        orderBook.DeleteOrder(order.OrderId);
                        var orderListSourcePrice = sourceBook[order.Price];
                        orderListSourcePrice.Remove(order.SequenceId);
                        if (orderListSourcePrice.Count == 0)
                        {
                            sourceBook.Remove(order.Price);
                        }
                    }

                    break;
                }
            }

            foreach (var priceLevel in priceLevelDeleteSet)
            {
                toBook.Remove(priceLevel);
            }
        }

        private void TradeSellWithBuyers(Order order,
                                                OrderBook orderBook,
                                                SortedList<uint, SortedList<ulong, Order>> sourceBook,
                                                SortedList<uint, SortedList<ulong, Order>> toBook)
        {
            if (toBook.Count == 0)
            {
                return;
            }

            var index = toBook.GreaterThanOrEqual(order.Price);

            if (index == -1)
            {
                return;
            }

            MatchTradesAtPrice(order,
                               orderBook,
                               sourceBook,
                               toBook,
                               toBook.Count - 1,
                               priceLevelIndex => priceLevelIndex >= index,
                               priceLevelIndex => priceLevelIndex - 1);
        }

        private void TradeBuyWithSellers(Order order,
                                            OrderBook orderBook,
                                            SortedList<uint, SortedList<ulong, Order>> sourceBook,
                                            SortedList<uint, SortedList<ulong, Order>> toBook)
        {
            if (toBook.Count == 0)
            {
                return;
            }

            var index = toBook.LessThanOrEqual(order.Price);

            if (index == -1)
            {
                return;
            }

            MatchTradesAtPrice(order,
                               orderBook,
                               sourceBook,
                               toBook,
                               0,
                               priceLevelIndex => priceLevelIndex <= index,
                               priceLevelIndex => priceLevelIndex + 1);
        }

        private void OnOrderBookChanged(object? sender, OrderBookChangeArgs args)
        {
            if (args.Order.BuySell == BuySellType.Sell)
            {
                TradeSellWithBuyers(args.Order, args.OrderBook, args.OrderBook.Sells, args.OrderBook.Buys);
            }
            else
            {
                TradeBuyWithSellers(args.Order, args.OrderBook, args.OrderBook.Buys, args.OrderBook.Sells);
            }
        }
    }

    public class OrderBookChangeArgs
    {
        public OrderBookChangeArgs(OrderBook orderBook, Order order)
        {
            OrderBook = orderBook;
            Order = order;
        }

        public OrderBook OrderBook { get; set; }
        public Order Order { get; set; }
    }

    public class OrderBook
    {
        private readonly Dictionary<string, Order> orderMap = new Dictionary<string, Order>();
        private readonly OrderSequenceGenerator orderSequenceGenerator;
        private readonly OutputWriter outputWriter;

        public event EventHandler<OrderBookChangeArgs>? OrderBookChanged;

        public SortedList<uint, SortedList<ulong, Order>> Buys { get; set; } = new SortedList<uint, SortedList<ulong, Order>>();
        public SortedList<uint, SortedList<ulong, Order>> Sells { get; set; } = new SortedList<uint, SortedList<ulong, Order>>();

        public OrderBook(OrderSequenceGenerator orderSequenceGenerator, OutputWriter outputWriter)
        {
            this.orderSequenceGenerator = orderSequenceGenerator;
            this.outputWriter = outputWriter;
        }

        public void ProcessOrderOperation(OrderOperation orderOperation)
        {
            switch (orderOperation.OpType)
            {
                case OpType.Buy:
                case OpType.Sell:
                    Create(orderOperation);
                    break;
                case OpType.Print:
                    Print();
                    break;
                case OpType.Modify:
                    Modify(orderOperation);
                    break;
                case OpType.Cancel:
                    Cancel(orderOperation);
                    break;
            }
        }

        private SortedList<ulong, Order> GetOrCreatePriceOrderList(uint price, BuySellType buySellType)
        {
            var targetBook = buySellType == BuySellType.Buy ? Buys : Sells;

            if (!targetBook.TryGetValue(price, out SortedList<ulong, Order>? orderList))
            {
                orderList = new SortedList<ulong, Order>();
                targetBook.Add(price, orderList);
            }

            return orderList;
        }

        private void Cancel(OrderOperation orderOperation)
        {
            if (!orderMap.TryGetValue(orderOperation.OrderId, out var cancelledOrder))
            {
                return;
            }

            orderMap.Remove(cancelledOrder.OrderId);
            var book = cancelledOrder.BuySell == BuySellType.Buy ? Buys : Sells;

            var orderList = book[cancelledOrder.Price];
            orderList.Remove(cancelledOrder.SequenceId);
            if (orderList.Count == 0)
            {
                book.Remove(cancelledOrder.Price);
            }
        }

        private void Modify(OrderOperation orderOperation)
        {
            if (orderOperation.OrderId == null)
            {
                return;
            }

            if (!orderMap.TryGetValue(orderOperation.OrderId, out var order))
            {
                return;
            }

            var sourceBook = order.BuySell == BuySellType.Buy ? Buys : Sells;
            var orderListSource = GetOrCreatePriceOrderList(order.Price, order.BuySell);
            orderListSource.Remove(order.SequenceId);
            if (orderListSource.Count == 0)
            {
                sourceBook.Remove(order.Price);
            }

            order.BuySell = orderOperation.BuySell;
            order.Price = orderOperation.NewPrice;
            order.Qty = orderOperation.NewQty;
            order.SequenceId = orderSequenceGenerator.NextId();

            var orderListTarget = GetOrCreatePriceOrderList(order.Price, order.BuySell);
            orderListTarget.Add(order.SequenceId, order);

            OrderBookChanged?.Invoke(this, new OrderBookChangeArgs(this, order));
        }

        private void Print()
        {
            void DumpBookPart(SortedList<uint, SortedList<ulong, Order>> book)
            {
                for (var i = book.Count - 1; i >= 0; i--)
                {
                    var orderList = book.Values[i];
                    var key = book.Keys[i];
                    var qtyTotal = orderList.Values.Sum(o => o.Qty);
                    if (qtyTotal > 0)
                    {
                        outputWriter.Write($"{key} {qtyTotal}");
                    }
                }
            }

            outputWriter.Write("SELL:");
            DumpBookPart(Sells);

            outputWriter.Write("BUY:");
            DumpBookPart(Buys);
        }

        private void Create(OrderOperation orderOperation)
        {
            var order = orderOperation.ToOrder();
            if (orderMap.ContainsKey(order.OrderId))
            {
                return;
            }

            order.SequenceId = orderSequenceGenerator.NextId();

            if (!order.IsIoc)
            {
                orderMap.Add(order.OrderId, order);

                var orderList = GetOrCreatePriceOrderList(order.Price, order.BuySell);
                orderList.Add(order.SequenceId, order);
            }

            OrderBookChanged?.Invoke(this, new OrderBookChangeArgs(this, order));
        }

        public void DeleteOrder(string? orderId)
        {
            if (orderId == null)
            {
                return;
            }

            orderMap.Remove(orderId);
        }
    }

    public class OrderSequenceGenerator
    {
        private ulong sequenceId;

        public ulong NextId()
        {
            return sequenceId++;
        }
    }

    public class OrderEngine
    {
        private readonly OrderReader orderReader;
        private readonly OrderBook orderBook;
        private readonly OrderExecutor orderExecutor;

        public OrderEngine(OrderReader orderReader, OrderBook orderBook, OrderExecutor orderExecutor)
        {
            this.orderReader = orderReader;
            this.orderBook = orderBook;
            this.orderExecutor = orderExecutor;
        }

        public void Start()
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            using (orderExecutor.Listen(orderBook))
            {
                foreach (var orderOperation in orderReader.NextOrderOperation())
                {
                    if (!orderOperation.IsValid)
                    {
                        continue;
                    }

                    orderBook.ProcessOrderOperation(orderOperation);
                }
            }

            sw.Stop();
            Console.WriteLine(sw.Elapsed);
        }
    }

    class Solution
    {
        public static void Execute()
        {
            var orderReader = new OrderReader(new OrderParser());
            var outputWriter = new OutputWriter();
            var orderBook = new OrderBook(new OrderSequenceGenerator(), outputWriter);
            var orderExecutor = new OrderExecutor(outputWriter);

            var engine = new OrderEngine(orderReader, orderBook, orderExecutor);
            engine.Start();
        }
    }
}