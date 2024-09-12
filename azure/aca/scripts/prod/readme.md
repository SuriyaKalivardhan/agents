# Steps
1. Multi-tenant service deployment 
    - 1.Mutlitenant FileAPI deployment
2. Customer side setup
    - 2.Customer data source setup
    - 3.Customer compute n/w to upload data, delete customer compute
    - 4.Customer delegated network for injection
3. Platform Singletenant setup
    - 5.CreateSubnet for customer injection
    - 6.Create Env

## Open Items still failing:
1. Private Only ACA is provisioning timeout
2. DNS resolution is applied from infra-network instead of customer network
3. How to GetIps from ACA instances (not environments)
4. Multi env on same customer network (single 1P vnet) - Need for migration. Overlook?
5. Image pull still through customer network

### Notes
    1. Subscriptions enabled
        - 921496dc-987f-410f-bd57-426eb2611356
        - 47f1c914-e299-4953-a99d-3e34644cfe1c
        - 6a6fff00-4464-4eab-a6b1-0b533c7202e0
        - ea4faa5b-5e44-4236-91f6-5483d5b17d14