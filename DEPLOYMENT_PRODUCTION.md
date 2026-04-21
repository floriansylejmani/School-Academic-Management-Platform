# Production Deployment Guide

## Overview

This guide provides comprehensive instructions for deploying the School Management System in production environments. The system is designed for scalability, security, and ease of maintenance.

---

## Deployment Options

### 1. Docker Compose (Recommended for Small-Medium Schools)
- **Best for**: Single-server deployments, quick setup
- **Requirements**: Docker & Docker Compose
- **Time to deploy**: 10-15 minutes

### 2. Kubernetes (Recommended for Large Institutions)
- **Best for**: Multi-server deployments, high availability
- **Requirements**: Kubernetes cluster
- **Time to deploy**: 30-45 minutes

### 3. Cloud Services (AWS, Azure, GCP)
- **Best for**: Managed infrastructure, auto-scaling
- **Requirements**: Cloud account
- **Time to deploy**: 20-30 minutes

---

## Quick Production Setup (Docker Compose)

### Prerequisites
- Docker 24+ with Compose plugin
- 2+ CPU cores, 4GB+ RAM
- SSL certificate (recommended)
- Domain name (optional but recommended)

### Step 1: Prepare Environment
```bash
# Clone the repository
git clone <repository-url>
cd school-management-system

# Copy and configure environment
cp .env.example .env.production
```

### Step 2: Configure Production Settings
Edit `.env.production` with your production values:

```bash
# Security (CRITICAL - Change These)
JWT_SECRET_KEY=$(openssl rand -base64 64)
POSTGRES_PASSWORD=$(openssl rand -base64 32)

# Domain Configuration
FRONTEND_ORIGIN=https://your-school.com
FRONTEND_ORIGIN_ALT=https://www.your-school.com
NEXT_PUBLIC_API_URL=https://api.your-school.com/api
PASSWORD_RESET_FRONTEND_URL=https://your-school.com/reset-password

# Database Configuration
POSTGRES_DB=school_management_prod
POSTGRES_USER=school_admin

# Production Settings
DATABASE_AUTO_MIGRATE=false  # Set to true only for initial deployment
DATABASE_SEED_DEMO_DATA=false  # Never enable in production
ASPNETCORE_ENVIRONMENT=Production

# Ports (behind reverse proxy)
API_PORT=8080
FRONTEND_PORT=3000
POSTGRES_PORT=5432  # Remove port mapping in production

# SSL Configuration
SSL_CERT_PATH=/etc/ssl/certs/your-school.com.crt
SSL_KEY_PATH=/etc/ssl/private/your-school.com.key
```

### Step 3: Create Production Docker Compose
```bash
# Create production compose file
cp docker-compose.yml docker-compose.prod.yml
```

Edit `docker-compose.prod.yml` for production:

```yaml
version: '3.8'

services:
  nginx:
    image: nginx:alpine
    container_name: school_nginx
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
      - ${SSL_CERT_PATH}:/etc/ssl/certs/your-school.com.crt
      - ${SSL_KEY_PATH}:/etc/ssl/private/your-school.com.key
    depends_on:
      - frontend
      - api
    restart: unless-stopped

  frontend:
    container_name: school_frontend
    build:
      context: ./frontend
      args:
        NEXT_PUBLIC_API_URL: ${NEXT_PUBLIC_API_URL}
    depends_on:
      api:
        condition: service_healthy
    environment:
      NODE_ENV: production
      NEXT_PUBLIC_API_URL: ${NEXT_PUBLIC_API_URL}
    restart: unless-stopped
    # Remove ports mapping - served through nginx

  api:
    container_name: school_api
    build:
      context: .
      dockerfile: src/SchoolManagement.API/Dockerfile
    depends_on:
      db:
        condition: service_healthy
    environment:
      ASPNETCORE_ENVIRONMENT: ${ASPNETCORE_ENVIRONMENT}
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__DefaultConnection: Host=db;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}
      Database__AutoMigrate: ${DATABASE_AUTO_MIGRATE}
      Database__SeedDemoData: ${DATABASE_SEED_DEMO_DATA}
      AllowedOrigins__0: ${FRONTEND_ORIGIN}
      AllowedOrigins__1: ${FRONTEND_ORIGIN_ALT}
      Jwt__Issuer: ${JWT_ISSUER}
      Jwt__Audience: ${JWT_AUDIENCE}
      Jwt__SecretKey: ${JWT_SECRET_KEY}
      # ... other configurations
    healthcheck:
      test: ["CMD-SHELL", "curl -f --silent --max-time 3 http://localhost:8080/health || exit 1"]
      interval: 30s
      timeout: 10s
      retries: 3
    volumes:
      - api_data_protection:/home/app/.aspnet/DataProtection-Keys
      - api_logs:/app/logs
    restart: unless-stopped
    # Remove ports mapping - served through nginx

  db:
    image: postgres:16
    container_name: school_db
    restart: unless-stopped
    environment:
      POSTGRES_DB: ${POSTGRES_DB}
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./backups:/backups
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER} -d ${POSTGRES_DB}"]
      interval: 10s
      timeout: 5s
      retries: 5
    # Remove ports mapping - internal only

volumes:
  postgres_data:
  api_data_protection:
  api_logs:
```

