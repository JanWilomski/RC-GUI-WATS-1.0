using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Threading;
using RiskCheckerGUI.Models;
using RiskCheckerGUI.Services;

namespace RiskCheckerGUI.ViewModels
{
    public class MessagesViewModel : ViewModelBase
    {
        private readonly TcpService _tcpService;
        private readonly UdpService _udpService;
        private readonly object _syncLock = new object();
        private readonly Dispatcher _dispatcher;

        // Kolekcje danych
        private ObservableCollection<LogMessage> _logs;
        private ObservableCollection<CcgMessage> _ccgMessages;
        private ObservableCollection<OrderBookEntry> _orderBook;
        private ObservableCollection<InstrumentPosition> _instrumentPositions;
        
        // Dane kapitału i limitów
        private CapitalUsage _capitalUsage;
        
        // Filtry
        private string _messageFilter = string.Empty;
        private string _instrumentFilter = string.Empty;
        private string _orderBookFilter = string.Empty;

        #region Properties

        public ObservableCollection<LogMessage> Logs
        {
            get => _logs;
            set => SetProperty(ref _logs, value);
        }

        public ObservableCollection<CcgMessage> CcgMessages
        {
            get => _ccgMessages;
            set => SetProperty(ref _ccgMessages, value);
        }

        public ObservableCollection<OrderBookEntry> OrderBook
        {
            get => _orderBook;
            set => SetProperty(ref _orderBook, value);
        }

        public ObservableCollection<InstrumentPosition> InstrumentPositions
        {
            get => _instrumentPositions;
            set => SetProperty(ref _instrumentPositions, value);
        }

        public CapitalUsage CapitalUsage
        {
            get => _capitalUsage;
            set => SetProperty(ref _capitalUsage, value);
        }

        public string MessageFilter
        {
            get => _messageFilter;
            set
            {
                if (SetProperty(ref _messageFilter, value))
                {
                    // Apply filter
                    CollectionViewSource.GetDefaultView(CcgMessages).Refresh();
                }
            }
        }

        public string InstrumentFilter
        {
            get => _instrumentFilter;
            set
            {
                if (SetProperty(ref _instrumentFilter, value))
                {
                    // Apply filter
                    CollectionViewSource.GetDefaultView(InstrumentPositions).Refresh();
                }
            }
        }

        public string OrderBookFilter
        {
            get => _orderBookFilter;
            set
            {
                if (SetProperty(ref _orderBookFilter, value))
                {
                    // Apply filter
                    CollectionViewSource.GetDefaultView(OrderBook).Refresh();
                }
            }
        }

        #endregion

                // W konstruktorze
        public MessagesViewModel(TcpService tcpService, UdpService udpService)
        {
            _tcpService = tcpService ?? throw new ArgumentNullException(nameof(tcpService));
            _udpService = udpService ?? throw new ArgumentNullException(nameof(udpService));
            _dispatcher = Dispatcher.CurrentDispatcher;

            // Inicjalizacja kolekcji
            _logs = new ObservableCollection<LogMessage>();
            _ccgMessages = new ObservableCollection<CcgMessage>();
            _orderBook = new ObservableCollection<OrderBookEntry>();
            _instrumentPositions = new ObservableCollection<InstrumentPosition>();
            _capitalUsage = new CapitalUsage(); // Inicjalizacja, by uniknąć null

            // Przykładowe wartości dla testów UI
            _capitalUsage.OpenCapital = 0;
            _capitalUsage.AccruedCapital = 0;
            _capitalUsage.TotalCapital = 0;
            _capitalUsage.MessageLimit = 115200;
            _capitalUsage.CapitalLimit = 100000;

            // Reszta kodu...
        }

