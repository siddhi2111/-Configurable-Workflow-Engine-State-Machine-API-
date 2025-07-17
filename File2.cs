using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using WorkflowEngine;
using WorkflowEngine.Models;

var builder = WebApplication.CreateBuilder(args);

// Register Swagger services for API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register a singleton instance of the workflowstore to persist data across requests
builder.Services.AddSingleton<workflowstore>();

var app = builder.Build();

// Enable Swagger UI for API testing and documentation
app.UseSwagger();
app.UseSwaggerUI();

/// <summary>
/// Validates a workflow definition before it is added to the store.
/// Ensures unique state IDs, exactly one initial state, and valid transitions.
/// </summary>
bool is_valid_workflowdef(workflowdefinition def, out string error)
{
    error = null;

    // Ensure all state IDs are unique
    var state_ids = new HashSet<string>();
    foreach (var state in def.states)
        if (!state_ids.Add(state.id))
        {
            error = "duplicate state ids";
            return false;
        }

    // Ensure exactly one state is marked as initial
    if (def.states.Count(s => s.isinitial) != 1)
    {
        error = "must have exactly one initial state";
        return false;
    }

    // Ensure all to/from states in actions reference valid state IDs
    var valid_ids = def.states.Select(s => s.id).ToHashSet();
    foreach (var action in def.actions)
    {
        if (!valid_ids.Contains(action.tostate))
        {
            error = $"action {action.id} tostate invalid";
            return false;
        }

        if (!action.fromstates.All(valid_ids.Contains))
        {
            error = $"action {action.id} fromstates invalid";
            return false;
        }
    }

    return true;
}

// --------------------------------------------
//              API ENDPOINTS
// --------------------------------------------

/// 1. Create a new workflow definition
app.MapPost("/workflows", (workflowdefinition def, workflowstore store) =>
{
    // Check for duplicate workflow IDs
    if (store.workflowtemplates.ContainsKey(def.id))
        return Results.BadRequest("workflowdefinition with same id exists");

    // Validate the structure of the workflow definition
    if (!is_valid_workflowdef(def, out var error))
        return Results.BadRequest(error);

    // Store the valid workflow definition
    store.workflowtemplates[def.id] = def;
    return Results.Created($"/workflows/{def.id}", def);
});

/// 2. Retrieve a workflow definition by ID
app.MapGet("/workflows/{id}", (string id, workflowstore store) =>
    store.workflowtemplates.TryGetValue(id, out var def)
        ? Results.Ok(def) // Return definition if found
        : Results.NotFound("workflowdefinition not found") // Otherwise 404
);

/// 3. List all workflow definitions
app.MapGet("/workflows", (workflowstore store) =>
    Results.Ok(store.workflowtemplates.Values)
);

/// 4. Create a new instance of a workflow definition
app.MapPost("/workflows/{id}/instances", (string id, workflowstore store) =>
{
    // Find the workflow definition
    if (!store.workflowtemplates.TryGetValue(id, out var def))
        return Results.NotFound("workflowdefinition not found");

    // Find the initial state that is enabled
    var initial = def.states.FirstOrDefault(s => s.isinitial && s.enabled);
    if (initial == null)
        return Results.BadRequest("no enabled initial state");

    // Create a new instance with a unique ID and set it to the initial state
    var instance = new workflowinstance
    {
        id = Guid.NewGuid().ToString(),
        workflowdefinitionid = id,
        currentstate = initial.id,
    };

    // Store the instance
    store.runningworkflows[instance.id] = instance;
    return Results.Created($"/instances/{instance.id}", instance);
});

/// 5. Get a specific workflow instance and its history
app.MapGet("/instances/{id}", (string id, workflowstore store) =>
    store.runningworkflows.TryGetValue(id, out var inst)
        ? Results.Ok(inst)
        : Results.NotFound("instance not found")
);

/// 6. List all workflow instances
app.MapGet("/instances", (workflowstore store) =>
    Results.Ok(store.runningworkflows.Values)
);

/// 7. Execute a defined action on a workflow instance
app.MapPost("/instances/{instanceid}/actions/{actionid}", (string instanceid, string actionid, workflowstore store) =>
{
    // Validate that the instance exists
    if (!store.runningworkflows.TryGetValue(instanceid, out var inst))
        return Results.NotFound("instance not found");

    // Get the associated workflow definition
    if (!store.workflowtemplates.TryGetValue(inst.workflowdefinitionid, out var def))
        return Results.NotFound("workflowdefinition not found");

    // Get the current state of the instance
    var curr_state = def.states.FirstOrDefault(s => s.id == inst.currentstate);
    if (curr_state == null || !curr_state.enabled)
        return Results.BadRequest("current state is invalid or disabled");

    // Disallow transitions from a final state
    if (curr_state.isfinal)
        return Results.BadRequest("instance is at a final state");

    // Get the action requested
    var action = def.actions.FirstOrDefault(a => a.id == actionid);
    if (action == null)
        return Results.BadRequest("action not found");

    if (!action.enabled)
        return Results.BadRequest("action is disabled");

    if (!action.fromstates.Contains(inst.currentstate))
        return Results.BadRequest("action cannot be executed from current state");

    // Get the destination state and validate it
    var to_state = def.states.FirstOrDefault(s => s.id == action.tostate && s.enabled);
    if (to_state == null)
        return Results.BadRequest("target state is invalid or disabled");

    // Record the action in the instance history
    var history = new actionhistoryitem
    {
        actionid = action.id,
        timestamp = DateTime.UtcNow,
        fromstate = inst.currentstate,
        tostate = to_state.id
    };

    // Update the instance with the new state
    inst.currentstate = to_state.id;
    inst.history.Add(history);

    return Results.Ok(inst);
});

// Start the web application
app.Run();
