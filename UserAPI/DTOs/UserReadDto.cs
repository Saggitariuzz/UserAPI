namespace UserAPI.DTOs
{
    public class UserReadDto
    {
        public string Id { get; set; } = string.Empty;

        public string Login { get; set; } = string.Empty;

        public int RegisteredObjects { get; set; }
    }
}
