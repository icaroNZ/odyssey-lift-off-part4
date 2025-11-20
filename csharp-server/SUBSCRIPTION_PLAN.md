# Stage 7: GraphQL Subscriptions - Real-Time Updates

## 📋 Overview

This stage adds **GraphQL Subscriptions** to the Catstronauts application, enabling real-time data updates using WebSockets. Subscriptions are the third pillar of GraphQL (alongside queries and mutations) and are essential for modern, interactive applications.

**What we'll build:** A real-time subscription that notifies clients when a track's view count changes.

**Learning outcomes:**
- Understand what GraphQL subscriptions are and when to use them
- Learn the publish-subscribe (PubSub) pattern
- Implement WebSocket communication with Hot Chocolate
- Connect mutations to subscriptions (trigger pattern)
- Test subscription resolvers
- Understand subscription scalability considerations

---

## 🎯 What Are GraphQL Subscriptions?

### The Three Pillars of GraphQL

| Operation | Purpose | Transport | Pattern |
|-----------|---------|-----------|---------|
| **Query** | Read data | HTTP GET/POST | Request → Response |
| **Mutation** | Write data | HTTP POST | Request → Response |
| **Subscription** | Real-time updates | **WebSocket** | Subscribe → Stream of events |

### Subscriptions vs Queries/Mutations

**Queries & Mutations:**
```
Client                    Server
  |                         |
  |-----> Request --------> |
  |                         | (Process)
  | <----- Response ------- |
  |                         |
(Connection closes)
```

**Subscriptions:**
```
Client                    Server
  |                         |
  |---> Subscribe --------> |
  |                         | (Keep connection open)
  | <---- Event 1 --------- |
  |                         |
  | <---- Event 2 --------- |
  |                         |
  | <---- Event 3 --------- |
  |                         |
(Connection stays open until unsubscribe)
```

### Key Differences

**Queries/Mutations:**
- Request/response pattern
- HTTP protocol (stateless)
- Client initiates, server responds once
- Connection closes immediately

**Subscriptions:**
- Publish/subscribe pattern
- WebSocket protocol (stateful, persistent connection)
- Client subscribes, server pushes multiple updates
- Connection stays open (long-lived)
- Server can send data without client request

---

## 🎓 Interview-Ready Explanation

**Q: "What are GraphQL subscriptions and when would you use them?"**

**A:** "GraphQL subscriptions enable real-time, bi-directional communication between client and server using WebSockets. While queries and mutations use request/response over HTTP, subscriptions maintain an open connection and allow the server to push updates to subscribed clients.

**Use cases:**
- **Real-time notifications**: Chat messages, social media likes, alerts
- **Live data**: Stock prices, sports scores, view counts
- **Collaborative editing**: Google Docs-style multi-user editing
- **Activity feeds**: New posts, comments, followers
- **IoT dashboards**: Sensor data, device status updates

**When NOT to use:**
- Simple data fetching (use queries)
- Infrequent updates (polling queries is simpler)
- Write operations (use mutations)
- High-scale broadcast (consider specialized pub/sub systems)

The key is: Use subscriptions when you need **immediate updates** pushed from server to client, not when client needs to **pull** data on demand."

---

## 🏗️ What We'll Build

### Feature: Track View Count Subscription

**Scenario:** When a user increments a track's view count (via mutation), all subscribed clients receive real-time updates about the new view count.

**GraphQL Subscription:**
```graphql
subscription OnTrackViewCountUpdated($trackId: String!) {
  trackViewCountUpdated(trackId: $trackId) {
    id
    title
    numberOfViews
  }
}
```

**Example Flow:**
```
1. Client A subscribes to track "c_0" view count updates
2. Client B subscribes to track "c_0" view count updates
3. Client C calls incrementTrackViews mutation for track "c_0"
4. Server:
   a. Increments view count (100 → 101)
   b. Publishes event: "Track c_0 updated, views = 101"
5. Both Client A and Client B receive the update immediately
6. Clients update their UI with new count (no refresh needed!)
```

