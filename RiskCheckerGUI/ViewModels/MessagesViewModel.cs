using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Threading;
using System.IO;
using System.Diagnostics;
using RiskCheckerGUI.Models;
using RiskCheckerGUI.Services;
using System.Text;

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

        // Computed properties for metrics
        public string MessageUsageText => $"{CapitalUsage?.MessageUsagePercent ?? 0:F1}% of {CapitalUsage?.MessageLimit ?? 0}";
        public string CapitalUsageText => $"{CapitalUsage?.CapitalUsagePercent ?? 0:F1}% of {CapitalUsage?.CapitalLimit ?? 0}";

        #endregion

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
            _capitalUsage = new CapitalUsage();

            // Domyślne wartości CapitalUsage
            _capitalUsage.MessageLimit = 115200;
            _capitalUsage.CapitalLimit = 100000;

            // Synchronizacja kolekcji
            BindingOperations.EnableCollectionSynchronization(_logs, _syncLock);
            BindingOperations.EnableCollectionSynchronization(_ccgMessages, _syncLock);
            BindingOperations.EnableCollectionSynchronization(_orderBook, _syncLock);
            BindingOperations.EnableCollectionSynchronization(_instrumentPositions, _syncLock);

            // Ustaw filtry dla kolekcji
            SetupCollectionFilters();

            // Subskrybuj zdarzenia z serwisów
            SubscribeToEvents();
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
                Debug.WriteLine($"Position update received: {e.ISIN}, Net={e.Net}, Long={e.OpenLong}, Short={e.OpenShort}");
                
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
                Debug.WriteLine($"Capital update received: Open={e.OpenCapital}, Accrued={e.AccruedCapital}, Total={e.TotalCapital}");
                
                CapitalUsage.OpenCapital = e.OpenCapital;
                CapitalUsage.AccruedCapital = e.AccruedCapital;
                CapitalUsage.TotalCapital = e.TotalCapital;
                
                // Update used capital (this is an approximation, adjust as needed)
                CapitalUsage.UsedCapital = e.TotalCapital;
                
                // Trigger updates for computed properties
                OnPropertyChanged(nameof(MessageUsageText));
                OnPropertyChanged(nameof(CapitalUsageText));
            });
        }

        private void OnCcgMessageReceived(object sender, byte[] messageBytes)
        {
            _dispatcher.InvokeAsync(() =>
            {
                try
                {
                    // Parse CCG message
                    var ccgMessage = ParseCcgMessage(messageBytes);
                    if (ccgMessage != null)
                    {
                        Debug.WriteLine($"CCG Message received: {ccgMessage.Header}, {ccgMessage.Name}, Symbol={ccgMessage.Symbol}");
                        
                        // Add to messages list
                        CcgMessages.Insert(0, ccgMessage);
                        if (CcgMessages.Count > 1000)
                            CcgMessages.RemoveAt(CcgMessages.Count - 1);

                        // Update message count for limits
                        CapitalUsage.UsedMessages++;
                        OnPropertyChanged(nameof(MessageUsageText));

                        // Update order book
                        UpdateOrderBook(ccgMessage);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error processing CCG message: {ex.Message}");
                }
            });
        }

        private CcgMessage ParseCcgMessage(byte[] messageBytes)
        {
            try
            {
                // Użyj MemoryStream dla wygodniejszego parsowania
                using (var ms = new MemoryStream(messageBytes))
                using (var reader = new BinaryReader(ms, Encoding.ASCII))
                {
                    string messageStr = Encoding.ASCII.GetString(messageBytes);
                    Debug.WriteLine($"Parsing CCG message: {messageStr}");
                    
                    // Stwórz podstawową wiadomość CCG
                    var ccgMessage = new CcgMessage
                    {
                        Nr = CcgMessages.Count + 1,
                        DateReceived = DateTime.Now,
                        RawData = messageBytes
                    };
                    
                    // Próba parsowania jako formatu FIX-like
                    // Format FIX: 8=FIX.4.4|9=100|35=D|34=123|...
                    if (messageStr.Contains("8=FIX"))
                    {
                        // Split na poszczególne tagi
                        var tags = messageStr.Split('|');
                        foreach (var tag in tags)
                        {
                            var parts = tag.Split('=');
                            if (parts.Length != 2) continue;
                            
                            var tagId = parts[0];
                            var value = parts[1];
                            
                            switch (tagId)
                            {
                                case "35": // MsgType
                                    ccgMessage.Header = value;
                                    ccgMessage.Name = MapMessageTypeToName(value);
                                    break;
                                case "34": // MsgSeqNum
                                    if (int.TryParse(value, out int seqNum))
                                        ccgMessage.MsgSeqNum = seqNum;
                                    break;
                                case "52": // TransactTime
                                    ccgMessage.TransactTime = value;
                                    break;
                                case "44": // Price 
                                    if (decimal.TryParse(value, out decimal price))
                                        ccgMessage.Price = price;
                                    break;
                                case "54": // Side
                                    ccgMessage.Side = MapSideCodeToName(value);
                                    break;
                                case "55": // Symbol
                                    ccgMessage.Symbol = value;
                                    break;
                                case "11": // ClOrdID
                                    ccgMessage.ClOrdID = value;
                                    break;
                            }
                        }
                    }
                    else
                    {
                        // Próba podstawowego parsowania tekstu
                        ccgMessage.Header = "UNKNOWN";
                        ccgMessage.Name = "Raw CCG Message";
                        ccgMessage.ClOrdID = "N/A";
                        
                        // Szukaj typowych wzorców w treści wiadomości
                        if (messageStr.Contains("BUY") || messageStr.Contains("SELL"))
                        {
                            if (messageStr.Contains("BUY"))
                                ccgMessage.Side = "Buy";
                            else if (messageStr.Contains("SELL")) 
                                ccgMessage.Side = "Sell";
                            
                            // Szukaj symbolu (zazwyczaj 3-6 znaków, litery)
                            var symbolPattern = @"\b[A-Z]{2,6}\b";
                            var symbolMatch = Regex.Match(messageStr, symbolPattern);
                            if (symbolMatch.Success)
                            {
                                ccgMessage.Symbol = symbolMatch.Value;
                            }
                            
                            // Szukaj ceny (liczba z kropką dziesiętną)
                            var pricePattern = @"\d+\.\d+";
                            var priceMatch = Regex.Match(messageStr, pricePattern);
                            if (priceMatch.Success && decimal.TryParse(priceMatch.Value, out decimal price))
                            {
                                ccgMessage.Price = price;
                            }
                        }
                    }
                    
                    return ccgMessage;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error parsing CCG message: {ex.Message}");
                
                // Zwróć podstawową wiadomość w przypadku błędu
                return new CcgMessage
                {
                    Nr = CcgMessages.Count + 1,
                    Header = "ERROR",
                    Name = "Parse Error",
                    DateReceived = DateTime.Now,
                    RawData = messageBytes
                };
            }
        }

        private void UpdateOrderBook(CcgMessage message)
        {
            // Update order book based on CCG message content
            try
            {
                // Przykładowe logowanie
                Debug.WriteLine($"Updating order book with message: Header={message.Header}, Symbol={message.Symbol}, ClOrdID={message.ClOrdID}");
                
                // Example: New Order, Modify, Cancel logic
                if (message.Header == "D") // New Order - używamy Header
                {
                    var orderEntry = new OrderBookEntry
                    {
                        OrderID = message.ClOrdID ?? $"ORD-{DateTime.Now.Ticks}",
                        TransactTime = message.TransactTime ?? DateTime.Now.ToString("HH:mm:ss.fff"),
                        Side = message.Side ?? "Unknown",
                        Ticker = message.Symbol ?? "Unknown",
                        Price = message.Price,
                        // Other fields would be extracted from the message
                        OrderQty = 100, // Placeholder
                        CumQty = 0,
                        LeavesQty = 100, // Placeholder
                        MarketID = "MARKET",
                        Account = "ACC",
                        LastModified = DateTime.Now.ToString("HH:mm:ss.ffffff"),
                        OrigOrderID = message.ClOrdID ?? $"ORD-{DateTime.Now.Ticks}",
                        Text = "",
                        IsActive = true
                    };
                    
                    OrderBook.Insert(0, orderEntry);
                    
                    // Update instrument positions if needed
                    UpdateInstrumentPositionFromOrder(orderEntry);
                }
                else if (message.Header == "G") // Order Cancel/Replace
                {
                    // Find the original order
                    var originalOrder = OrderBook.FirstOrDefault(o => o.OrderID == message.ClOrdID);
                    if (originalOrder != null)
                    {
                        // Mark original as inactive
                        originalOrder.IsActive = false;
                        
                        // Create new modified order
                        var modifiedOrder = new OrderBookEntry
                        {
                            OrderID = $"{message.ClOrdID}-M", // Modified order suffix
                            TransactTime = message.TransactTime ?? DateTime.Now.ToString("HH:mm:ss.fff"),
                            Side = message.Side ?? originalOrder.Side,
                            Ticker = message.Symbol ?? originalOrder.Ticker,
                            Price = message.Price > 0 ? message.Price : originalOrder.Price,
                            // Other fields
                            OrderQty = originalOrder.OrderQty, // Placeholder
                            CumQty = originalOrder.CumQty,
                            LeavesQty = originalOrder.OrderQty - originalOrder.CumQty, // Placeholder
                            MarketID = originalOrder.MarketID,
                            Account = originalOrder.Account,
                            LastModified = DateTime.Now.ToString("HH:mm:ss.ffffff"),
                            OrigOrderID = originalOrder.OrderID,
                            Text = "Modified",
                            IsActive = true
                        };
                        
                        OrderBook.Insert(0, modifiedOrder);
                    }
                    else
                    {
                        Debug.WriteLine($"Could not find original order with ID: {message.ClOrdID}");
                    }
                }
                else if (message.Header == "F") // Order Cancel
                {
                    // Find the original order
                    var originalOrder = OrderBook.FirstOrDefault(o => o.OrderID == message.ClOrdID);
                    if (originalOrder != null)
                    {
                        // Mark as inactive
                        originalOrder.IsActive = false;
                        
                        // Create cancel entry
                        var cancelOrder = new OrderBookEntry
                        {
                            OrderID = originalOrder.OrderID,
                            TransactTime = message.TransactTime ?? DateTime.Now.ToString("HH:mm:ss.fff"),
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
                    else
                    {
                        Debug.WriteLine($"Could not find original order with ID: {message.ClOrdID}");
                    }
                }
                
                // Maintain reasonable collection size
                if (OrderBook.Count > 1000)
                    OrderBook.RemoveAt(OrderBook.Count - 1);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating order book: {ex.Message}");
            }
        }

        private void UpdateInstrumentPositionFromOrder(OrderBookEntry order)
        {
            try
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
                if (order.Side == "Buy")
                {
                    instrument.OpenLong += order.OrderQty;
                    instrument.Net = instrument.OpenLong - instrument.OpenShort;
                }
                else if (order.Side == "Sell")
                {
                    instrument.OpenShort += order.OrderQty;
                    instrument.Net = instrument.OpenLong - instrument.OpenShort;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating instrument position: {ex.Message}");
            }
        }

        // Helper methods for CCG messages
        private string MapMessageTypeToName(string msgType)
        {
            switch (msgType)
            {
                case "D": return "New Order";
                case "F": return "Cancel Order";
                case "G": return "Modify Order";
                case "8": return "Execution Report";
                case "9": return "Cancel Reject";
                case "0": return "Heartbeat";
                case "A": return "Logon";
                case "5": return "Logout";
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
                default: return sideCode;
            }
        }

        // Demo data methods
        public void LoadSampleData()
        {
            // Clear existing data
            ClearData();
            
            // Add sample data
            AddSampleData();
        }
        
        public void ClearData()
        {
            CcgMessages.Clear();
            Logs.Clear();
            OrderBook.Clear();
            InstrumentPositions.Clear();
            
            // Reset capital usage
            CapitalUsage.OpenCapital = 0;
            CapitalUsage.AccruedCapital = 0;
            CapitalUsage.TotalCapital = 0;
            CapitalUsage.UsedMessages = 0;
            CapitalUsage.UsedCapital = 0;
            
            OnPropertyChanged(nameof(MessageUsageText));
            OnPropertyChanged(nameof(CapitalUsageText));
        }

        private void AddSampleData()
        {
            // Przykładowe dane dla kapitału
            CapitalUsage.OpenCapital = 5000;
            CapitalUsage.AccruedCapital = 2468.89;
            CapitalUsage.TotalCapital = CapitalUsage.OpenCapital + CapitalUsage.AccruedCapital;
            CapitalUsage.UsedMessages = 5000;
            CapitalUsage.MessageLimit = 115200;
            CapitalUsage.UsedCapital = 25000;
            CapitalUsage.CapitalLimit = 100000;
            
            OnPropertyChanged(nameof(MessageUsageText));
            OnPropertyChanged(nameof(CapitalUsageText));
            
            // Przykładowe instrumenty
            InstrumentPositions.Add(new InstrumentPosition
            {
                ISIN = "PLPKO0000016",
                TickerName = "PKO",
                Net = 100,
                OpenLong = 100,
                OpenShort = 0
            });
            
            InstrumentPositions.Add(new InstrumentPosition
            {
                ISIN = "PLKGHM000017",
                TickerName = "KGH",
                Net = -50,
                OpenLong = 0,
                OpenShort = 50
            });
            
            // Przykładowe wiadomości CCG
            AddSampleCcgMessages();
        }

        private void AddSampleCcgMessages()
        {
            var random = new Random();
            
            // Kilka przykładowych typów wiadomości
            var msgTypes = new[] { "D", "G", "F", "8", "9" };
            var sides = new[] { "1", "2" };
            var symbols = new[] { "PKO", "PGE", "KGH", "PZU", "OPL" };
            
            for (int i = 1; i <= 10; i++)
            {
                var msgType = msgTypes[random.Next(msgTypes.Length)];
                var side = sides[random.Next(sides.Length)];
                var symbol = symbols[random.Next(symbols.Length)];
                
                var ccgMessage = new CcgMessage
                {
                    Nr = i,
                    Header = msgType,
                    Name = MapMessageTypeToName(msgType),
                    MsgSeqNum = i + 1000,
                    DateReceived = DateTime.Now.AddSeconds(-random.Next(60)),
                    TransactTime = DateTime.Now.AddSeconds(-random.Next(120)).ToString("yyyyMMdd-HH:mm:ss.fff"),
                    Price = Math.Round(10 + (decimal)random.NextDouble() * 90, 2),
                    Side = MapSideCodeToName(side),
                    Symbol = symbol,
                    ClOrdID = $"ORD{100000 + i}"
                };
                
                CcgMessages.Add(ccgMessage);
                
                // Dodaj odpowiednie wpisy do OrderBook
                if (msgType == "D") // New Order
                {
                    var orderEntry = new OrderBookEntry
                    {
                        OrderID = ccgMessage.ClOrdID,
                        TransactTime = ccgMessage.TransactTime,
                        Side = ccgMessage.Side,
                        Ticker = ccgMessage.Symbol,
                        Price = ccgMessage.Price,
                        OrderQty = 100 + random.Next(900),
                        CumQty = 0,
                        LeavesQty = 100 + random.Next(900),
                        MarketID = "GPW",
                        Account = $"ACC{random.Next(10)}",
                        LastModified = DateTime.Now.ToString("HH:mm:ss.ffffff"),
                        OrigOrderID = ccgMessage.ClOrdID,
                        Text = "",
                        IsActive = true
                    };
                    
                    OrderBook.Add(orderEntry);
                }
            }
            
            // Dodaj przykładowe logi
            Logs.Add(new LogMessage { Type = LogType.Info, Message = "Application started", Timestamp = DateTime.Now.AddMinutes(-5) });
            Logs.Add(new LogMessage { Type = LogType.Debug, Message = "Initializing systems", Timestamp = DateTime.Now.AddMinutes(-4) });
            Logs.Add(new LogMessage { Type = LogType.Info, Message = "Systems initialized", Timestamp = DateTime.Now.AddMinutes(-3) });
            Logs.Add(new LogMessage { Type = LogType.Warning, Message = "Network latency detected", Timestamp = DateTime.Now.AddMinutes(-2) });
            Logs.Add(new LogMessage { Type = LogType.Info, Message = "Ready to process orders", Timestamp = DateTime.Now.AddMinutes(-1) });
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