        private void SetupCollectionFilters()
        {
            // Filtrowanie CCG Messages
            var ccgView = CollectionViewSource.GetDefaultView(CcgMessages);
            ccgView.Filter = obj =>
            {
                if (string.IsNullOrEmpty(MessageFilter)) return true;
                var message = obj as CcgMessage;
                return message != null && 
                    (message.ClOrdID?.Contains(MessageFilter, StringComparison.OrdinalIgnoreCase) == true ||
                        message.Symbol?.Contains(MessageFilter, StringComparison.OrdinalIgnoreCase) == true ||
                        message.Name?.Contains(MessageFilter, StringComparison.OrdinalIgnoreCase) == true ||
                        message.Header?.Contains(MessageFilter, StringComparison.OrdinalIgnoreCase) == true);
            };

            // Filtrowanie Instrument Positions
            var instrumentsView = CollectionViewSource.GetDefaultView(InstrumentPositions);
            instrumentsView.Filter = obj =>
            {
                if (string.IsNullOrEmpty(InstrumentFilter)) return true;
                var instrument = obj as InstrumentPosition;
                return instrument != null && 
                       (instrument.ISIN?.Contains(InstrumentFilter, StringComparison.OrdinalIgnoreCase) == true ||
                        instrument.TickerName?.Contains(InstrumentFilter, StringComparison.OrdinalIgnoreCase) == true);
            };

            // Filtrowanie Order Book
            var orderBookView = CollectionViewSource.GetDefaultView(OrderBook);
            orderBookView.Filter = obj =>
            {
                if (string.IsNullOrEmpty(OrderBookFilter)) return true;
                var order = obj as OrderBookEntry;
                return order != null && 
                       (order.OrderID?.Contains(OrderBookFilter, StringComparison.OrdinalIgnoreCase) == true ||
                        order.Ticker?.Contains(OrderBookFilter, StringComparison.OrdinalIgnoreCase) == true ||
                        order.Account?.Contains(OrderBookFilter, StringComparison.OrdinalIgnoreCase) == true);
            };
        }

        private void SubscribeToEvents()
        {
            // Log messages
            _tcpService.LogReceived += OnLogReceived;
            _udpService.LogReceived += OnLogReceived;

            // Position updates
            _tcpService.PositionReceived += OnPositionReceived;
            _udpService.PositionReceived += OnPositionReceived;

            // Capital updates
            _tcpService.CapitalReceived += OnCapitalReceived;
            _udpService.CapitalReceived += OnCapitalReceived;

            // CCG messages
            _tcpService.IOBytesReceived += OnCcgMessageReceived;
            _udpService.IOBytesReceived += OnCcgMessageReceived;
        }

        private void OnLogReceived(object sender, LogMessage e)
        {
            _dispatcher.InvokeAsync(() =>
            {
                Logs.Insert(0, e);
                
                // Limit collection size to prevent memory issues
                if (Logs.Count > 1000)
                    Logs.RemoveAt(Logs.Count - 1);
            });
        }

        private void OnPositionReceived(object sender, Position e)
        {
            _dispatcher.InvokeAsync(() =>
            {
                // Update instrument positions
                var existingPosition = InstrumentPositions.FirstOrDefault(p => p.ISIN == e.ISIN);
                if (existingPosition != null)
                {
                    existingPosition.Net = e.Net;
                    existingPosition.OpenLong = e.OpenLong;
                    existingPosition.OpenShort = e.OpenShort;
                }
                else
                {
                    InstrumentPositions.Add(new InstrumentPosition
                    {
                        ISIN = e.ISIN,
                        // Ticker name might be extracted from elsewhere
                        TickerName = e.ISIN.Split('.').LastOrDefault() ?? e.ISIN,
                        Net = e.Net,
                        OpenLong = e.OpenLong,
                        OpenShort = e.OpenShort
                    });
                }
            });
        }

        private void OnCapitalReceived(object sender, Capital e)
        {
            _dispatcher.InvokeAsync(() =>
            {
                CapitalUsage.OpenCapital = e.OpenCapital;
                CapitalUsage.AccruedCapital = e.AccruedCapital;
                CapitalUsage.TotalCapital = e.TotalCapital;
                
                // Update used capital (this is an approximation, adjust as needed)
                CapitalUsage.UsedCapital = e.TotalCapital;
            });
        }

        private void OnCcgMessageReceived(object sender, byte[] messageBytes)
        {
            _dispatcher.InvokeAsync(() =>
            {
                // Parse CCG message
                var ccgMessage = ParseCcgMessage(messageBytes);
                if (ccgMessage != null)
                {
                    // Add to messages list
                    ccgMessage.Nr = CcgMessages.Count + 1; // Upewnij się, że numer jest unikalny
                    CcgMessages.Insert(0, ccgMessage);
                    if (CcgMessages.Count > 1000)
                        CcgMessages.RemoveAt(CcgMessages.Count - 1);

                    // Update message count for limits
                    CapitalUsage.UsedMessages++;

                    // Update order book
                    UpdateOrderBook(ccgMessage);
                }
            });
        }