**Real-world analogy:** Think of YouTube's view counter. When you watch a video, you might see the view count update in real-time as other people watch it too. That's powered by subscriptions (or similar tech like Server-Sent Events).

---

## 📐 Architecture: The PubSub Pattern

### Publish-Subscribe Pattern Explained

**Components:**

1. **Publishers** (Mutations)
   - Generate events when data changes
   - Example: `IncrementTrackViews` mutation

2. **Event Channel** (Topic)
   - Named channel for specific event types
   - Example: "TrackViewCountUpdated"

3. **Subscribers** (Subscription resolvers)
   - Listen to specific event channels
   - Receive events and send to clients

4. **PubSub Engine** (In-Memory or External)
   - Manages event distribution
   - Hot Chocolate provides in-memory implementation
   - Production: Redis, RabbitMQ, Azure Service Bus, etc.

**Flow Diagram:**
```
┌─────────────────────────────────────────────────────────────┐
│                        GraphQL Server                        │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────┐         ┌──────────────┐                 │
│  │   Mutation   │         │ Subscription │                 │
│  │              │         │   Resolver   │                 │
│  │ Increment    │         │              │                 │
│  │ TrackViews   │         │ Subscribe to │                 │
│  │              │         │ Track Updates│                 │
│  └──────┬───────┘         └──────▲───────┘                 │
│         │                        │                          │
│         │ Publish                │ Subscribe                │
│         │                        │                          │
│         ▼                        │                          │
│  ┌─────────────────────────────────────┐                   │
│  │        PubSub Event Bus             │                   │
│  │   Topic: "TrackViewCountUpdated"    │                   │
│  └─────────────────────────────────────┘                   │
│                                                              │
└─────────────────────────────────────────────────────────────┘
         │                                          │
         │                                          │
    ┌────▼────┐                              ┌─────▼─────┐
    │ Client A│                              │ Client B  │
    │(WebSocket)                             │(WebSocket)│
    └─────────┘                              └───────────┘
```

### In-Memory vs External PubSub

**In-Memory (Development/Small Scale):**
```csharp
builder.Services.AddInMemorySubscriptions();
```
- ✅ Simple, no external dependencies
- ✅ Fast, low latency
- ❌ Single server only (no horizontal scaling)
- ❌ Events lost on server restart
- ❌ Can't share events across multiple server instances

**External (Production/Scale):**
```csharp
// Redis example
builder.Services.AddRedisSubscriptions();

// Azure Service Bus example
builder.Services.AddAzureServiceBusSubscriptions();
```
- ✅ Horizontal scaling (multiple servers)
- ✅ Events persist across restarts
- ✅ Shared across server instances
- ❌ Additional infrastructure required
- ❌ Slightly higher latency

**Our implementation:** We'll use in-memory for simplicity, but structure the code to easily swap to external systems.

---

## 🛠️ Implementation Plan

### Step 1: Create Subscription Resolver (30 minutes)

**File:** `GraphQL/Subscriptions/Subscription.cs`

**What we'll implement:**
```csharp
public class Subscription
{
    [Subscribe]
    [Topic("TrackViewCountUpdated_{trackId}")]
    public Track TrackViewCountUpdated(
        string trackId,
        [EventMessage] Track track)
    {
        return track;
    }
}
```

**Key concepts:**
- **[Subscribe]**: Marks method as subscription resolver
- **[Topic]**: Defines event channel name
- **Dynamic topics**: `{trackId}` creates separate channel per track
- **[EventMessage]**: Receives the published event payload

**Why dynamic topics?**
- Without: All clients get all track updates (inefficient)
- With: Clients only get updates for tracks they care about

### Step 2: Update Mutation to Publish Events (20 minutes)

**File:** `GraphQL/Mutations/Mutation.cs`

