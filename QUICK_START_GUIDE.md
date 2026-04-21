# Quick Start Guide - Deploy in 10 Minutes

## Overview

Get your School Management System running in production with this step-by-step guide. Perfect for administrators, developers, and IT professionals.

---

## System Requirements

### Minimum Requirements
- **CPU**: 2 cores
- **RAM**: 4GB
- **Storage**: 20GB
- **OS**: Linux, macOS, or Windows with Docker

### Recommended Requirements
- **CPU**: 4+ cores
- **RAM**: 8GB+
- **Storage**: 50GB+ SSD
- **Network**: Stable internet connection

### Software Prerequisites
- Docker 24+ with Compose plugin
- Git (for cloning repository)
- SSL certificate (for production)

---

## 5-Minute Quick Start (Development)

### Step 1: Get the Code
```bash
git clone <repository-url>
cd school-management-system
```

### Step 2: Configure Environment
```bash
cp .env.example .env
# Edit .env if needed (defaults work for development)
```

### Step 3: Deploy
```bash
docker compose up --build
```

### Step 4: Access
- **Frontend**: http://localhost:3000
- **API**: http://localhost:5000
- **Admin Login**: admin@school.com / Admin@12345

---

## 10-Minute Production Setup

### Pre-Setup Checklist
- [ ] Have domain name ready (optional but recommended)
- [ ] Have SSL certificate (required for production)
- [ ] Server meets minimum requirements
- [ ] Docker installed and running

### Step 1: Prepare Environment
```bash
# Clone repository
git clone <repository-url>
cd school-management-system

# Create production environment file
cp .env.example .env.production
```

### Step 2: Generate Secure Secrets
```bash
# Generate JWT secret (64 characters)
JWT_SECRET=$(openssl rand -base64 64)
echo "JWT_SECRET_KEY=$JWT_SECRET" >> .env.production

# Generate database password
DB_PASSWORD=$(openssl rand -base64 32)
echo "POSTGRES_PASSWORD=$DB_PASSWORD" >> .env.production
```

### Step 3: Configure Production Settings
Edit `.env.production` and set:

```bash
# Domain Configuration
FRONTEND_ORIGIN=https://your-school.com
NEXT_PUBLIC_API_URL=https://api.your-school.com/api
PASSWORD_RESET_FRONTEND_URL=https://your-school.com/reset-password

# Security
JWT_ISSUER=SchoolManagement
JWT_AUDIENCE=SchoolManagement.Client
JWT_ACCESS_TOKEN_EXPIRY_MINUTES=60
JWT_REFRESH_TOKEN_EXPIRY_DAYS=7

# Database
POSTGRES_DB=school_management_prod
POSTGRES_USER=school_admin
POSTGRES_PASSWORD=<generated-password>

# Production Settings
ASPNETCORE_ENVIRONMENT=Production
DATABASE_AUTO_MIGRATE=true
DATABASE_SEED_DEMO_DATA=false

# Security Features
RATE_LIMITING_DEFAULT_LIMIT=100
RATE_LIMITING_AUTH_LIMIT=5
AUDIT_LOG_ALL_EVENTS=true
AUDIT_RETENTION_DAYS=90

# AI Features (optional)
OPENAI_ENABLED=false
```

### Step 4: Deploy with Docker Compose
```bash
# Deploy with migrations
docker compose -f docker-compose.yml --env-file .env.production up --build -d

# Wait for startup (30-60 seconds)
sleep 45

# Verify deployment
curl http://localhost:5000/health
```

### Step 5: Initial Setup
1. Open http://localhost:3000 in browser
2. Login with admin@school.com / Admin@12345
3. Change admin password immediately
4. Configure school settings
5. Create user accounts

---

## Setup Checklist

### Pre-Deployment
- [ ] Server meets requirements
- [ ] Docker installed and running
- [ ] SSL certificate obtained
- [ ] Domain name configured
- [ ] Firewall rules set

### Security Configuration
- [ ] JWT secret generated (64+ characters)
- [ ] Database password set (strong)
- [ ] SSL certificate configured
- [ ] Rate limiting enabled
- [ ] Audit logging enabled
- [ ] Security headers configured

### Application Configuration
- [ ] Environment variables set
- [ ] Database connection tested
- [ ] CORS origins configured
- [ ] Email settings configured
- [ ] File upload limits set

### Post-Deployment
- [ ] Health checks passing
- [ ] SSL certificate working
- [ ] Admin account secured
- [ ] Backup strategy implemented
- [ ] Monitoring configured
- [ ] Documentation reviewed

---

## Configuration Options

