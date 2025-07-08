# Orchestration and Observability Implementation TODO

## Current Status
Basic Docker Compose setup with MySQL, Redis, and API. YARP reverse proxy for load balancing. No observability or monitoring tools implemented.

## Implementation Strategy: Manual Solution (Avoiding .NET Aspire)

### Phase 1: Observability Foundation

#### OpenTelemetry Implementation
- [ ] Add OpenTelemetry packages to `ToolsSharing.API.csproj`:
  - [ ] `OpenTelemetry.Extensions.Hosting`
  - [ ] `OpenTelemetry.Instrumentation.AspNetCore`
  - [ ] `OpenTelemetry.Instrumentation.EntityFrameworkCore`
  - [ ] `OpenTelemetry.Instrumentation.Http`
  - [ ] `OpenTelemetry.Exporter.Jaeger`

#### Distributed Tracing Setup
- [ ] Configure OpenTelemetry in `Program.cs`
- [ ] Add custom activity sources for business operations
- [ ] Implement correlation IDs across service calls
- [ ] Add tracing to database operations and external API calls

#### Jaeger Integration
- [ ] Add Jaeger service to `docker-compose.yml`
- [ ] Configure Jaeger exporter in application
- [ ] Set up trace sampling policies
- [ ] Create custom trace spans for important operations

### Phase 2: Metrics and Monitoring

#### Prometheus Metrics
- [ ] Add Prometheus packages:
  - [ ] `prometheus-net.AspNetCore`
  - [ ] `OpenTelemetry.Exporter.Prometheus.AspNetCore`
- [ ] Configure custom metrics for business operations:
  - [ ] Tool creation/update/delete counters
  - [ ] User registration/login metrics
  - [ ] Rental request/approval rates
  - [ ] API response time histograms
  - [ ] Database connection pool metrics

#### Grafana Dashboards
- [ ] Add Grafana service to Docker Compose
- [ ] Create dashboard templates for:
  - [ ] API performance metrics (response times, error rates)
  - [ ] Business metrics (active users, tool listings, rentals)
  - [ ] Infrastructure metrics (database, Redis, container health)
  - [ ] Real-time service status overview

#### Container Monitoring
- [ ] Add Portainer for container management UI
- [ ] Configure container health checks
- [ ] Set up resource usage monitoring
- [ ] Add restart policies and failure notifications

### Phase 3: Enhanced Monitoring

#### Structured Logging Enhancement
- [ ] Enhance Serilog configuration with:
  - [ ] Correlation ID enrichment
  - [ ] Structured property logging
  - [ ] Log level configuration per service
  - [ ] JSON output formatting for log aggregation

#### ELK Stack (Optional)
- [ ] Add Elasticsearch service to Docker Compose
- [ ] Configure Logstash for log processing
- [ ] Set up Kibana for log visualization
- [ ] Create log dashboards and alerts

#### Health Checks Enhancement
- [ ] Expand existing health checks to include:
  - [ ] Redis connectivity
  - [ ] External service dependencies
  - [ ] Database migration status
  - [ ] Custom business health indicators
- [ ] Create health check aggregation dashboard
- [ ] Set up health check endpoints for load balancer

### Phase 4: Advanced Observability

#### Application Performance Monitoring
- [ ] Add custom performance counters
- [ ] Implement request/response logging middleware
- [ ] Set up performance threshold alerts
- [ ] Create performance regression detection

#### Error Tracking and Alerting
- [ ] Enhance exception handling with structured error data
- [ ] Set up error rate alerts in Grafana
- [ ] Implement error notification system
- [ ] Create error trend analysis dashboards

#### Security Monitoring
- [ ] Add authentication/authorization metrics
- [ ] Monitor failed login attempts
- [ ] Track API key usage patterns
- [ ] Set up security alert notifications

## Docker Compose Services to Add

```yaml
# Services to add to existing docker-compose.yml

# Observability Stack
jaeger:
  image: jaegertracing/all-in-one:latest
  ports:
    - "16686:16686"    # Jaeger UI
    - "14268:14268"    # HTTP collector

prometheus:
  image: prom/prometheus:latest
  ports:
    - "9090:9090"      # Prometheus UI
  volumes:
    - ./monitoring/prometheus.yml:/etc/prometheus/prometheus.yml

grafana:
  image: grafana/grafana:latest
  ports:
    - "3000:3000"      # Grafana UI
  environment:
    - GF_SECURITY_ADMIN_PASSWORD=admin
  volumes:
    - grafana_data:/var/lib/grafana

# Container Management
portainer:
  image: portainer/portainer-ce:latest
  ports:
    - "9443:9443"      # Portainer UI
  volumes:
    - /var/run/docker.sock:/var/run/docker.sock
    - portainer_data:/data

# Optional: ELK Stack
elasticsearch:
  image: docker.elastic.co/elasticsearch/elasticsearch:8.11.0
  ports:
    - "9200:9200"
  environment:
    - discovery.type=single-node
    - xpack.security.enabled=false

kibana:
  image: docker.elastic.co/kibana/kibana:8.11.0
  ports:
    - "5601:5601"      # Kibana UI
  depends_on:
    - elasticsearch
```

## Configuration Files to Create

- [ ] `monitoring/prometheus.yml` - Prometheus scraping configuration
- [ ] `monitoring/grafana-dashboards/` - Pre-built dashboard definitions
- [ ] `monitoring/alerts/` - Alert rule definitions
- [ ] `observability.json` - OpenTelemetry configuration templates

## Benefits of This Approach

### Advantages
- **Full Control**: No "magic" or hidden configurations
- **Incremental**: Can implement piece by piece
- **Industry Standard**: Uses well-established tools
- **Portable**: Works with any container orchestrator
- **Transparent**: Clear understanding of each component
- **Cost Effective**: All open-source tools

### Monitoring Capabilities
- **Request Tracing**: End-to-end request visibility
- **Performance Metrics**: Response times, throughput, errors
- **Business Metrics**: User activity, tool usage, rental patterns
- **Infrastructure Health**: Container, database, cache status
- **Real-time Alerts**: Proactive issue detection

## Implementation Priority

### High Priority
1. OpenTelemetry + Jaeger for distributed tracing
2. Basic Prometheus metrics
3. Simple Grafana dashboards

### Medium Priority
1. Enhanced health checks
2. Portainer for container management
3. Custom business metrics

### Low Priority
1. ELK stack for log aggregation
2. Advanced alerting rules
3. Security monitoring dashboards

## Alternative: Future Kubernetes Migration

If scaling becomes necessary:
- [ ] Evaluate k3s/minikube for local development
- [ ] Consider managed Kubernetes (AKS, EKS, GKE) for production
- [ ] Migrate Docker Compose services to Kubernetes manifests
- [ ] Implement Kubernetes-native monitoring (Prometheus Operator)

## Notes
- Avoid .NET Aspire due to past complexity issues with ports/discovery
- Keep current Docker Compose foundation
- Maintain ability to run individual services for development
- Ensure all monitoring tools are optional (app works without them)