        private CcgMessage ParseCcgMessage(byte[] messageBytes)
        {
            try
            {
                // Konwertuj wiadomość do tekstu
                string messageString = System.Text.Encoding.ASCII.GetString(messageBytes);
                
                // Najprostsze podejście - podziel wiadomość po określonym separatorze
                // Zakładając format podobny do FIX (tag=value|tag=value)
                // W rzeczywistości format może być inny, dostosuj do formatu twoich wiadomości
                
                // Przykładowy kod dla formatu tagowanego z separatorami
                // Załóżmy, że wiadomość CCG ma format: 35=D|34=123|52=20250512-14:30:00|...
                var parts = messageString.Split('|');
                
                var message = new CcgMessage
                {
                    Nr = _ccgMessages.Count + 1,  // Przydziel numer kolejny
                    DateReceived = DateTime.Now,
                    RawData = messageBytes
                };
                
                foreach (var part in parts)
                {
                    var tagValue = part.Split('=');
                    if (tagValue.Length != 2) continue;
                    
                    var tag = tagValue[0];
                    var value = tagValue[1];
                    
                    switch (tag)
                    {
                        case "35": // MsgType
                            message.Header = value;
                            // Mapowanie typu wiadomości na nazwę
                            message.Name = MapMessageTypeToName(value);
                            break;
                        case "34": // MsgSeqNum
                            if (int.TryParse(value, out int seqNum))
                                message.MsgSeqNum = seqNum;
                            break;
                        case "52": // TransactTime
                            message.TransactTime = value;
                            break;
                        case "44": // Price 
                            if (decimal.TryParse(value, out decimal price))
                                message.Price = price;
                            break;
                        case "54": // Side
                            message.Side = MapSideCodeToName(value);
                            break;
                        case "55": // Symbol
                            message.Symbol = value;
                            break;
                        case "11": // ClOrdID
                            message.ClOrdID = value;
                            break;
                        // Dodaj więcej tagów według potrzeb
                    }
                }
                
                return message;
            }
            catch (Exception ex)
            {
                // Log błędu
                Console.WriteLine($"Error parsing CCG message: {ex.Message}");
                return null;
            }
        }

        private string MapMessageTypeToName(string msgType)
        {
            switch (msgType)
            {
                case "D": return "New Order";
                case "G": return "Order Cancel/Replace";
                case "F": return "Order Cancel";
                case "8": return "Execution Report";
                case "9": return "Order Cancel Reject";
                // Dodaj więcej typów według potrzeb
                default: return msgType;
            }
        }

        private string MapSideCodeToName(string sideCode)
        {
            switch (sideCode)
            {
                case "1": return "Buy";
                case "2": return "Sell";
                case "3": return "Buy minus";
                case "4": return "Sell plus";
                case "5": return "Sell short";
                case "6": return "Sell short exempt";
                case "7": return "Undisclosed";
                case "8": return "Cross";
                case "9": return "Cross short";
                // Dodaj więcej kodów stron według potrzeb
                default: return sideCode;
            }
        }