### Step 4: Create Nginx Configuration
Create `nginx.conf`:

```nginx
events {
    worker_connections 1024;
}

http {
    upstream api {
        server api:8080;
    }

    upstream frontend {
        server frontend:3000;
    }

    # HTTP to HTTPS redirect
    server {
        listen 80;
        server_name your-school.com www.your-school.com;
        return 301 https://$server_name$request_uri;
    }

    # HTTPS server
    server {
        listen 443 ssl http2;
        server_name your-school.com www.your-school.com;

        ssl_certificate /etc/ssl/certs/your-school.com.crt;
        ssl_certificate_key /etc/ssl/private/your-school.com.key;
        ssl_protocols TLSv1.2 TLSv1.3;
        ssl_ciphers ECDHE-RSA-AES256-GCM-SHA512:DHE-RSA-AES256-GCM-SHA512:ECDHE-RSA-AES256-GCM-SHA384:DHE-RSA-AES256-GCM-SHA384;
        ssl_prefer_server_ciphers off;

        # Security headers
        add_header X-Frame-Options DENY;
        add_header X-Content-Type-Options nosniff;
        add_header X-XSS-Protection "1; mode=block";
        add_header Strict-Transport-Security "max-age=63072000; includeSubDomains; preload";

        # Frontend
        location / {
            proxy_pass http://frontend;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
        }

        # API
        location /api/ {
            proxy_pass http://api;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
            
            # WebSocket support for SignalR
            proxy_http_version 1.1;
            proxy_set_header Upgrade $http_upgrade;
            proxy_set_header Connection "upgrade";
        }

        # Health check
        location /health {
            proxy_pass http://api/health;
            access_log off;
        }
    }
}
```

### Step 5: Deploy
```bash
# Initial deployment with migrations
export DATABASE_AUTO_MIGRATE=true
docker compose -f docker-compose.prod.yml up --build -d

# Wait for deployment to complete
sleep 30

# Verify health
curl -k https://your-school.com/health

# Disable migrations for subsequent deployments
export DATABASE_AUTO_MIGRATE=false
```

---

## Kubernetes Deployment

### 1. Create Namespace
```bash
kubectl create namespace school-management
```

### 2. Create Secrets
```bash
# Database secret
kubectl create secret generic db-secret \
  --from-literal=postgres-user=school_admin \
  --from-literal=postgres-password=$(openssl rand -base64 32) \
  --namespace school-management

# JWT secret
kubectl create secret generic jwt-secret \
  --from-literal=jwt-secret=$(openssl rand -base64 64) \
  --namespace school-management
```

### 3. Deploy Database
```yaml
# postgres-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: postgres
  namespace: school-management
spec:
  replicas: 1
  selector:
    matchLabels:
      app: postgres
  template:
    metadata:
      labels:
        app: postgres
    spec:
      containers:
      - name: postgres
        image: postgres:16
        env:
        - name: POSTGRES_DB
          value: school_management
        - name: POSTGRES_USER
          valueFrom:
            secretKeyRef:
              name: db-secret
              key: postgres-user
        - name: POSTGRES_PASSWORD
          valueFrom:
            secretKeyRef:
              name: db-secret
              key: postgres-password
        ports:
        - containerPort: 5432
        volumeMounts:
        - name: postgres-storage
          mountPath: /var/lib/postgresql/data
      volumes:
      - name: postgres-storage
        persistentVolumeClaim:
          claimName: postgres-pvc
---
apiVersion: v1
kind: Service
metadata:
  name: postgres-service
  namespace: school-management
spec:
  selector:
    app: postgres
  ports:
  - port: 5432
    targetPort: 5432
---
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: postgres-pvc
  namespace: school-management
spec:
  accessModes:
  - ReadWriteOnce
  resources:
    requests:
      storage: 20Gi
```

