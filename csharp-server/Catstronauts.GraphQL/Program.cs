using Catstronauts.GraphQL.GraphQL.Queries;

var builder = WebApplication.CreateBuilder(args);

// Register GraphQL server with Hot Chocolate
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>();

var app = builder.Build();

// Map the GraphQL endpoint to /graphql
app.MapGraphQL();

app.Run();
