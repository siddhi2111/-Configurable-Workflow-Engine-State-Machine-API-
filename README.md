# -Configurable-Workflow-Engine-State-Machine-API-
#  Workflow Engine API

A lightweight and extensible *workflow engine* built with *ASP.NET Core*. This project allows users to define workflows with states and transitions, instantiate workflows, and execute actions to move through states.

## Quick Start

1. Clone this repository
2. Run the API dotnet run
---

## Features

- Create and retrieve *workflow definitions*
- Create and manage *workflow instances*
- Perform *state transitions* via defined actions
- Full *action history tracking*
- Built-in *Swagger UI* for interactive API testing

---

##  Tech Stack

- ASP.NET Core 7 / Minimal API
- In-memory data store (no database required)
- Swagger (OpenAPI) for documentation

---

##  Workflow Concepts

- *State*: A named stage in a workflow (e.g., Pending, Approved, Rejected)
- *Action*: A transition between states (e.g., Approve, Reject)
- *Workflow Definition*: A template describing states and allowed actions
- *Workflow Instance*: A running workflow with a current state and history

---