**Add event publishing to `IncrementTrackViews`:**
```csharp
public async Task<IncrementTrackViewsResponse> IncrementTrackViews(
    string id,
    [Service] ITrackService trackService,
    [Service] ITopicEventSender eventSender)  // ← New!
{
    // ... existing validation and service call ...

    var track = await trackService.IncrementTrackViewsAsync(id);

    // Publish event to subscribers
    await eventSender.SendAsync(
        $"TrackViewCountUpdated_{id}",  // Topic name
        track);                          // Event payload

    // ... return response ...
}
```

**Key concepts:**
- **ITopicEventSender**: Hot Chocolate service for publishing events
- **Topic naming**: Must match subscription's topic pattern
- **Event payload**: The Track object sent to all subscribers

### Step 3: Register Subscription Infrastructure (10 minutes)

**File:** `Program.cs`

**Add subscription support:**
```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddSubscriptionType<Subscription>()  // ← New!
    .RegisterDataLoader<AuthorDataLoader>()
    .RegisterDataLoader<ModuleDataLoader>()
    .AddInMemorySubscriptions();  // ← New! PubSub engine

// Enable WebSocket support
app.UseWebSockets();  // ← New! Must be before MapGraphQL()
app.MapGraphQL();
```

**Key concepts:**
- **AddSubscriptionType**: Registers subscription resolvers
- **AddInMemorySubscriptions**: Enables in-memory PubSub
- **UseWebSockets**: Enables WebSocket protocol (required for subscriptions)

### Step 4: Create Subscription Tests (45 minutes)

**File:** `Catstronauts.Tests/GraphQL/Subscriptions/SubscriptionTests.cs`

**What we'll test:**
1. Subscription resolver exists and has correct signature
2. Subscription returns correct data type
3. Topic attribute is present and correct
4. Event message parameter is present
5. Integration test: Mutation triggers subscription event (complex!)

**Testing challenges:**
- Subscriptions are asynchronous streams (IAsyncEnumerable)
- Need to simulate event publishing
- Integration tests require full GraphQL execution pipeline

**Test example:**
```csharp
[Fact]
public async Task TrackViewCountUpdated_ReturnsTrack()
{
    // Arrange
    var track = new Track { Id = "c_0", NumberOfViews = 101 };
    var subscription = new Subscription();

    // Act
    var result = subscription.TrackViewCountUpdated("c_0", track);

    // Assert
    result.Should().BeEquivalentTo(track);
}
```

### Step 5: Integration Testing (Optional, 30 minutes)

**Test the full flow:**
1. Client subscribes to track "c_0" updates
2. Mutation increments views for track "c_0"
3. Verify subscription receives the update

**This requires:**
- Hot Chocolate's test server
- Subscription execution helpers
- More complex setup, but valuable for production

---

## 📝 Code Structure

```
csharp-server/
├── Catstronauts.GraphQL/
│   ├── GraphQL/
│   │   ├── Queries/
│   │   │   └── Query.cs
│   │   ├── Mutations/
│   │   │   └── Mutation.cs (UPDATED - add event publishing)
│   │   ├── Subscriptions/
│   │   │   └── Subscription.cs (NEW)
│   │   └── DataLoaders/
│   │       ├── AuthorDataLoader.cs
│   │       └── ModuleDataLoader.cs
│   ├── Models/
│   ├── Services/
│   └── Program.cs (UPDATED - add subscriptions + WebSockets)
│
└── Catstronauts.Tests/
    └── GraphQL/
        ├── Queries/
        ├── Mutations/
        ├── DataLoaders/
        └── Subscriptions/
            └── SubscriptionTests.cs (NEW)
```

---

## 🎨 Example GraphQL Operations

### Subscribe to Track Updates

```graphql
subscription WatchTrackViewCount {
  trackViewCountUpdated(trackId: "c_0") {
    id
    title
    numberOfViews
  }
}
```

**WebSocket connection established, client waits for events...**

