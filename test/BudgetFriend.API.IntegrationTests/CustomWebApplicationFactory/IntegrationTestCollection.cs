[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace BudgetFriend.API.IntegrationTests.CustomWebApplicationFactory;

[CollectionDefinition("IntegrationTests")]
public sealed class IntegrationTestCollection : ICollectionFixture<BudgetFriendApiFactory>;