        private void UpdateOrderBook(CcgMessage message)
        {
            // Update order book based on CCG message content
            // This is a simplified example - you'll need to implement actual logic
            // based on your protocol specification and business rules
            
            // Example: New Order, Modify, Cancel logic
            if (message.Header == "D") // New Order - używamy Header zamiast Type
            {
                var orderEntry = new OrderBookEntry
                {
                    OrderID = message.ClOrdID, // używamy ClOrdID zamiast OrderID
                    TransactTime = message.TransactTime,
                    Side = message.Side,
                    Ticker = message.Symbol,
                    Price = message.Price,
                    // Other fields would be extracted from the message
                    // This is just an example
                    OrderQty = 100, // Placeholder
                    CumQty = 0,
                    LeavesQty = 100, // Placeholder
                    MarketID = "MARKET",
                    Account = "ACC",
                    LastModified = DateTime.Now.ToString("HH:mm:ss.ffffff"),
                    OrigOrderID = message.ClOrdID, // używamy ClOrdID zamiast OrderID
                    Text = ""
                };
                
                OrderBook.Insert(0, orderEntry);
                
                // Update instrument positions if needed
                UpdateInstrumentPositionFromOrder(orderEntry);
            }
            else if (message.Header == "G") // Order Cancel/Replace - używamy Header zamiast Type
            {
                // Find the original order
                var originalOrder = OrderBook.FirstOrDefault(o => o.OrderID == message.ClOrdID); // używamy ClOrdID zamiast OrderID
                if (originalOrder != null)
                {
                    // Mark original as inactive
                    originalOrder.IsActive = false;
                    
                    // Create new modified order
                    var modifiedOrder = new OrderBookEntry
                    {
                        OrderID = message.ClOrdID + "M", // używamy ClOrdID zamiast OrderID
                        TransactTime = message.TransactTime,
                        Side = message.Side,
                        Ticker = message.Symbol,
                        Price = message.Price,
                        // Other fields
                        OrderQty = 100, // Placeholder
                        CumQty = originalOrder.CumQty,
                        LeavesQty = 100 - originalOrder.CumQty, // Placeholder
                        MarketID = originalOrder.MarketID,
                        Account = originalOrder.Account,
                        LastModified = DateTime.Now.ToString("HH:mm:ss.ffffff"),
                        OrigOrderID = originalOrder.OrderID,
                        Text = "Modified"
                    };
                    
                    OrderBook.Insert(0, modifiedOrder);
                }
            }
            else if (message.Header == "F") // Order Cancel - używamy Header zamiast Type
            {
                // Find the original order
                var originalOrder = OrderBook.FirstOrDefault(o => o.OrderID == message.ClOrdID); // używamy ClOrdID zamiast OrderID
                if (originalOrder != null)
                {
                    // Mark as inactive
                    originalOrder.IsActive = false;
                    
                    // Create cancel entry
                    var cancelOrder = new OrderBookEntry
                    {
                        OrderID = originalOrder.OrderID,
                        TransactTime = message.TransactTime,
                        Side = originalOrder.Side,
                        Ticker = originalOrder.Ticker,
                        Price = originalOrder.Price,
                        OrderQty = originalOrder.OrderQty,
                        CumQty = originalOrder.CumQty,
                        LeavesQty = 0, // Cancelled
                        MarketID = originalOrder.MarketID,
                        Account = originalOrder.Account,
                        LastModified = DateTime.Now.ToString("HH:mm:ss.ffffff"),
                        OrigOrderID = originalOrder.OrderID,
                        Text = "Cancelled",
                        IsActive = false
                    };
                    
                    OrderBook.Insert(0, cancelOrder);
                }
            }
            
            // Maintain reasonable collection size
            if (OrderBook.Count > 1000)
                OrderBook.RemoveAt(OrderBook.Count - 1);
        }

        private void UpdateInstrumentPositionFromOrder(OrderBookEntry order)
        {
            // This is a simplified example
            // You'll need to implement actual logic based on your business rules
            
            var instrument = InstrumentPositions.FirstOrDefault(i => i.TickerName == order.Ticker);
            if (instrument == null)
            {
                // Create new instrument position
                instrument = new InstrumentPosition
                {
                    ISIN = "PLACEHOLDER", // You'd need to get the actual ISIN from somewhere
                    TickerName = order.Ticker,
                    Net = 0,
                    OpenLong = 0,
                    OpenShort = 0
                };
                
                InstrumentPositions.Add(instrument);
            }
            
            // Update position based on order
            // This is simplified logic - adjust based on your actual requirements
            if (order.Side == "BUY")
            {
                instrument.OpenLong += order.OrderQty;
                instrument.Net = instrument.OpenLong - instrument.OpenShort;
            }
            else if (order.Side == "SELL")
            {
                instrument.OpenShort += order.OrderQty;
                instrument.Net = instrument.OpenLong - instrument.OpenShort;
            }
        }

        public void Cleanup()
        {
            // Unsubscribe from events
            _tcpService.LogReceived -= OnLogReceived;
            _tcpService.PositionReceived -= OnPositionReceived;
            _tcpService.CapitalReceived -= OnCapitalReceived;
            _tcpService.IOBytesReceived -= OnCcgMessageReceived;

            _udpService.LogReceived -= OnLogReceived;
            _udpService.PositionReceived -= OnPositionReceived;
            _udpService.CapitalReceived -= OnCapitalReceived;
            _udpService.IOBytesReceived -= OnCcgMessageReceived;
        }
    }
}