### Trigger Update via Mutation

```graphql
mutation IncrementViews {
  incrementTrackViews(id: "c_0") {
    code
    success
    message
    track {
      id
      numberOfViews
    }
  }
}
```

**Result:** Mutation completes AND subscription receives event:
```json
{
  "data": {
    "trackViewCountUpdated": {
      "id": "c_0",
      "title": "Catstronauts",
      "numberOfViews": 101
    }
  }
}
```

### Client Code Example (JavaScript)

```javascript
// Using @apollo/client
import { gql, useSubscription } from '@apollo/client';

const TRACK_VIEW_COUNT_SUBSCRIPTION = gql`
  subscription OnTrackViewCountUpdated($trackId: String!) {
    trackViewCountUpdated(trackId: $trackId) {
      id
      numberOfViews
    }
  }
`;

function TrackViewCounter({ trackId }) {
  const { data, loading } = useSubscription(
    TRACK_VIEW_COUNT_SUBSCRIPTION,
    { variables: { trackId } }
  );

  if (loading) return <p>Connecting...</p>;

  return (
    <div>
      <h3>Track {trackId}</h3>
      <p>Views: {data.trackViewCountUpdated.numberOfViews}</p>
      {/* Updates automatically when mutation is called! */}
    </div>
  );
}
```

---

## 🔍 Testing Strategy

### Unit Tests (SubscriptionTests.cs)

**1. Resolver Method Tests:**
- Verify subscription method exists
- Correct return type (Track)
- Correct parameters (trackId, Track)
- [Subscribe] attribute present
- [Topic] attribute present and formatted correctly
- [EventMessage] attribute on correct parameter

**2. Topic Pattern Tests:**
- Dynamic topic generation works
- Different track IDs create different topics
- Topic naming follows convention

**3. Data Flow Tests:**
- Event message parameter correctly receives track
- Returned track matches event message

### Integration Tests (Optional)

**Full Subscription Flow:**
```csharp
[Fact]
public async Task Mutation_TriggersSubscription_SubscribersReceiveUpdate()
{
    // Arrange - Set up test server with subscriptions
    var server = CreateTestServer();

    // Act - Client 1 subscribes
    var subscription = await server.ExecuteSubscriptionAsync(
        "subscription { trackViewCountUpdated(trackId: \"c_0\") { id numberOfViews } }");

    // Act - Client 2 calls mutation
    await server.ExecuteMutationAsync(
        "mutation { incrementTrackViews(id: \"c_0\") { success } }");

    // Assert - Client 1 receives event
    var result = await subscription.ReadNextAsync();
    result.Data["trackViewCountUpdated"]["numberOfViews"].Should().Be(101);
}
```

---

## 🚀 Advanced Concepts (Optional)

### 1. Subscription Filtering

**Problem:** Send only relevant events to each subscriber

```csharp
[Subscribe]
[Topic("TrackUpdated")]
public Track TrackUpdated(
    string trackId,
    [EventMessage] Track track)
{
    // Only send events for the requested track ID
    if (track.Id != trackId)
        return null;  // Filter out

    return track;
}
```

### 2. Subscription with DataLoaders

**Problem:** Subscription returns Track, but client wants Author too

```csharp
[Subscribe]
[Topic("TrackViewCountUpdated_{trackId}")]
public async Task<TrackWithAuthor> TrackViewCountUpdated(
    string trackId,
    [EventMessage] Track track,
    AuthorDataLoader authorLoader)
{
    var author = await authorLoader.LoadAsync(track.AuthorId);
    return new TrackWithAuthor { Track = track, Author = author };
}
```

### 3. Authentication & Authorization

**Problem:** Only authenticated users should receive certain events

```csharp
[Subscribe]
[Authorize]  // Require authentication
[Topic("TrackViewCountUpdated_{trackId}")]
public Track TrackViewCountUpdated(
    string trackId,
    [EventMessage] Track track)
{
    return track;
}
```

