using Catstronauts.GraphQL.GraphQL.Queries;
using Catstronauts.GraphQL.Services;

var builder = WebApplication.CreateBuilder(args);

// Register HttpClient with IHttpClientFactory for TrackService
// This manages connection pooling and lifecycle automatically
builder.Services.AddHttpClient<ITrackService, TrackService>();

// Register GraphQL server with Hot Chocolate
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>();

var app = builder.Build();

// Map the GraphQL endpoint to /graphql
app.MapGraphQL();

app.Run();