### 4. Deploy Application
```yaml
# api-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: school-api
  namespace: school-management
spec:
  replicas: 3
  selector:
    matchLabels:
      app: school-api
  template:
    metadata:
      labels:
        app: school-api
    spec:
      containers:
      - name: api
        image: your-registry/school-api:latest
        ports:
        - containerPort: 8080
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__DefaultConnection
          value: "Host=postgres-service;Port=5432;Database=school_management;Username=$(POSTGRES_USER);Password=$(POSTGRES_PASSWORD)"
        envFrom:
        - secretRef:
            name: db-secret
        - secretRef:
            name: jwt-secret
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5
---
apiVersion: v1
kind: Service
metadata:
  name: school-api-service
  namespace: school-management
spec:
  selector:
    app: school-api
  ports:
  - port: 80
    targetPort: 8080
  type: LoadBalancer
```

---

## Cloud Deployment Guides

### AWS ECS Deployment
```bash
# Create ECR repository
aws ecr create-repository --repository-name school-management-api

# Build and push image
docker build -t school-management-api .
aws ecr get-login-password | docker login --username AWS --password-stdin <account-id>.dkr.ecr.<region>.amazonaws.com
docker tag school-management-api:latest <account-id>.dkr.ecr.<region>.amazonaws.com/school-management-api:latest
docker push <account-id>.dkr.ecr.<region>.amazonaws.com/school-management-api:latest

# Deploy with ECS CLI
ecs-cli compose --project-name school-management up --create-log-groups
```

### Azure Container Instances
```bash
# Create resource group
az group create --name school-management --location eastus

# Deploy container group
az container create \
  --resource-group school-management \
  --name school-api \
  --image your-registry/school-api:latest \
  --dns-name-label school-api-unique \
  --ports 80 443 \
  --environment-variables \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__DefaultConnection="Server=<db-server>;Database=school_management;User Id=<user>;Password=<password>"
```

---

## Monitoring & Maintenance

### Health Monitoring
```bash
# Check container health
docker compose ps

# View logs
docker compose logs -f api
docker compose logs -f frontend

# Health check endpoint
curl -k https://your-school.com/health
```

### Backup Strategy
```bash
# Database backup script
#!/bin/bash
BACKUP_DIR="/backups"
DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="$BACKUP_DIR/school_management_$DATE.sql"

# Create backup
docker compose exec -T db pg_dump -U school_admin school_management > $BACKUP_FILE

# Compress backup
gzip $BACKUP_FILE

# Remove old backups (keep 30 days)
find $BACKUP_DIR -name "*.sql.gz" -mtime +30 -delete

echo "Backup completed: $BACKUP_FILE.gz"
```

### Update Process
```bash
# Update application
git pull
docker compose -f docker-compose.prod.yml build
docker compose -f docker-compose.prod.yml up -d

# Verify update
curl -k https://your-school.com/health
```

---

## Security Checklist

### Pre-Deployment Security
- [ ] Change all default passwords and secrets
- [ ] Configure SSL certificates
- [ ] Set up firewall rules
- [ ] Enable security headers
- [ ] Configure rate limiting
- [ ] Set up monitoring and alerting

### Post-Deployment Security
- [ ] Verify HTTPS enforcement
- [ ] Test authentication flows
- [ ] Check security headers
- [ ] Validate rate limiting
- [ ] Review audit logs
- [ ] Perform security scan

---

## Troubleshooting

### Common Issues

#### Database Connection Failed
```bash
# Check database health
docker compose exec db pg_isready -U school_admin

# View database logs
docker compose logs db

# Restart database
docker compose restart db
```

#### API Not Responding
```bash
# Check API health
curl -k https://your-school.com/health

# View API logs
docker compose logs api

# Restart API
docker compose restart api
```

#### Frontend Not Loading
```bash
# Check frontend logs
docker compose logs frontend

# Rebuild frontend
docker compose build --no-cache frontend
docker compose up -d frontend
```

### Performance Issues
```bash
# Monitor resource usage
docker stats

# Check database performance
docker compose exec db psql -U school_admin -d school_management -c "
SELECT query, calls, total_time, mean_time 
FROM pg_stat_statements 
ORDER BY total_time DESC 
LIMIT 10;"
```

---

## Support & Maintenance

### Regular Maintenance Tasks
- **Daily**: Check health status and monitor logs
- **Weekly**: Review security logs and update dependencies
- **Monthly**: Database maintenance and backup verification
- **Quarterly**: Security audit and performance review

### Emergency Procedures
1. **System Down**: Check health endpoints and restart services
2. **Database Issue**: Verify database connectivity and check logs
3. **Security Incident**: Review audit logs and implement containment
4. **Performance Issue**: Monitor resources and check database queries

### Contact Support
- **Documentation**: Review this guide and API documentation
- **Community**: GitHub issues and discussions
- **Emergency**: System logs and health check results

---

*This deployment guide ensures your School Management System runs securely and efficiently in production environments.*