### 4. Connection Lifecycle

**Problem:** Know when clients connect/disconnect

```csharp
public class Subscription
{
    [Subscribe(With = nameof(SubscribeToTrackUpdates))]
    public Track TrackViewCountUpdated(
        string trackId,
        [EventMessage] Track track)
    {
        return track;
    }

    // Called when client subscribes
    public async ValueTask<ISourceStream<Track>> SubscribeToTrackUpdates(
        string trackId,
        [Service] ITopicEventReceiver receiver)
    {
        // Log subscription start
        Console.WriteLine($"Client subscribed to track {trackId}");

        return await receiver.SubscribeAsync<Track>(
            $"TrackViewCountUpdated_{trackId}");
    }
}
```

---

## 📊 Performance & Scalability Considerations

### Connection Limits

**WebSocket connections are long-lived:**
- Each subscriber maintains open connection
- Server resources (memory, file descriptors) consumed per connection
- Typical limits: 10,000-65,000 concurrent connections per server

**Scalability strategies:**
- Use external PubSub (Redis, RabbitMQ) for multi-server deployments
- Implement connection pooling and limits
- Use sticky sessions (load balancer routes same client to same server)
- Consider specialized services (Pusher, Ably) for massive scale

### Memory Management

**In-memory subscriptions:**
- Events stored in memory until delivered
- Subscribers buffer events if processing slowly
- Implement backpressure handling

**Best practices:**
- Set max buffer size per subscriber
- Drop old events if buffer full
- Monitor memory usage

### Event Fanout

**One event → Many subscribers:**
```
1 mutation → Publish event
            ↓
        10,000 subscribed clients
            ↓
        10,000 messages sent
```

**Optimization:**
- Batch event delivery where possible
- Use efficient serialization (Protocol Buffers vs JSON)
- Consider message compression

---

## 🎯 Interview Questions & Answers

### Q1: How do GraphQL subscriptions differ from polling?

**Polling:**
```javascript
// Client polls every 5 seconds
setInterval(() => {
  client.query({ query: GET_TRACK_VIEWS });
}, 5000);
```
- ❌ Wasteful: Most polls return no new data
- ❌ Higher latency: Updates delayed by poll interval
- ❌ More server load: Constant requests even when nothing changed
- ❌ More bandwidth: Full query response each time

**Subscriptions:**
```javascript
// Client subscribes once
client.subscribe({ query: TRACK_VIEW_COUNT_SUBSCRIPTION });
```
- ✅ Efficient: Updates only when data changes
- ✅ Low latency: Immediate updates (milliseconds)
- ✅ Less server load: One connection, events only on changes
- ✅ Less bandwidth: Only changed data sent

**When to use polling:**
- Very infrequent updates (once per hour)
- Simple caching scenarios
- Can't use WebSockets (some corporate firewalls block)

### Q2: What's the difference between ITopicEventSender and ITopicEventReceiver?

**ITopicEventSender (Publisher - Mutations):**
```csharp
await eventSender.SendAsync("Topic", payload);
```
- Used to **publish** events to a topic
- Called by mutations when data changes
- Sends events to all subscribers on that topic

**ITopicEventReceiver (Subscriber - Subscription resolvers):**
```csharp
await receiver.SubscribeAsync<Track>("Topic");
```
- Used to **subscribe** to a topic
- Receives events from publishers
- Returns stream of events to GraphQL subscription

**Analogy:**
- Sender = Radio station transmitting signal
- Receiver = Radio tuning into station frequency

### Q3: Can a subscription call a service or DataLoader?

**Yes! Subscriptions can have complex logic:**

