using Confluent.Kafka;
using Microsoft.Extensions.Options;
using System.Text.Json;
using UserAPI.KafkaModels;
using UserAPI.Settings;

namespace UserAPI.Services
{
    public class KafkaUserProcessingService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly KafkaSettings _kafkaSettings;


        public KafkaUserProcessingService(IServiceScopeFactory scopeFactory, IOptions<KafkaSettings> kafkasettings)
        {
            _scopeFactory = scopeFactory;
            _kafkaSettings = kafkasettings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _kafkaSettings.BootstrapServers,
                GroupId = _kafkaSettings.GroupId,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
            var producerConfig = new ProducerConfig { BootstrapServers = _kafkaSettings.BootstrapServers };
            using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
            using var producer = new ProducerBuilder<Null, string>(producerConfig).Build();
            consumer.Subscribe(_kafkaSettings.ConsumeTopic);
            await Task.Yield();
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var consumeResult = consumer.Consume(stoppingToken);
                    var messageJson = consumeResult.Message.Value;
                    try
                    {
                        var creationMessage = JsonSerializer.Deserialize<ObjectCreationMessage>(messageJson);
                        if (creationMessage == null) continue;
                        using(var scope = _scopeFactory.CreateScope())
                        {
                            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                            var user = await userService.GetUserAsync(creationMessage.UserId);
                            if(user != null)
                            {
                                user.RegisteredObjects++;
                                await userService.UpdateAsync(user.Id, user);
                                var confirmation = new ObjectConfirmationMessage
                                {
                                    ObjectId = creationMessage.ObjectId,
                                    ConfirmationTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                                };
                                await producer.ProduceAsync(_kafkaSettings.ProduceTopic, new Message<Null, string>
                                {
                                    Value = JsonSerializer.Serialize(confirmation)
                                }, stoppingToken);
                            }
                        }
                    }
                    catch { }
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                consumer.Close();
            }
        }
    }
}
