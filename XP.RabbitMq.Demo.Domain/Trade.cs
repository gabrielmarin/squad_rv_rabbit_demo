namespace XP.RabbitMq.Demo.Domain
{
    public class Trade
    {
        public string Client { get; set; }
        public string Symbol { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }


        public override string ToString() => $"{Client} {(Quantity < 0 ? "Selling" : "Buying")} {Symbol} {Quantity}@{Price:C}";

    }
}
