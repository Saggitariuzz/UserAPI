namespace UserAPI.KafkaModels
{
    public class ObjectConfirmationMessage
    {
        public string ObjectId { get; set; } = string.Empty;
        public string ConfirmationTime { get; set; } = string.Empty;
        public bool IsUserExists { get; set; }
    }
}