```csharp
[Subscribe]
[Topic("TrackUpdated_{trackId}")]
public async Task<TrackWithDetails> TrackUpdated(
    string trackId,
    [EventMessage] Track track,
    [Service] ITrackService service,
    AuthorDataLoader authorLoader,
    ModuleDataLoader moduleLoader)
{
    // Enrich the event with additional data
    var author = await authorLoader.LoadAsync(track.AuthorId);
    var modules = await moduleLoader.LoadAsync(track.Id);

    return new TrackWithDetails
    {
        Track = track,
        Author = author,
        Modules = modules
    };
}
```

**Benefit:** Subscribers get enriched data without extra queries!

### Q4: How do you handle subscription errors?

**Errors can occur at different stages:**

**1. Subscription Start Error:**
```csharp
[Subscribe]
public ValueTask<ISourceStream<Track>> SubscribeToTrack(
    string trackId,
    [Service] ITopicEventReceiver receiver)
{
    if (string.IsNullOrEmpty(trackId))
        throw new GraphQLException("Track ID is required");

    return receiver.SubscribeAsync<Track>($"Track_{trackId}");
}
```
- Error sent to client immediately
- Subscription not established

**2. Event Processing Error:**
```csharp
[Subscribe]
[Topic("Track_{trackId}")]
public Track OnTrackUpdate(
    string trackId,
    [EventMessage] Track track)
{
    try
    {
        // Process event
        return track;
    }
    catch (Exception ex)
    {
        // Log error, return null to skip this event
        logger.LogError(ex, "Error processing track update");
        return null;
    }
}
```
- Skip problematic event, continue subscription
- OR throw to send error to client

**3. Connection Error:**
- WebSocket disconnected
- Hot Chocolate automatically cleans up subscription
- Client should implement reconnection logic

---

## 📚 Resources & Further Reading

### Hot Chocolate Documentation
- [Subscriptions Overview](https://chillicream.com/docs/hotchocolate/v13/defining-a-schema/subscriptions)
- [PubSub Documentation](https://chillicream.com/docs/hotchocolate/v13/defining-a-schema/subscriptions#pub-sub)
- [Testing Subscriptions](https://chillicream.com/docs/hotchocolate/v13/testing)

### GraphQL Specification
- [GraphQL Subscriptions Spec](https://spec.graphql.org/October2021/#sec-Subscription)
- [WebSocket Subprotocol](https://github.com/enisdenjo/graphql-ws)

### Real-World Examples
- **Chat applications**: Messages, typing indicators
- **Collaboration tools**: Document edits, cursor positions
- **Trading platforms**: Stock price updates
- **Gaming**: Player movements, game state
- **IoT dashboards**: Sensor readings, alerts

---

## ✅ Success Criteria

**After completing this stage, you should be able to:**

1. ✅ Explain what GraphQL subscriptions are and when to use them
2. ✅ Describe the publish-subscribe pattern
3. ✅ Implement a subscription resolver with Hot Chocolate
4. ✅ Publish events from mutations
5. ✅ Configure WebSocket support in ASP.NET Core
6. ✅ Use dynamic topic patterns for filtering
7. ✅ Test subscription resolvers
8. ✅ Discuss scalability considerations
9. ✅ Compare in-memory vs external PubSub systems
10. ✅ Explain subscription lifecycle (connect, events, disconnect)

**Interview-ready skills:**
- Explain subscriptions vs polling trade-offs
- Describe PubSub architecture
- Discuss WebSocket protocol
- Explain subscription scalability challenges
- Describe real-world use cases

---

## 🚦 Next Steps

**After creating the plan:**
1. Review and approve the approach
2. Implement Subscription resolver class
3. Update Mutation to publish events
4. Update Program.cs for WebSocket + subscriptions
5. Create comprehensive tests
6. Test with GraphQL Playground (WebSocket protocol)
7. Document in Docs/Stage-07-Subscriptions.md
8. Commit and push

**Estimated time:** 2-3 hours for complete implementation and testing

---

**Ready to implement real-time GraphQL subscriptions?** 🚀

This will complete the full GraphQL feature set: Queries, Mutations, Subscriptions, and DataLoaders!
