using Confluent.Kafka;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Prometheus;
using System.Text.Json;
using UserAPI.KafkaModels;
using UserAPI.Settings;

namespace UserAPI.Services
{
    public class KafkaUserProcessingService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly KafkaSettings _kafkaSettings;

        private readonly IProducer<Null, string> _producer;

        private static readonly Counter KafkaConsumeTotal = Metrics.
            CreateCounter("user_api_kafka_consume_total", "Количество входящих сообщений",
                new CounterConfiguration { LabelNames = new[] { "topic", "status" } }
        );

        private static readonly Counter KafkaProduceTotal = Metrics.
            CreateCounter("user_api_kafka_produce_total", "Количество отправленных ответов",
                new CounterConfiguration { LabelNames = new[] { "topic", "status" } }
        );

        private static readonly Histogram ProcessingLatency = Metrics.
            CreateHistogram("user_api_kafka_processing_duration_seconds", "Время обработки сообщения внутри UserAPI",
                new HistogramConfiguration
                {
                    LabelNames = new[] { "topic" },
                    Buckets = new[] { 0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1.0 }
                }
        );

        public KafkaUserProcessingService(IServiceScopeFactory scopeFactory, IOptions<KafkaSettings> kafkasettings, IProducer<Null, string> producer)
        {
            _scopeFactory = scopeFactory;
            _kafkaSettings = kafkasettings.Value;
            _producer = producer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _kafkaSettings.BootstrapServers,
                GroupId = _kafkaSettings.GroupId,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
            using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
            consumer.Subscribe(_kafkaSettings.ConsumeTopic);
            await Task.Yield();
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var consumeResult = consumer.Consume(stoppingToken);
                    var messageJson = consumeResult.Message.Value;
                    using (ProcessingLatency.WithLabels(_kafkaSettings.ConsumeTopic).NewTimer())
                    {
                        try
                        {
                            var creationMessage = JsonSerializer.Deserialize<ObjectCreationMessage>(messageJson);
                            if (creationMessage == null) continue;
                            using (var scope = _scopeFactory.CreateScope())
                            {
                                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                                var confirmation = new ObjectConfirmationMessage
                                {
                                    ObjectId = creationMessage.ObjectId,
                                    ConfirmationTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                                };
                                var user = await userService.GetUserAsync(creationMessage.UserId);
                                if (user != null)
                                {
                                    user.RegisteredObjects++;
                                    await userService.UpdateAsync(user.Id, user);
                                    confirmation.IsUserExists = true;
                                }
                                else
                                {
                                    confirmation.IsUserExists = false;
                                }
                                await _producer.ProduceAsync(_kafkaSettings.ProduceTopic, new Message<Null, string>
                                {
                                    Value = JsonSerializer.Serialize(confirmation)
                                }, stoppingToken);
                                KafkaProduceTotal.WithLabels(_kafkaSettings.ProduceTopic, "success").Inc();
                            }
                            KafkaConsumeTotal.WithLabels(_kafkaSettings.ConsumeTopic, "success").Inc();
                        }
                        catch (JsonException)
                        {
                            KafkaConsumeTotal.WithLabels(_kafkaSettings.ConsumeTopic, "serialization_error").Inc();
                        }
                        catch (Exception)
                        {
                            KafkaConsumeTotal.WithLabels(_kafkaSettings.ConsumeTopic, "error").Inc();
                            KafkaProduceTotal.WithLabels(_kafkaSettings.ProduceTopic, "error").Inc();
                        }
                    }
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
