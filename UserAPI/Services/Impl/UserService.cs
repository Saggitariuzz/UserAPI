using Microsoft.Extensions.Options;
using MongoDB.Driver;
using UserAPI.Models;

namespace UserAPI.Services.Impl
{
    public class UserService : IUserService
    {
        private readonly IMongoCollection<User> _userCollection;

        public UserService(IOptions<UsersDBSettings> usersDbSettings)
        {
            var mongoClient = new MongoClient(usersDbSettings.Value.ConnectionString);
            var mongoDatabase = mongoClient.GetDatabase(usersDbSettings.Value.DatabaseName);
            _userCollection = mongoDatabase.GetCollection<User>(usersDbSettings.Value.CollectionName);
        }

        public async Task<List<User>> GetAllAsync()
        {
            List<User> users;
            users = await _userCollection.Aggregate().Sample(1000).ToListAsync();
            return users;
        }

        public async Task<User> GetUserAsync(string id)
        {
            User user;
            user = await _userCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
            return user;
        }

        public async Task CreateAsync(User user)
        {
            await _userCollection.InsertOneAsync(user);
        }

        public async Task UpdateAsync(string id, User user)
        {
            await _userCollection.ReplaceOneAsync(x => x.Id == id, user);
        }

        public async Task DeleteAsync(string id)
        {
            await _userCollection.DeleteOneAsync(x => x.Id == id);
        }
    }
}
