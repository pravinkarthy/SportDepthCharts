# Depth Chart Management Console Application

This console application manages NFL and MLB depth charts using RabbitMQ for message processing. It supports adding players, modifying depth chart positions, removing players, and querying the chart, with messages sent to RabbitMQ queues.

## Prerequisites

- **RabbitMQ**: Running locally or accessible (default: `localhost:5672`, `guest/guest`).
- **.NET**: Required to build and run the project.
- **Dependencies**: `MediatR`, `Microsoft.Extensions.DependencyInjection`, `Newtonsoft.Json`, `RabbitMQ.Client`.

## Configuration

- **`appsettings.json`**:
  ```json
  {
    "RabbitMQ": {
      "Host": "localhost",
      "Username": "guest",
      "Password": "guest"
    }
  }
  ```
  Place this file in the project directory to configure RabbitMQ connection settings.

## Running the Application

1. Build the project: `dotnet build`.
2. Run: `dotnet run`.
3. The app starts listeners for `nfl_depth_chart_queue` (NFL) and `mlb_depth_chart_queue` (MLB).
4. Enter JSON messages in the console to send to `nfl_depth_chart_queue` (e.g., `{"type":"add_player","playerId":1,"name":"Bob"}`). Type `exit` to quit.

## Message Types

The application processes four `"type"` values via a switch-case in `RabbitMqReceiver<TPosition>.ProcessMessageAsync`. Messages are camelCase JSON sent to RabbitMQ queues.

### 1. `"add_player"`

Adds a player to the system with a unique ID and name.

- **Sample Input**:
  ```json
  {"type":"add_player","playerId":1,"name":"Bob"}
  ```
- **Sent**:
  ```
  Sent to nfl_depth_chart_queue: {"type":"add_player","playerId":1,"name":"Bob"}
  ```
- **Effect**: Registers player "Bob" with ID 1. No console output by default.

### 2. `"add"`

Adds or updates a player’s position in the depth chart.

- **Sample Inputs**:
  ```json
  {"type":"add","name":"Bob","position":"WR","depth":0}
  ```
  ```
  Sent to nfl_depth_chart_queue: {"type":"add","playerId":1,"name":"Bob","position":"WR","depth":0}
  > Added Player Bob with position 'WR' to depth: 0
  ```
  ```json
  {"type":"add","name":"Charlie","position":"WR","depth":2}
  ```
  ```
  Sent to nfl_depth_chart_queue: {"type":"add","playerId":2,"name":"Charlie","position":"WR","depth":2}
  > Added Player Charlie with position 'WR' to depth: 2
  ```
  ```json
  {"type":"add","name":"Alice","position":"WR","depth":0}
  ```
  ```
  Sent to nfl_depth_chart_queue: {"type":"add","playerId":3,"name":"Alice","position":"WR","depth":0}
  > Added Player Alice with position 'WR' to depth: 0
  ```
  ```json
  {"type":"add","name":"Bob","position":"KR"}
  ```
  ```
  Sent to nfl_depth_chart_queue: {"type":"add","playerId":1,"name":"Bob","position":"KR"}
  > Added Player Bob with position 'KR' to depth:
  ```
- **Notes**:
  - `"depth"` is optional; if omitted, the player is added without a specific depth.
  - `"playerId"` and `"name"` must match an existing player or register a new one.

### 3. `"remove"`

Removes a player from a specific position in the depth chart.

- **Sample Input**:
  ```json
  {"type":"remove","name":"Bob","position":"WR"}
  ```
- **Sent**:
  ```
  Sent to nfl_depth_chart_queue: {"type":"remove","name":"Bob","position":"WR"}
  ```
- **Effect**: Removes "Bob" from the WR position. No console output by default.

### 4. `"get_full"`

Displays the full depth chart for all positions.

- **Sample Input**:
  ```json
  {"type":"get_full"}
  ```
- **Sent**:
  ```
  Sent to nfl_depth_chart_queue: {"type":"get_full"}
  > Depth Chart:
  [{"Position":2,"Players":["Alice","Bob","Charlie"]},{"Position":8,"Players":["Bob"]}]
  ```
- **Output**: JSON array of positions and players, e.g., WR (enum value 2) and KR (enum value 8).

### 5. `"get_under"`

Lists players below a specified player in a position.

- **Sample Input**:
  ```json
  {"type":"get_under","name":"Alice","position":"WR"}
  ```
- **Sent**:
  ```
  Sent to nfl_depth_chart_queue: {"type":"get_under","name":"Alice","position":"WR"}
  > ["Bob","Charlie"]
  ```
- **Output**: JSON array of player names below "Alice" in WR (depths 1 and 2).

## RabbitMQ Service

- **Default Queue**: `nfl_depth_chart_queue` for NFL messages; `mlb_depth_chart_queue` for MLB.
- **Service**: `RabbitMqReceiver<TPosition>` listens to queues, processes messages, and uses MediatR to dispatch commands/queries to `DepthChartService<TPosition>`.

## Enums

- **`NflPosition`**:
  ```csharp
  public enum NflPosition { QB = 0, WR = 2, KR = 8 /* etc. */ }
  ```
  Maps position strings (e.g., `"WR"`) to enum values for NFL depth charts.
- **`MlbPosition`**:
  ```csharp
  public enum MlbPosition { P = 0, C = 1 /* etc. */ }
  ```
  Maps position strings for MLB depth charts.

## Notes

- **Error Handling**: Invalid JSON or missing `"type"` results in an error message to the console (e.g., `"Error processing message: ..."`).
- **Depth Chart Logic**: Players are ordered by `"depth"`; lower values are higher in the chart.
- **Testing**: Unit tests in `DepthChartTests` verify all message types using real MediatR and service implementations.
