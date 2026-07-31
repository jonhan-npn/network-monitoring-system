# Grafana dashboards

This folder is a placeholder for the optional infra-metrics stack
(Prometheus + Grafana), which is separate from your main Angular
dashboard — that one reads directly from Postgres via the API and
doesn't need Grafana at all.

Add this only if you later want infrastructure-level charts (request
latency, GC, container resource usage) layered on top of the app.

To use it:
1. Export a dashboard JSON from the Grafana UI (Share -> Export -> Save to file).
2. Drop it into `infrastructure/monitoring/grafana/provisioning/dashboards/json/`.
3. Restart the grafana container — the provider in `dashboards.yml` picks it up automatically.
