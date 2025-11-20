using Catstronauts.GraphQL.GraphQL.DataLoaders;
using Catstronauts.GraphQL.GraphQL.Mutations;
using Catstronauts.GraphQL.GraphQL.Queries;
using Catstronauts.GraphQL.Services;

var builder = WebApplication.CreateBuilder(args);

// Register HttpClient with IHttpClientFactory for TrackService
// This manages connection pooling and lifecycle automatically
builder.Services.AddHttpClient<ITrackService, TrackService>();

// Register GraphQL server with Hot Chocolate
// AddQueryType: Registers the Query class for read operations
// AddMutationType: Registers the Mutation class for write operations
// DataLoaders: Batch and cache data fetching to solve N+1 problem
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    // Register DataLoaders for batching and caching
    // These are automatically scoped to each GraphQL request
    .RegisterDataLoader<AuthorDataLoader>()
    .RegisterDataLoader<ModuleDataLoader>();

var app = builder.Build();

// Map the GraphQL endpoint to /graphql
app.MapGraphQL();

app.Run();
