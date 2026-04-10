namespace UserAPI.Settings
{
    public class KafkaSettings
    {
        public string BootstrapServers { get; set; } = null!;

        public string ConsumeTopic { get; set; } = null!;

        public string ProduceTopic { get; set; } = null!;

        public string GroupId { get; set; } = null!;
    }
}
