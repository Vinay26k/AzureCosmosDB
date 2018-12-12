using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
namespace Lab_1
{
    class Program
    {
        private static readonly Uri _endpointUri = new Uri("<URL>");
        private static readonly string _primaryKey = "<PRIMARY KEY>";
        public static async Task Main(string[] args)
        {
            //use below code only once to connect or to create a db
            using (DocumentClient client = new DocumentClient(_endpointUri, _primaryKey))
            {
                // creating new database instance
                Database targetDB = new Database { Id = "EntertainmentDatabase" };
                // create if targetDB doesnot exist
                targetDB = await client.CreateDatabaseIfNotExistsAsync(targetDB);
                await Console.Out.WriteLineAsync($"Database Self-link:\t{targetDB.SelfLink}");
            }


            //Fixed Collection creation
            using (DocumentClient client = new DocumentClient(_endpointUri, _primaryKey))
            {
                await client.OpenAsync();
                Uri databaseLink = UriFactory.CreateDatabaseUri("EntertainmentDatabase");
                DocumentCollection defaultCollection = new DocumentCollection { Id = "DefaultCollection" };
                defaultCollection = await client.CreateDocumentCollectionAsync(databaseLink, defaultCollection);
                await Console.Out.WriteLineAsync($"Default Collection Self-Link\t{defaultCollection.SelfLink}");
            }


            //Unlimited Collection or Custome Collection
            using (DocumentClient client = new DocumentClient(_endpointUri, _primaryKey))
            {
                await client.OpenAsync();
                Uri databaseLink = UriFactory.CreateDatabaseUri("EntertainmentDatabase");
                IndexingPolicy indexingPolicy = new IndexingPolicy
                {
                    IndexingMode = IndexingMode.Consistent,
                    Automatic = true,
                    IncludedPaths = new Collection<IncludedPath>{
                        new IncludedPath{
                            Path ="/*",
                            Indexes = new Collection<Index>{
                                new RangeIndex(DataType.Number, -1),
                                new RangeIndex(DataType.String, -1)
                            }
                        }
                    }
                };
                PartitionKeyDefinition partitionKeyDefinition = new PartitionKeyDefinition
                {
                    Paths = new Collection<string> { "/type" }
                };
                DocumentCollection customCollection = new DocumentCollection
                {
                    Id = "CustomCollection",
                    PartitionKey = partitionKeyDefinition,
                    IndexingPolicy = indexingPolicy
                };
                RequestOptions requestOptions = new RequestOptions
                {
                    OfferThroughput = 10000
                };
                customCollection = await client.CreateDocumentCollectionIfNotExistsAsync(databaseLink, customCollection, requestOptions);
                await Console.Out.WriteLineAsync($"Custom Collection Self-Link:\t{customCollection.SelfLink}");
            }

            //populating foodInteractions
            using (DocumentClient client = new DocumentClient(_endpointUri, _primaryKey))
            {
                await client.OpenAsync();
                Uri collectionLink = UriFactory.CreateDocumentCollectionUri("EntertainmentDatabase", "CustomCollection");
                var foodInteractions = new Bogus.Faker<PurchaseFoodOrBeverage>()
                .RuleFor(i => i.type, (fake) => nameof(PurchaseFoodOrBeverage))
                .RuleFor(i => i.unitPrice, (fake) => Math.Round(fake.Random.Decimal(1.99m, 15.9m), 2))
                .RuleFor(i => i.quantity, (fake) => fake.Random.Number(1, 5))
                .RuleFor(i => i.totalPrice, (fake, user) => Math.Round(user.unitPrice * user.quantity, 2))
                .Generate(500);
                foreach (var interaction in foodInteractions)
                {
                    ResourceResponse<Document> result = await client.CreateDocumentAsync(collectionLink, interaction);
                    await Console.Out.WriteLineAsync($"Document #{foodInteractions.IndexOf(interaction):000} Created\t{result.Resource.Id}");
                };
            }

            //populating tv interactions
            using (DocumentClient client = new DocumentClient(_endpointUri, _primaryKey))
            {
                await client.OpenAsync();
                Uri collectionLink = UriFactory.CreateDocumentCollectionUri("EntertainmentDatabase", "CustomCollection");
                var tvInteractions = new Bogus.Faker<WatchLiveTelevisionChannel>()
                    .RuleFor(i => i.type, (fake) => nameof(WatchLiveTelevisionChannel))
                    .RuleFor(i => i.minutesViewed, (fake) => fake.Random.Number(1, 45))
                    .RuleFor(i => i.channelName, (fake) => fake.PickRandom(new List<string> { "NEWS-6", "DRAMA-15", "ACTION-12", "DOCUMENTARY-4", "SPORTS-8" }))
                    .Generate(500);
                foreach (var interaction in tvInteractions)
                {
                    ResourceResponse<Document> result = await client.CreateDocumentAsync(collectionLink, interaction);
                    await Console.Out.WriteLineAsync($"Document #{tvInteractions.IndexOf(interaction):000} Created\t{result.Resource.Id}");
                }
            }

            //populating Map interactions
            using (DocumentClient client = new DocumentClient(_endpointUri, _primaryKey))
            {
                await client.OpenAsync();
                Uri collectionLink = UriFactory.CreateDocumentCollectionUri("EntertainmentDatabase", "CustomCollection");
                var mapInteractions = new Bogus.Faker<ViewMap>()
                    .RuleFor(i => i.type, (fake) => nameof(ViewMap))
                    .RuleFor(i => i.minutesViewed, (fake) => fake.Random.Number(1, 45))
                    .Generate(500);
                foreach (var interaction in mapInteractions)
                {
                    ResourceResponse<Document> result = await client.CreateDocumentAsync(collectionLink, interaction);
                    await Console.Out.WriteLineAsync($"Document #{mapInteractions.IndexOf(interaction):000} Created\t{result.Resource.Id}");
                }
            }
        }
    }
}
