<Ivy-Generated>

# Ivy MCP Integration

## MCP Server Connection

The Ivy MCP server provides the following capabilities: ["tutorial", "lsp-hover", "lsp-definition", "lsp-completion", "docs", "feedback", "build"]

You should always use Ivy MCP tools when developing Ivy applications.
Project has been initiated with `ivy init` command. This created the base project structure.
You are now in the root of the project that you should contribute to.

Apps should be put into the App folder. This ensures the correct dependencies.

When using a component, see it's use cases using the docs tool.
If possible, fetch the definitions for each component using the docs tool.

Apps must compile, test this by running dotnet build.
If not, try to fix this using Ivy MCP Server tools.

### App Type Detection

When working with Ivy applications, determine the app type based on the user's request:

**Dashboard Apps**: Look for keywords like:

- "dashboard", "analytics", "report", "chart", "metrics"
- "statistics", "kpi", "performance", "monitoring", "visualization"
- "business intelligence", "data visualization", "analytics dashboard"
- "sales dashboard", "performance dashboard", "metrics dashboard"

**CRUD Apps**: Look for keywords like:

- "product management", "customer database", "order tracking"
- "inventory system", "user management", "employee database"
- "category management", "supplier tracking", "client database"
- "item management", "record keeping", "data entry"
- Any request mentioning "CRUD", "Create Read Update Delete", "data management"

**Other Apps**: Default to fallback tutorial for:

- Chat applications, AI assistants, bots
- Games, simulations, visualizations
- APIs, services, utilities
- Complex business logic applications

**Tutorial Selection**:

- For Dashboard apps: Use the "dashboard" tutorial topic
- For CRUD apps: Use the "crud" tutorial topic
- For other apps: Use the "fallback" tutorial topic (default)

When the user asks for help or tutorials, automatically detect the app type and suggest the appropriate tutorial.

### Server Command

```bash
ivy-local mcp
```

For local development, use `ivy-local`. For production, use `ivy`.

## Version Information

- **Generated**: 2025-10-31 19:02:03
- **Ivy Version**: 1.0.130.0

</Ivy-Generated>
