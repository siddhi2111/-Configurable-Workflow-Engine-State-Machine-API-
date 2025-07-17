using System.Collections.Generic;
using WorkflowEngine.Models;

namespace WorkflowEngine
{
    public class workflowstore
    {
        // stores predefined workflow templates with unique identifiers
        public Dictionary<string, workflowdefinition> workflowtemplates { get; } = new();

        // stores active or historical workflow instances with unique identifiers
        public Dictionary<string, workflowinstance> runningworkflows { get; } = new();
    }
}