### Essential Settings
| Setting | Description | Default | Production |
|---|---|---|---|
| `JWT_SECRET_KEY` | JWT signing key | Dev placeholder | **Must change** |
| `POSTGRES_PASSWORD` | Database password | postgres | **Must change** |
| `FRONTEND_ORIGIN` | Frontend URL | localhost | **Must change** |
| `DATABASE_SEED_DEMO_DATA` | Demo data | true | false |

### Security Settings
| Setting | Description | Recommended |
|---|---|---|
| `RATE_LIMITING_AUTH_LIMIT` | Auth requests per 5min | 5 |
| `AUDIT_RETENTION_DAYS` | Log retention | 90 |
| `JWT_ACCESS_TOKEN_EXPIRY_MINUTES` | Token lifetime | 60 |
| `JWT_REFRESH_TOKEN_EXPIRY_DAYS` | Refresh lifetime | 7 |

### Performance Settings
| Setting | Description | Recommended |
|---|---|---|
| `DATABASE_AUTO_MIGRATE` | Auto migrations | false (after initial) |
| `OPENAI_ENABLED` | AI features | false (unless needed) |
| `ASPNETCORE_ENVIRONMENT` | Environment | Production |

---

## Common Setup Scenarios

### Small School (Single Server)
```bash
# Quick deployment
git clone <repo>
cd school-management-system
cp .env.example .env
# Edit basic settings
docker compose up --build -d
```

### Medium School (With SSL)
```bash
# Production deployment
git clone <repo>
cd school-management-system
cp .env.example .env.production
# Configure SSL and domain
docker compose -f docker-compose.yml --env-file .env.production up --build -d
```

### Large School (Load Balanced)
```bash
# Use Kubernetes deployment
kubectl apply -f k8s/
# Configure ingress controller
# Set up load balancer
```

---

## Troubleshooting Quick Guide

### Common Issues & Solutions

#### Port Already in Use
```bash
# Check what's using the port
sudo netstat -tulpn | grep :3000
# Change port in .env
FRONTEND_PORT=3001
```

#### Database Connection Failed
```bash
# Check database status
docker compose logs db
# Restart database
docker compose restart db
# Verify credentials
docker compose exec db psql -U postgres -d school_management
```

#### Frontend Not Loading
```bash
# Rebuild frontend
docker compose build --no-cache frontend
# Check logs
docker compose logs frontend
# Clear browser cache
```

#### SSL Certificate Issues
```bash
# Verify certificate
openssl s_client -connect your-school.com:443
# Check certificate path
ls -la /etc/ssl/certs/
# Restart nginx if using reverse proxy
```

### Health Check Commands
```bash
# API Health
curl -f http://localhost:5000/health

# Database Health
docker compose exec db pg_isready -U postgres

# Frontend Health
curl -f http://localhost:3000

# Container Status
docker compose ps
```

---

## Next Steps After Setup

### 1. Secure Your System
- Change default admin password
- Configure user roles and permissions
- Set up backup schedule
- Enable monitoring and alerts

### 2. Configure School Data
- Add school information
- Create academic year structure
- Set up classes and subjects
- Import student data

### 3. Customize System
- Configure school branding
- Set up notification preferences
- Configure fee structures
- Customize reports

### 4. Train Users
- Create user accounts
- Provide training materials
- Set up user guides
- Conduct training sessions

---

## Support Resources

### Documentation
- [Full API Documentation](API_CONTRACT.md)
- [Production Deployment Guide](DEPLOYMENT_PRODUCTION.md)
- [Backend Architecture](BACKEND.md)
- [Frontend Guide](FRONTEND.md)

### Community Support
- GitHub Issues: Report bugs and request features
- Documentation: Comprehensive guides and tutorials
- Community Forum: User discussions and best practices

### Emergency Contacts
- System Status: Check health endpoints
- Log Analysis: Review application logs
- Database Issues: Check database connectivity

---

## Performance Tips

### Database Optimization
- Regular vacuum and analyze
- Monitor query performance
- Set up connection pooling
- Configure read replicas for large deployments

### Application Optimization
- Enable response caching
- Optimize image sizes
- Use CDN for static assets
- Monitor memory usage

### Infrastructure Optimization
- Use SSD storage
- Configure proper resource limits
- Set up horizontal scaling
- Monitor system metrics

---

## Security Best Practices

### Network Security
- Use HTTPS everywhere
- Configure firewall rules
- Implement rate limiting
- Monitor for attacks

### Application Security
- Keep dependencies updated
- Use strong passwords
- Enable audit logging
- Regular security scans

### Data Security
- Encrypt sensitive data
- Regular backups
- Access control
- Data retention policies

---

*This guide helps you deploy the School Management System quickly and securely. For detailed information, refer to the comprehensive documentation.*
