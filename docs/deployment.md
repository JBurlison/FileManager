# Deployment Guide

## Network Configuration

The Web File Explorer application enforces strict network binding to explicitly configured `10.0.0.x` networks for security reasons.

### Configuring the Allowed Local IP Prefix

By default, the server binds to interfaces with the `10.` prefix. In production, this must be correctly mapped to your local `10.x.x.x` subnet.

You configure the prefix in your `appsettings.json`, environment variables, or other ASP.NET Core configuration providers:

```json
{
  "NetworkBinding": {
    "AllowedPrefix": "10.0.0.",
    "Port": 5000
  }
}
```

Or via environment variable:
```
NetworkBinding__AllowedPrefix=10.0.0.
```

### Finding the Effective Listening Address

When the application starts, it scans for network interfaces matching the `NetworkBinding:AllowedPrefix` configuration value.

To verify the effective listening address:
1. Start the application.
2. Observe the console output or the application log (e.g., standard out, file logs).
3. Look for the message: `Binding to allowed local address: {IP}:{Port}`.

For example:
```
info: WebFileExplorer.Server.Program[0]
      Binding to allowed local address: 10.0.0.15:5000
```

If no interfaces match the prefix, the application will forcefully terminate during startup with a `InvalidOperationException`:
`FATAL ERROR: No network interfaces matching '10.0.0.' found. The application strictly requires a local 10.0.0.x network. Shutting down.`

### Application Layer Enforcement

Even if an external reverse proxy maps requests to this IP, the application enforces a strict source address check on `Connection.RemoteIpAddress`. Requests with an originating IP address that does not start with the configured prefix will result in an HTTP `403 Forbidden` response and an associated warning log.
