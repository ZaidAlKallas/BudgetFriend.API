using Xunit.v3;

[assembly: Parallelization(Mode = Xunit.Sdk.ParallelMode.None)]

namespace BudgetFriend.API.IntegrationTests.CustomWebApplicationFactory;

[CollectionDefinition("IntegrationTests")]
public sealed class IntegrationTestCollection : ICollectionFixture<BudgetFriendApiFactory>;
