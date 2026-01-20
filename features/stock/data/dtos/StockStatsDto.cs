namespace mtc_app.features.stock.data.dtos
{
    public class StockStatsDto
    {
        public int TotalRequests { get; set; }
        public int PendingCount { get; set; }
        public int ReadyCount { get; set; }
    }
}
