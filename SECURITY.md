# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 1.x.x   | :white_check_mark: |
| < 1.0   | :x:                |

## Reporting a Vulnerability

We take the security of APRS.NET seriously. If you discover a security vulnerability, please report it responsibly.

### How to Report

1. **DO NOT** open a public GitHub issue for security vulnerabilities.

2. **Email** security concerns to the maintainers directly (add your email here before publishing).

3. Include in your report:
   - Description of the vulnerability
   - Steps to reproduce
   - Potential impact
   - Suggested fix (if any)

### What to Expect

- **Acknowledgment**: Within 48 hours of your report
- **Initial Assessment**: Within 1 week
- **Resolution**: Depends on severity, typically 2-4 weeks
- **Public Disclosure**: Coordinated with reporter after fix

## Security Best Practices for Deployment

### Configuration

1. **Never commit secrets** - Use environment variables or secure vaults
2. **Use strong APRS passwords** - Don't use default `-1` in production
3. **Rotate credentials** regularly

### Network Security

1. **Use HTTPS** in production for the API
2. **Firewall** the PostgreSQL and Redis ports
3. **Use rate limiting** (already built-in)

### Container Security

1. Run containers as **non-root** users
2. Use **read-only** file systems where possible
3. Keep base images **updated**
4. Scan images for **vulnerabilities** before deployment

### Database Security

1. Use **strong passwords** for PostgreSQL
2. Enable **SSL/TLS** connections
3. Apply **principle of least privilege** for database users
4. Regular **backups** and backup verification

## Known Security Considerations

### APRS-IS Protocol

- APRS-IS is a cleartext protocol; data in transit is not encrypted
- APRS passcodes are not true authentication (they can be calculated from callsigns)
- Position data transmitted over APRS is inherently public

### Rate Limiting

The API includes rate limiting to prevent abuse:
- Fixed window: 100 requests/minute
- Sliding window: 1000 requests/minute

### Input Validation

- All packet parsing includes regex timeout protection (ReDoS mitigation)
- FluentValidation on all API inputs
- Parameterized queries (EF Core) prevent SQL injection

## Security Updates

Security updates will be released as patch versions and announced via:
- GitHub Security Advisories
- Release notes
- CHANGELOG.md

## Acknowledgments

We appreciate the security research community's efforts in helping us keep APRS.NET secure.
