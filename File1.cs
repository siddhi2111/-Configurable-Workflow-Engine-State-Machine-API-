using System;
using System.Collections.Generic;

namespace WorkflowEngine.Models
{
    public class state
    {
        public string id { get; set; }
        public string name { get; set; }
        public bool isinitial { get; set; }
        public bool isfinal { get; set; }
        public bool enabled { get; set; } = true;
        public string description { get; set; }
    }

    public class actiondef
    {
        public string id { get; set; }
        public string name { get; set; }
        public bool enabled { get; set; } = true;
        public List<string> fromstates { get; set; } = new();
        public string tostate { get; set; }
        public string description { get; set; }
    }

    public class workflowdefinition
    {
        public string id { get; set; }
        public string name { get; set; }
        public List<state> states { get; set; } = new();
        public List<actiondef> actions { get; set; } = new();
    }

    public class actionhistoryitem
    {
        public string actionid { get; set; }
        public DateTime timestamp { get; set; }
        public string fromstate { get; set; }
        public string tostate { get; set; }
    }

    public class workflowinstance
    {
        public string id { get; set; }
        public string workflowdefinitionid { get; set; }
        public string currentstate { get; set; }
        public List<actionhistoryitem> history { get; set; } = new();
    }
}
