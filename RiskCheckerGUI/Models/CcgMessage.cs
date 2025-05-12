using System;

namespace RiskCheckerGUI.Models
{
    public class CcgMessage
    {
        public int Nr { get; set; }          // Numer kolejny wiadomości
        public string Header { get; set; }    // Nagłówek wiadomości
        public string Name { get; set; }      // Nazwa wiadomości (np. Order, Trade)
        public int MsgSeqNum { get; set; }    // Numer sekwencyjny wiadomości
        public DateTime DateReceived { get; set; }  // Data otrzymania
        public string TransactTime { get; set; }    // Czas transakcji
        public decimal Price { get; set; }          // Cena
        public string Side { get; set; }            // Strona (Buy/Sell)
        public string Symbol { get; set; }          // Symbol instrumentu
        public string ClOrdID { get; set; }         // ID zlecenia klienta
        
        // Surowe dane wiadomości do ewentualnego dalszego przetwarzania
        public byte[] RawData { get; set; }
    }
}