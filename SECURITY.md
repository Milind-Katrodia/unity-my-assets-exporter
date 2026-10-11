# Security Policy

## Security & Privacy Philosophy

The **Unity My Assets Exporter** is designed with privacy and safety as first-class priorities:

- **100% Offline & Local Execution**: All package discovery is performed in memory directly within your running Unity Editor session. No external network requests are dispatched to external or third-party servers.
- **Zero Credential / Token Exposure**: The tool does not store, serialize, or transmit passwords, session tokens, or API secrets. Only public asset metadata (names, package IDs, versions, descriptions, and compatibility targets) are exported.
- **Read-Only Operation**: The tool only reads the in-memory data already populated by Unity's Package Manager UI. It makes zero modifications to project assets, scene hierarchies, dependencies, or package manifests.

---

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | :white_check_mark: |

---

## Reporting a Vulnerability

If you discover any security issue or unexpected data exposure:

1. Please do **not** open a public issue on GitHub.
2. Email details to: `milindkatrodia@gmail.com`
3. Include reproduction steps and details of the environment.
4. Reports are acknowledged within 48 hours with an assessment and remediation plan.
